using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class InventorySlotUI : MonoBehaviour, ItemSlot, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{

    [Header("Binding")]
    public int slotIndex;
    public ItemUI itemUI;
    
    
    public bool isQuickSlot = false; //260221 Äü½½·Ô Ãß°¡  
    // Interface Property
    public Item CurrentItem { get; set; }

    private bool _wasDragging;

    private void Reset()
    {
        if (itemUI == null)
            itemUI = GetComponentInChildren<ItemUI>(true);
    }
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
        if (_wasDragging) return;
    }
}
