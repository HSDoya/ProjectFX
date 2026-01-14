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
        // 1. 메인 인벤토리 슬롯 수집
        mainSlots.Clear();
        if (itemGrid != null)
        {
            mainSlots.AddRange(itemGrid.GetComponentsInChildren<InventorySlotUI>(true));
        }

        // 메인 슬롯 인덱스 설정 (0 ~ 69)
        for (int i = 0; i < mainSlots.Count; i++)
            mainSlots[i].slotIndex = i;

        // 2. 퀵슬롯 UI 수집 (만약 퀵슬롯 UI도 InventorySlotUI를 재사용한다면)
        quickSlotsUI.Clear();
        if (quickSlotGrid != null)
        {
            quickSlotsUI.AddRange(quickSlotGrid.GetComponentsInChildren<InventorySlotUI>(true));
        }

        // 퀵슬롯 인덱스 설정 (퀵슬롯은 별도 로직이 필요할 수 있으나, 일단 0~13으로 세팅)
        // 주의: 드래그 시 인덱스 혼동을 막기 위해 퀵슬롯용 별도 컴포넌트를 쓰거나,
        // InventorySlotUI에 isQuickSlot 같은 플래그를 두는 것이 좋습니다.
        for (int i = 0; i < quickSlotsUI.Count; i++)
        {
            quickSlotsUI[i].slotIndex = i;
            // 퀵슬롯임을 구분할 방법이 필요합니다.
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
