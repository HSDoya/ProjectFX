using UnityEngine;

public class InventorySlotUI : MonoBehaviour, ItemSlot
{
    [Header("Binding")]
    public int slotIndex;     // Inventory.items에서 이 슬롯이 담당하는 index
    public ItemUI itemUI;     // 자식(또는 동일 오브젝트)에 있는 ItemUI

    public Item CurrentItem { get; set; }

    private void Reset()
    {
        // 자동 연결(가능하면)
        if (itemUI == null)
            itemUI = GetComponentInChildren<ItemUI>(true);
    }

    /// <summary>
    /// 인벤 슬롯은 제한 없음: 모든 아이템 수용
    /// </summary>
    public bool CanReceive(Item item) => true;

    /// <summary>
    /// CurrentItem 기반으로 내부 아이콘/수량 갱신
    /// </summary>
    public void Refresh()
    {
        if (itemUI == null) return;

        if (CurrentItem != null)
            itemUI.SetItem(CurrentItem);
        else
            itemUI.ClearSlot();
    }

    /// <summary>
    /// InventoryUI가 items 리스트를 넘겨주면 인덱스에 맞게 CurrentItem을 채우는 헬퍼.
    /// </summary>
    public void BindFromInventoryList(System.Collections.Generic.List<Item> items)
    {
        if (items == null)
        {
            CurrentItem = null;
            Refresh();
            return;
        }

        CurrentItem = (slotIndex >= 0 && slotIndex < items.Count) ? items[slotIndex] : null;
        Refresh();
    }
}
