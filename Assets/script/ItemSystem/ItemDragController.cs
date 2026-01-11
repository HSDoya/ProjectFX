using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemDragController : MonoBehaviour
{
    public static ItemDragController Instance { get; private set; }
    [Header("Drag Visual")]
    public RectTransform dragIconRoot;   // DragIcon의 RectTransform
    public Image dragIconImage;          // DragIcon의 Image
    public Canvas canvas;               // 보통 최상위 Canvas

    private ItemSlot sourceSlot;
    private Item draggedItem;
    private bool isDragging;

    private void Awake()
    {
        
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dragIconImage != null)
        {
            dragIconImage.raycastTarget = false;
            dragIconImage.enabled = false;
        }
    }

    public bool HasDrag => isDragging;

    public void BeginDrag(ItemSlot source, Item item, Sprite icon)
    {
        if (source == null || item == null || item.data == null) return;
        if (dragIconRoot == null || dragIconImage == null || canvas == null)
        {
            Debug.LogError("ItemDragController 참조(dragIconRoot/dragIconImage/canvas)가 설정되지 않았습니다.");
            return;
        }

        sourceSlot = source;
        draggedItem = item;
        isDragging = true;

        dragIconImage.sprite = icon;
        dragIconImage.enabled = true;

        SetDragPosition(Input.mousePosition);
    }

    public void Drag(PointerEventData eventData)
    {
        if (!isDragging) return;
        SetDragPosition(eventData.position);
    }

    public void EndDrag()
    {
        ClearDragVisual();
        sourceSlot = null;
        draggedItem = null;
        isDragging = false;
    }

    public void DropOn(ItemSlot targetSlot)
    {
        if (!isDragging || sourceSlot == null || draggedItem == null)
        {
            EndDrag();
            return;
        }

        if (targetSlot == null || targetSlot == sourceSlot)
        {
            EndDrag();
            return;
        }

        // 1) 타겟 슬롯이 드래그 아이템을 받을 수 있는지
        if (!targetSlot.CanReceive(draggedItem))
        {
            EndDrag();
            return;
        }

        // 2) (스왑일 경우) 소스 슬롯이 타겟 아이템을 받을 수 있는지도 확인
        var targetItem = targetSlot.CurrentItem;
        if (targetItem != null && !sourceSlot.CanReceive(targetItem))
        {
            EndDrag();
            return;
        }

        // 3) 실제 이동/스왑 수행
        PerformMove(sourceSlot, targetSlot, draggedItem, targetItem);

        EndDrag();
    }

    private void PerformMove(ItemSlot src, ItemSlot dst, Item srcItem, Item dstItem)
    {
        // 케이스 A: 인벤 ↔ 인벤 스왑
        if (src is InventorySlotUI invA && dst is InventorySlotUI invB)
        {
            SwapInventory(invA.slotIndex, invB.slotIndex);
            return;
        }

        // 케이스 B: 인벤 → 장비 (장착)
        if (src is InventorySlotUI invSrc && dst is EquipmentSlotUI eqDst)
        {
            EquipFromInventory(invSrc, eqDst);
            return;
        }

        // 케이스 C: 장비 → 인벤 (해제)
        if (src is EquipmentSlotUI eqSrc && dst is InventorySlotUI invDst)
        {
            UnequipToInventory(eqSrc, invDst);
            return;
        }

        // (지금 단계에서는 여기까지만)
        // 퀵슬롯은 다음 단계에서 추가
    }

    private void SwapInventory(int a, int b)
    {
        var items = Inventory.instance.items;
        if (a < 0 || b < 0 || a >= items.Count || b >= items.Count) return;

        var tmp = items[a];
        items[a] = items[b];
        items[b] = tmp;

        Inventory.instance.onItemChangedCallback?.Invoke();
    }

    private void EquipFromInventory(InventorySlotUI invSrc, EquipmentSlotUI eqDst)
    {
        // 인벤에서 1개(또는 통째) 꺼내기
        if (!Inventory.instance.TryTakeOneAt(invSrc.slotIndex, out var taken))
            return;

        // 목적 슬롯의 타입에 장착 시도(EquipmentManager에서 slotType으로 관리)
        var replaced = EquipmentManager.instance.Equip(eqDst.slotType, taken);

        // 교체된 장비가 있으면 인벤으로 반환
        if (replaced != null)
        {
            bool ok = Inventory.instance.AddItem(replaced);
            if (!ok)
            {
                // 인벤에 못 돌려놓으면 원복
                EquipmentManager.instance.Equip(eqDst.slotType, replaced);
                Inventory.instance.AddItem(taken);
            }
        }
    }

    private void UnequipToInventory(EquipmentSlotUI eqSrc, InventorySlotUI invDst)
    {
        var removed = EquipmentManager.instance.Unequip(eqSrc.slotType);
        if (removed == null) return;

        // 인벤에 넣기 (빈칸 개념이 아직 없어서 List에 Add됩니다)
        bool ok = Inventory.instance.AddItem(removed);
        if (!ok)
        {
            // 실패하면 원복
            EquipmentManager.instance.Equip(eqSrc.slotType, removed);
        }
    }

    private void SetDragPosition(Vector2 screenPos)
    {
        // Canvas 스케일을 고려한 위치 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.worldCamera,
            out var localPos
        );
        dragIconRoot.localPosition = localPos;
    }

    private void ClearDragVisual()
    {
        if (dragIconImage != null)
        {
            dragIconImage.sprite = null;
            dragIconImage.enabled = false;
        }
    }
}
