using UnityEngine;

public class Item
{
    public ItemData data;    // ������ ���� (ScriptableObject)
    public int quantity;     // ������ ���� (���� ���� ������ ���)

    // �⺻ ������
    public Item(ItemData newItemData, int initialQuantity = 1)
    {
        data = newItemData;
        quantity = initialQuantity;
    }

    // ���� ���� �޼��� (����)
    public void AddQuantity(int amount)
    {
        quantity += amount;
        if (quantity > data.maxStackAmount)
            quantity = data.maxStackAmount;
    }

    // ���� ���� �޼��� (����)
    public void RemoveQuantity(int amount)
    {
        quantity -= amount;
        if (quantity < 0)
            quantity = 0;
    }
}
