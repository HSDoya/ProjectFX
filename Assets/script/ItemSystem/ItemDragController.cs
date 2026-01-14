using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemDragController : MonoBehaviour
{
    public static ItemDragController Instance { get; private set; }

    [Header("Drag Visual")]
    public RectTransform dragIconRoot;   // DragIcon의 RectTransform 부모
    public Image dragIconImage;          // 실제 아이콘 Image
    public Canvas canvas;                // 최상위 Canvas (좌표 변환용)

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
            dragIconImage.raycastTarget = false; // 드래그 중인 아이콘이 마우스 이벤트를 가로채지 않게 설정
            dragIconImage.enabled = false;
        }
    }

    public bool HasDrag => isDragging;

    public void BeginDrag(ItemSlot source, Item item, Sprite icon)
    {
        if (source == null || item == null || item.data == null) return;

        // 필수 컴포넌트 체크
        if (dragIconRoot == null || dragIconImage == null || canvas == null)
        {
            Debug.LogError("ItemDragController: dragIconRoot, dragIconImage, or canvas is missing!");
            return;
        }

        sourceSlot = source;
        draggedItem = item;
        isDragging = true;

        // 드래그 이미지 설정
        dragIconImage.sprite = icon;
        dragIconImage.enabled = true;

        // 아이콘 크기를 슬롯과 비슷하게 맞추려면 여기서 sizeDelta 조정 가능
        // dragIconRoot.sizeDelta = new Vector2(50, 50); 

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

        // 1) 타겟 슬롯이 드래그 아이템을 받을 수 있는지 확인 (예: 장비 슬롯 타입 체크)
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
        // 케이스 A: 인벤 ↔ 인벤 스왑 (또는 이동)
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

        // (추후 퀵슬롯 로직이 추가된다면 여기서 처리)
    }

    /// <summary>
    /// 인벤토리 내부 스왑 (또는 빈칸으로 이동)
    /// </summary>
    private void SwapInventory(int a, int b)
    {
        // ★ [수정됨] Inventory.instance.items는 이제 배열(Array)입니다.
        var items = Inventory.instance.items;

        // ★ .Count -> .Length 로 변경
        if (items == null || a < 0 || b < 0 || a >= items.Length || b >= items.Length)
            return;

        // 단순 교환 (빈칸이 null이어도 정상 작동)
        var tmp = items[a];
        items[a] = items[b];
        items[b] = tmp;

        // UI 갱신 알림
        // (Inventory 스크립트에서 public event Action onItemChangedCallback; 으로 변경했다고 가정)
        // 만약 에러나면 Inventory.cs의 이벤트 이름을 확인하세요.
        // 여기서는 기존 코드 호환을 위해 delegate Invoke 방식으로 호출합니다.
        Inventory.instance.RefreshUI();
    }

    private void EquipFromInventory(InventorySlotUI invSrc, EquipmentSlotUI eqDst)
    {
        // 인벤에서 1개(또는 통째) 꺼내기
        if (!Inventory.instance.TryTakeOneAt(invSrc.slotIndex, out var taken))
            return;

        // 장착 시도 (기존 장비가 있다면 리턴됨)
        var replaced = EquipmentManager.instance.Equip(eqDst.slotType, taken);

        // 교체된 장비가 있으면 인벤으로 반환
        if (replaced != null)
        {
            // 인벤토리의 해당 슬롯이 비었으면 그 자리에 넣고, 아니면 AddItem으로 빈 곳 찾기
            // (방금 TryTakeOneAt으로 비웠으니 그 자리가 비어있을 확률이 높지만, 스택 분리 시에는 아닐 수 있음)

            // 1. 원래 있던 자리에 넣기 시도 (사용자 경험상 좋음)
            if (Inventory.instance.items[invSrc.slotIndex] == null)
            {
                Inventory.instance.items[invSrc.slotIndex] = replaced;
                Inventory.instance.RefreshUI();
            }
            else
            {
                // 2. 원래 자리가 찼으면(스택 분리 등) 다른 빈칸에 넣기
                bool ok = Inventory.instance.AddItem(replaced);
                if (!ok)
                {
                    // 인벤 꽉 참 -> 장착 취소(원복)
                    EquipmentManager.instance.Equip(eqDst.slotType, replaced); // 다시 장착
                    Inventory.instance.AddItem(taken); // 뺀거 다시 넣기
                    Debug.Log("인벤토리가 꽉 차서 장비를 교체할 수 없습니다.");
                }
            }
        }
    }

    private void UnequipToInventory(EquipmentSlotUI eqSrc, InventorySlotUI invDst)
    {
        // 목적지 슬롯(invDst)에 아이템이 있으면 스왑 불가 (장비 해제는 보통 빈칸에만 허용)
        // 기획에 따라 장비<->인벤아이템 스왑을 허용할 수도 있지만, 복잡하므로 여기선 빈칸일 때만.
        if (invDst.CurrentItem != null)
        {
            Debug.Log("장비 해제는 빈 슬롯으로만 가능합니다.");
            // (필요하면 여기서 Swap 로직을 구현할 수도 있습니다)
            return;
        }

        var removed = EquipmentManager.instance.Unequip(eqSrc.slotType);
        if (removed == null) return;

        // 드롭한 특정 슬롯(invDst.slotIndex)에 넣기
        var items = Inventory.instance.items;
        if (invDst.slotIndex >= 0 && invDst.slotIndex < items.Length)
        {
            items[invDst.slotIndex] = removed;
            Inventory.instance.RefreshUI();
        }
        else
        {
            // 인덱스 오류 시 그냥 자동 추가
            Inventory.instance.AddItem(removed);
        }
    }

    private void SetDragPosition(Vector2 screenPos)
    {
        if (canvas == null) return;

        // Canvas의 RenderMode에 따라 좌표 변환 방식이 다를 수 있음
        // ScreenSpace-Overlay일 경우 그냥 position 대입해도 되지만, 
        // ScreenSpace-Camera나 WorldSpace일 경우 아래 방식이 안전함.
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.worldCamera,
            out var localPos
        ))
        {
            dragIconRoot.localPosition = localPos;
        }
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