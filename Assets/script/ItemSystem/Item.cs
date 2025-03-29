using UnityEngine;

public class Item
{
    public ItemData data;    // 아이템 정보 (ScriptableObject)
    public int quantity;     // 아이템 수량 (스택 가능 아이템 대비)

    // 기본 생성자
    public Item(ItemData newItemData, int initialQuantity = 1)
    {
        data = newItemData;
        quantity = initialQuantity;
    }

    // 스택 증가 메서드 (선택)
    public void AddQuantity(int amount)
    {
        quantity += amount;
        if (quantity > data.maxStackAmount)
            quantity = data.maxStackAmount;
    }

    // 스택 감소 메서드 (선택)
    public void RemoveQuantity(int amount)
    {
        quantity -= amount;
        if (quantity < 0)
            quantity = 0;
    }
}
