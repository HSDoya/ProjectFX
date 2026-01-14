using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class InventorySlotUI : MonoBehaviour, ItemSlot, IPointerClickHandler, IBeginDragHandler,IDragHandler,IEndDragHandler,IDropHandler
{

    [Header("Binding")]
    public int slotIndex;
    public ItemUI itemUI;

    // Interface Property
    public Item CurrentItem { get; set; }

    private bool _wasDragging;

    private void Reset()
    {
        if (itemUI == null)
            itemUI = GetComponentInChildren<ItemUI>(true);
    }

    // ★ [핵심 변경] 리스트를 통째로 받는 대신, 아이템 1개를 받아서 세팅하도록 변경
    public void BindItem(Item item)
    {
        CurrentItem = item;
        Refresh();
    }

    // Interface Method
    public void Refresh()
    {
        if (itemUI == null) return;

        if (CurrentItem != null && CurrentItem.data != null)
            itemUI.SetItem(CurrentItem);
        else
            itemUI.ClearSlot();
    }

    // ... (OnBeginDrag, OnDrag, OnEndDrag, OnDrop, OnPointerClick 등 기존 로직은 그대로 유지) ...
    // ... (기존 드래그 및 클릭 로직에는 변경 사항 없습니다) ...

    // 드래그 코드 생략 (기존과 동일)
    public void OnBeginDrag(PointerEventData eventData)
    {
        _wasDragging = false;
        if (CurrentItem == null || CurrentItem.data == null) return;
        if (ItemDragController.Instance == null) return;
        Sprite iconSprite = (itemUI != null && itemUI.icon != null) ? itemUI.icon.sprite : null;
        ItemDragController.Instance.BeginDrag(this, CurrentItem, iconSprite);
    }
    public void OnDrag(PointerEventData eventData) { if (ItemDragController.Instance) { _wasDragging = true; ItemDragController.Instance.Drag(eventData); } }
    public void OnEndDrag(PointerEventData eventData) { ItemDragController.Instance?.EndDrag(); }
    public void OnDrop(PointerEventData eventData) { ItemDragController.Instance?.DropOn(this); }
    public bool CanReceive(Item item) => true;
    public void OnPointerClick(PointerEventData eventData)
    {
        // 기존 OnPointerClick 로직 그대로 유지
        if (_wasDragging) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (CurrentItem == null || CurrentItem.data == null) return;
        if (EquipmentManager.instance == null) return;
        if (CurrentItem.data.itemType != ItemType.Equipment) return;
        if (CurrentItem.data.equipSlot == EquipmentSlotType.None) return;

        // ★ 주의: 여기서 TryTakeOneAt 호출 시 Inventory 로직이 바뀌었으므로
        // Inventory.cs의 TryTakeOneAt이 정상 구현되어 있다면 여기는 수정할 필요 없습니다.
        if (!Inventory.instance.TryTakeOneAt(slotIndex, out var taken)) return;

        var replaced = EquipmentManager.instance.Equip(taken.data.equipSlot, taken);
        if (replaced != null)
        {
            bool ok = Inventory.instance.AddItem(replaced);
            if (!ok)
            {
                EquipmentManager.instance.Equip(replaced.data.equipSlot, replaced);
                Inventory.instance.AddItem(taken);
            }
        }
    }
}
