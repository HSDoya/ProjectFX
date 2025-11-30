using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager instance;

    public Item[] equippedItems; // Weapon, Armor, Hat, Shoes ... 순서대로

    public delegate void OnEquipmentChanged();
    public OnEquipmentChanged onEquipmentChangedCallback;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 슬롯 개수만큼 배열 생성 (예: 4개)
        equippedItems = new Item[4];
    }

    public void Equip(Item item)
    {
        if (item.data.itemType != ItemType.Equipment)
            return;

        int index = (int)item.data.equipSlot; // Weapon=1, Armor=2 이런 식으로 맞춰두기

        Item oldItem = equippedItems[index];
        if (oldItem != null)
        {
            // 기존 장비는 다시 인벤토리로
            Inventory.instance.Add(oldItem.data);
        }

        equippedItems[index] = item;
        Inventory.instance.Remove(item); // 인벤토리에서 제거

        onEquipmentChangedCallback?.Invoke();
    }

    public void Unequip(int slotIndex)
    {
        Item item = equippedItems[slotIndex];
        if (item == null) return;

        Inventory.instance.Add(item.data);
        equippedItems[slotIndex] = null;

        onEquipmentChangedCallback?.Invoke();
    }
}
