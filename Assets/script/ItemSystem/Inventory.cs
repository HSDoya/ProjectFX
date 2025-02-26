using UnityEngine;
using System.Collections.Generic;
public class Inventory : MonoBehaviour
{
    #region Singleton
    public static Inventory instance;
    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogWarning("More than one instance of Inventory found!");
            return;
        }
        instance = this;
    }
    #endregion

    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    public int space = 20;  // 인벤토리 아이템 슬롯의 수
    public List<Item> items = new List<Item>();  // 인벤토리에 현재 있는 아이템 리스트

    public bool Add(ItemData newItemData)
    {
        if (items.Count >= space)
        {
            Debug.Log("Not enough room.");
            return false;
        }

        // Item 객체 대신 ItemData 저장
        Item newItem = new Item(newItemData);
        items.Add(newItem);

        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();

        return true;
    }
}
