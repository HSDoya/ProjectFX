using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    private void Awake()
    {
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

    public bool Add(ItemData newItemData)
    {
        if (items.Count >= space)
        {
            Debug.Log("Not enough room.");
            return false;
        }

        items.Add(new Item(newItemData));
        onItemChangedCallback?.Invoke();
        return true;
    }
}
