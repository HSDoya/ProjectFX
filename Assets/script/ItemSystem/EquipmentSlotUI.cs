using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour,ItemSlot, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Slot Rule")]
    public EquipmentSlotType slotType;   // ★ 반드시 필요

    [Header("UI")]
    public ItemUI itemUI;

    public Item CurrentItem { get; set; }

    private bool _wasDragging;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _wasDragging = false;

        if (CurrentItem == null || CurrentItem.data == null) return;
        if (ItemDragController.Instance == null) return;

        Sprite iconSprite = (itemUI != null && itemUI.icon != null) ? itemUI.icon.sprite : null;

        ItemDragController.Instance.BeginDrag(this, CurrentItem, iconSprite);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ItemDragController.Instance == null) return;

        _wasDragging = true;
        ItemDragController.Instance.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ItemDragController.Instance?.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemDragController.Instance?.DropOn(this);
    }

    private void Reset()
    {
        if (itemUI == null)
            itemUI = GetComponentInChildren<ItemUI>(true);
    }

    private void OnEnable()
    {
        if (EquipmentManager.instance != null)
            EquipmentManager.instance.OnEquipmentChanged += RefreshFromManager;

        RefreshFromManager();
    }

    private void OnDisable()
    {
        if (EquipmentManager.instance != null)
            EquipmentManager.instance.OnEquipmentChanged -= RefreshFromManager;
    }

    public bool CanReceive(Item item)
    {
        if (item == null || item.data == null) return false;
        return item.data.equipSlot == slotType;
    }

    public void Refresh()
    {
        if (itemUI == null) return;

        if (CurrentItem != null)
            itemUI.SetItem(CurrentItem);
        else
            itemUI.ClearSlot();
    }

    private void RefreshFromManager()
    {
        if (EquipmentManager.instance == null)
        {
            CurrentItem = null;
            Refresh();
            return;
        }

        CurrentItem = EquipmentManager.instance.GetEquipped(slotType);
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_wasDragging) return;
        //if (eventData.button != PointerEventData.InputButton.Left)
        //    return;

        //if (EquipmentManager.instance == null)
        //    return;

        //var removed = EquipmentManager.instance.Unequip(slotType);
        //if (removed == null)
        //    return;

        //bool ok = Inventory.instance.AddItem(removed);
        //if (!ok)
        //{
        //    EquipmentManager.instance.Equip(slotType, removed);
        //}
    }
}
