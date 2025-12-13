using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] public Transform itemGrid;

    private readonly List<InventorySlotUI> slots = new();

    void Awake()
    {
        slots.Clear();
        slots.AddRange(itemGrid.GetComponentsInChildren<InventorySlotUI>(true)); // 슬롯 수집

        // 슬롯 인덱스 자동 세팅(Inspector에서 이미 넣어뒀다면 아래 줄 제거 가능)
        for (int i = 0; i < slots.Count; i++)
            slots[i].slotIndex = i;
    }

    void OnEnable()
    {
        Inventory.instance.onItemChangedCallback -= UpdateUI;
        Inventory.instance.onItemChangedCallback += UpdateUI;
        UpdateUI();
    }

    void OnDisable()
    {
        Inventory.instance.onItemChangedCallback -= UpdateUI;
    }

    public void UpdateUI()
    {
        var items = Inventory.instance.items;

        // 모든 슬롯에 대해 인덱스 기준으로 바인딩
        for (int i = 0; i < slots.Count; i++)
            slots[i].BindFromInventoryList(items);
    }

}
