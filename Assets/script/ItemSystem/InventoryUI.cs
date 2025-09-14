using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] public Transform itemGrid;

    private readonly List<ItemUI> slots = new();

    void Awake()
    {
        slots.Clear();
        slots.AddRange(itemGrid.GetComponentsInChildren<ItemUI>(true)); // 손배치 슬롯 수집
    }

    void OnEnable()
    {
        Inventory.instance.onItemChangedCallback -= UpdateUI;
        Inventory.instance.onItemChangedCallback += UpdateUI; // 콜백 등록
        UpdateUI(); // 처음 켤 때 즉시 반영
    }

    void OnDisable()
    {
        Inventory.instance.onItemChangedCallback -= UpdateUI; // 콜백 해제
    }

    public void UpdateUI()
    {
        foreach (var s in slots) s.ClearSlot();

        var items = Inventory.instance.items;   // 인벤토리 데이터를
        for (int i = 0; i < slots.Count && i < items.Count; i++)
            slots[i].SetItem(items[i]);         // 슬롯에 그대로 바인딩
    }

}
