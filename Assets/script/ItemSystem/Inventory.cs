using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;
    [SerializeField] private GameObject inventoryUI;
    public bool inventory_bool;

    private void Awake()
    {
        inventory_bool = false;
        inventoryUI.SetActive(false);
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of Inventory found!");
            return;
        }
        instance = this;
    }
    private void Start()
    {
        var wood = ItemDataCsvLoader.instance?.GetItemDataByID("wood");

        if (ItemDataCsvLoader.instance == null)
        {
            Debug.LogError("ItemDataCsvLoader가 초기화되지 않았습니다!");
        }
        if (wood == null)
        {
            Debug.LogError("[Inventory.cs] wood 아이템이 null입니다. CSV 확인 필요!");
            return;
        }

        for (int i = 0; i < 99; i++)
            Add(wood);
    }

    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    public int space = 20;
    public List<Item> items = new List<Item>();

    private void Update()
    {
        inventory_UI();
    }
    public bool Add(ItemData newItemData)
    {
        // 고유 ID로 비교
        if (newItemData.canStack)
        {
            foreach (Item item in items)
            {
                if (item.data.itemID == newItemData.itemID)
                {
                    item.AddQuantity(1);
                    onItemChangedCallback?.Invoke();
                    return true;
                }
            }
        }

        if (items.Count >= space)
        {
            Debug.Log("Not enough room.");
            return false;
        }

        items.Add(new Item(newItemData));
        onItemChangedCallback?.Invoke();
        return true;
    }

    private void inventory_UI()
    {
        if(inventory_bool == true)
        {
            inventoryUI.SetActive(true);
        }
        else
        {
            inventoryUI.SetActive(false);
        }
    }
    public bool Remove(Item item)
    {
        bool removed = items.Remove(item);
        if (removed)
            onItemChangedCallback?.Invoke();
        return removed;
    }
}
