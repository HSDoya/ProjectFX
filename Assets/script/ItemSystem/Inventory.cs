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
            Debug.LogError("ItemDataCsvLoader�� �ʱ�ȭ���� �ʾҽ��ϴ�!");
        }
        if (wood == null)
        {
            Debug.LogError("[Inventory.cs] wood �������� null�Դϴ�. CSV Ȯ�� �ʿ�!");
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
        // ���� ID�� ��
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
