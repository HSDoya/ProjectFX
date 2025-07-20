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
}
