using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabaseSO : ScriptableObject
{
    // 모든 아이템 에셋을 담아둘 리스트
    public List<ItemData> allItems = new List<ItemData>();

    // 런타임 빠른 검색을 위한 딕셔너리
    private Dictionary<string, ItemData> itemDict = new Dictionary<string, ItemData>();

    public void Initialize()
    {
        itemDict.Clear();
        foreach (var item in allItems)
        {
            if (item != null && !itemDict.ContainsKey(item.itemID))
            {
                itemDict.Add(item.itemID, item);
            }
        }
    }

    public ItemData GetItemByID(string id)
    {
        if (itemDict.Count == 0) Initialize(); // 초기화 보장

        itemDict.TryGetValue(id, out var item);
        return item;
    }
}