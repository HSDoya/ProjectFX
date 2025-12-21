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
        var armor = ItemDataCsvLoader.instance?.GetItemDataByID("Breastplate");

        if (ItemDataCsvLoader.instance == null)
        {
            Debug.LogError("ItemDataCsvLoader가 초기화되지 않았습니다!");
            return;
        }

        if (wood == null)
        {
            Debug.LogError("[Inventory.cs] wood 아이템이 null입니다. CSV 확인 필요!");
            return;
        }

        if (armor == null)
        {
            Debug.LogError("[Inventory.cs] Breastplate 아이템이 null입니다. CSV 확인 필요!");
            return;
        }

        // 기존 테스트: wood 99개
        for (int i = 0; i < 99; i++)
            Add(wood);

        // 추가: 갑옷 1개
        Add(armor);
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

    public bool AddItem(Item item)
    {
        if (item == null || item.data == null) return false;

        // 스택 가능이면 합치기
        if (item.data.canStack)
        {
            foreach (var it in items)
            {
                if (it.data.itemID == item.data.itemID)
                {
                    it.AddQuantity(item.quantity);
                    onItemChangedCallback?.Invoke();
                    return true;
                }
            }
        }

        if (items.Count >= space)
        {
            Debug.Log("Not enough room (AddItem).");
            return false;
        }

        items.Add(item);
        onItemChangedCallback?.Invoke();
        return true;
    }

    /// <summary>
    /// 인벤 특정 index에서 장착용으로 '1개'를 꺼냄.
    /// - 수량이 2 이상이면 quantity만 1 줄이고, 꺼낸 Item(1개)을 반환
    /// - 수량이 1이면 리스트에서 제거하고 반환
    /// </summary>
    public bool TryTakeOneAt(int index, out Item taken)
    {
        taken = null;

        if (index < 0 || index >= items.Count) return false;
        var src = items[index];
        if (src == null || src.data == null) return false;

        if (src.data.canStack && src.quantity > 1)
        {
            src.RemoveQuantity(1);
            taken = new Item(src.data, 1);
            onItemChangedCallback?.Invoke();
            return true;
        }

        // 수량 1이거나 스택 불가면 통째로 이동
        items.RemoveAt(index);
        taken = src;
        onItemChangedCallback?.Invoke();
        return true;
    }
}
