using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Grid Parents")]
    [SerializeField] public Transform itemGrid;      // 메인 인벤토리(14x5) 부모
    [SerializeField] public Transform quickSlotGrid; // 퀵슬롯(1x14) 부모 (없으면 비워도 됨)

    private List<InventorySlotUI> mainSlots = new();
    private List<InventorySlotUI> quickSlotsUI = new();

    void Awake()
    {
        // 1. 메인 인벤토리 슬롯 설정
        mainSlots.Clear();
        if (itemGrid != null)
            mainSlots.AddRange(itemGrid.GetComponentsInChildren<InventorySlotUI>(true));

        for (int i = 0; i < mainSlots.Count; i++)
        {
            mainSlots[i].slotIndex = i;
            //mainSlots[i].isQuickSlot = false; // ★ 메인 인벤토리임
        }

        // 2. 퀵슬롯 UI 설정
        quickSlotsUI.Clear();
        if (quickSlotGrid != null)
            quickSlotsUI.AddRange(quickSlotGrid.GetComponentsInChildren<InventorySlotUI>(true));

        for (int i = 0; i < quickSlotsUI.Count; i++)
        {
            quickSlotsUI[i].slotIndex = i;
            quickSlotsUI[i].isQuickSlot = true; // ★ 퀵슬롯임!
        }
    }

    void OnEnable()
    {
        if (Inventory.instance != null)
        {
            Inventory.instance.onItemChangedCallback -= UpdateUI;
            Inventory.instance.onItemChangedCallback += UpdateUI;
        }
        UpdateUI();
    }

    void OnDisable()
    {
        if (Inventory.instance != null)
            Inventory.instance.onItemChangedCallback -= UpdateUI;
    }

    public void UpdateUI()
    {
        if (Inventory.instance == null) return;

        // 1. 메인 인벤토리 갱신
        var items = Inventory.instance.items; // 배열(Item[])
        for (int i = 0; i < mainSlots.Count; i++)
        {
            if (i < items.Length)
                mainSlots[i].BindItem(items[i]); // 데이터가 있든 null이든 그대로 전달
            else
                mainSlots[i].BindItem(null);     // 범위를 벗어난 슬롯은 비움
        }

        // 2. 퀵슬롯 갱신 (데이터가 존재한다면)
        var qItems = Inventory.instance.quickSlots;
        if (qItems != null)
        {
            for (int i = 0; i < quickSlotsUI.Count; i++)
            {
                if (i < qItems.Length)
                    quickSlotsUI[i].BindItem(qItems[i]);
                else
                    quickSlotsUI[i].BindItem(null);
            }
        }
    }
}
