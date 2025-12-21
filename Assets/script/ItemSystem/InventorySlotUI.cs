using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, ItemSlot, IPointerClickHandler
{
    [Header("Binding")]
    public int slotIndex;
    public ItemUI itemUI;

    public Item CurrentItem { get; set; }

    private void Reset()
    {
        if (itemUI == null)
            itemUI = GetComponentInChildren<ItemUI>(true);
    }

    public bool CanReceive(Item item) => true;

    public void Refresh()
    {
        if (itemUI == null) return;

        if (CurrentItem != null)
            itemUI.SetItem(CurrentItem);
        else
            itemUI.ClearSlot();
    }

    public void BindFromInventoryList(List<Item> items)
    {
        CurrentItem = (items != null && slotIndex >= 0 && slotIndex < items.Count) ? items[slotIndex] : null;
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 좌클릭만 처리
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (CurrentItem == null || CurrentItem.data == null)
            return;

        if (EquipmentManager.instance == null)
        {
            Debug.LogError("EquipmentManager.instance가 없습니다(씬에 EquipmentManager를 배치했는지 확인).");
            return;
        }

        // 장비 아이템만 장착 (원하면 Consumable도 퀵슬롯으로 보내는 로직을 나중에 추가)
        if (CurrentItem.data.itemType != ItemType.Equipment)
            return;

        if (CurrentItem.data.equipSlot == EquipmentSlotType.None)
            return;

        // 인벤에서 1개(또는 통째) 꺼내기
        if (!Inventory.instance.TryTakeOneAt(slotIndex, out var taken))
            return;

        // 장착 시도: 교체된 장비가 있으면 반환
        var replaced = EquipmentManager.instance.Equip(taken.data.equipSlot, taken);

        // 교체 장비를 인벤으로 반환
        if (replaced != null)
        {
            bool ok = Inventory.instance.AddItem(replaced);
            if (!ok)
            {
                // 인벤이 가득 찼으면 원복
                EquipmentManager.instance.Equip(replaced.data.equipSlot, replaced);
                Inventory.instance.AddItem(taken);
            }
        }
    }
}
