using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// 탭 창의 Items 탭 내용. 인벤토리 칸과 창 안 퀵슬롯 줄만 담당한다.
//
// 화면 하단 HUD 퀵슬롯 바는 QuickSlotBarUI가 따로 담당한다. 예전엔 이 컴포넌트가 둘 다
// 맡고 있었는데, 이 컴포넌트는 창 패널 위에 붙어 있어서 창이 닫히면 같이 꺼진다.
// 그래서 창을 닫힌 상태로 시작하자 Awake가 실행되지 않아 HUD 퀵슬롯이 통째로 비었다.
// 수명이 다른 UI는 다른 컴포넌트가 소유해야 한다.
public class InventoryUI : MonoBehaviour
{
    [Header("UI Grid Parents")]
    [SerializeField] public Transform itemGrid;      // 메인 인벤토리(14x5) 부모
    [SerializeField] public Transform quickSlotGrid; // 퀵슬롯(1x14) 부모 (없으면 비워도 됨)


    private List<InventorySlotUI> mainSlots = new();
    private List<InventorySlotUI> quickSlotsUI = new();

    // 구독해 둔 인벤토리. Inventory.instance가 아직 비어 있는 순간에 OnEnable이 돌면 구독이
    // 조용히 실패하므로, 직접 찾아 참조를 쥐고 그걸로 해제까지 한다.
    private Inventory subscribedInventory;

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

        // Awake 시점엔 Inventory의 Awake가 아직 안 돌아 instance가 비어 있을 수 있어 직접 찾는다.
        subscribedInventory = Inventory.instance != null ? Inventory.instance : FindFirstObjectByType<Inventory>();
        if (subscribedInventory != null)
        {
            subscribedInventory.onItemChangedCallback -= UpdateUI;
            subscribedInventory.onItemChangedCallback += UpdateUI;
        }
    }
    // 창 내용이므로 보일 때만 구독한다. 숨어 있는 동안 갱신할 이유가 없다
    // (항상 보이는 HUD 퀵슬롯은 QuickSlotBarUI가 따로 구독한다).
    void OnEnable()
    {
        if (subscribedInventory == null)
            subscribedInventory = Inventory.instance != null ? Inventory.instance : FindFirstObjectByType<Inventory>();

        if (subscribedInventory != null)
        {
            subscribedInventory.onItemChangedCallback -= UpdateUI;
            subscribedInventory.onItemChangedCallback += UpdateUI;
        }

        UpdateUI();
    }

    void OnDisable()
    {
        if (subscribedInventory != null)
            subscribedInventory.onItemChangedCallback -= UpdateUI;
    }

    void OnDestroy()
    {
        if (subscribedInventory != null)
            subscribedInventory.onItemChangedCallback -= UpdateUI;
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
