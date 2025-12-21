using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour,ItemSlot, IPointerClickHandler
{
    [Header("Slot Rule")]
    public EquipmentSlotType slotType;   // ★ 반드시 필요

    [Header("UI")]
    public ItemUI itemUI;

    public Item CurrentItem { get; set; }

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
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (EquipmentManager.instance == null)
            return;

        var removed = EquipmentManager.instance.Unequip(slotType);
        if (removed == null)
            return;

        bool ok = Inventory.instance.AddItem(removed);
        if (!ok)
        {
            EquipmentManager.instance.Equip(slotType, removed);
        }
    }
}
