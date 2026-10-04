using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Grid Parents")]
    [SerializeField] public Transform itemGrid;      // 메인 인벤토리(14x5) 부모
    [SerializeField] public Transform quickSlotGrid; // 퀵슬롯(1x14) 부모 (없으면 비워도 됨)
    [SerializeField] public Transform hudQuickSlotGrid;


    private List<InventorySlotUI> mainSlots = new();
    private List<InventorySlotUI> quickSlotsUI = new();
    private List<InventorySlotUI> hudQuickSlotsUI = new();

    // 구독해 둔 인벤토리. 이 컴포넌트는 인벤토리 창(BG) 위에 있어서 창이 닫히면 같이 꺼지는데,
    // 항상 떠 있는 HUD 퀵슬롯도 여기서 갱신하므로 꺼져 있는 동안에도 구독을 유지해야 한다.
    // (예전엔 OnDisable에서 구독을 끊어서, 창이 닫힌 평상시에는 퀵슬롯이 전혀 갱신되지 않았다.)
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

        hudQuickSlotsUI.Clear();
        if (hudQuickSlotGrid != null)
            hudQuickSlotsUI.AddRange(hudQuickSlotGrid.GetComponentsInChildren<InventorySlotUI>(true));


        for (int i = 0; i < quickSlotsUI.Count; i++)
        {
            quickSlotsUI[i].slotIndex = i;
            quickSlotsUI[i].isQuickSlot = true; // ★ 퀵슬롯임!
        }
        for (int i = 0; i < hudQuickSlotsUI.Count; i++)
        {
            hudQuickSlotsUI[i].slotIndex = i;
            hudQuickSlotsUI[i].isQuickSlot = true; // 퀵슬롯과 동일하게 취급
        }

        // Awake 시점엔 Inventory의 Awake가 아직 안 돌아 instance가 비어 있을 수 있어 직접 찾는다.
        subscribedInventory = Inventory.instance != null ? Inventory.instance : FindFirstObjectByType<Inventory>();
        if (subscribedInventory != null)
        {
            subscribedInventory.onItemChangedCallback -= UpdateUI;
            subscribedInventory.onItemChangedCallback += UpdateUI;
        }
    }
    void Start()
    {
        // Start는 모든 스크립트의 Awake가 끝난 뒤 실행되므로, 
        // 이때는 Inventory.instance가 무조건 존재합니다.
        UpdateUI();
    }
    void OnEnable()
    {
        UpdateUI();
    }

    void OnDestroy()
    {
        if (subscribedInventory != null)
            subscribedInventory.onItemChangedCallback -= UpdateUI;
    }

    // 쿨타임 오버레이처럼 특정 퀵슬롯 칸 위에 뭔가를 띄워야 할 때 쓴다.
    // 슬롯 목록 수집은 이 클래스가 계속 소유하고(Awake에서 한 번), 밖에서는 조회만 하게 한다.
    public RectTransform GetHudQuickSlotRect(int index)
    {
        if (index < 0 || index >= hudQuickSlotsUI.Count) return null;
        return hudQuickSlotsUI[index] != null ? hudQuickSlotsUI[index].transform as RectTransform : null;
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

        // ★ 3. 새로 추가된 부분: HUD 퀵슬롯 갱신
        var hqItems = Inventory.instance.quickSlots;
        if (hqItems != null && hudQuickSlotsUI.Count > 0)
        {
            for (int i = 0; i < hudQuickSlotsUI.Count; i++)
            {
                if (i < hqItems.Length)
                    hudQuickSlotsUI[i].BindItem(hqItems[i]);
                else
                    hudQuickSlotsUI[i].BindItem(null);
            }
        }

    }
}
