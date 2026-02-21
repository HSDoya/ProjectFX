using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;


public class Inventory : MonoBehaviour
{
    #region Singleton
    public static Inventory instance;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of Inventory found!");
            Destroy(gameObject);
            return;
        }
        instance = this;

        // 배열 초기화 (초기값은 전부 null)
        items = new Item[INVENTORY_SIZE];
        quickSlots = new Item[QUICK_SLOT_SIZE];
    }
    #endregion

    [Header("UI Reference")]
    [SerializeField] private GameObject inventoryUI; // 인벤토리 UI 전체 부모

    [Header("Inventory Settings")]
    // 이미지에 맞춘 14x5 설정
    public const int COL_COUNT = 14;
    public const int ROW_COUNT = 5;
    public const int INVENTORY_SIZE = 70; // 14 * 5

    [Header("QuickSlot Settings")]
    public const int QUICK_SLOT_SIZE = 14;

    [Header("Data")]
    // ★ List 대신 배열 사용 (빈칸 유지를 위해 필수)
    public Item[] items;       // 메인 인벤토리 (70칸)
    public Item[] quickSlots;  // 퀵슬롯 (14칸)

    public bool isInventoryOpen = false;

    // UI 갱신을 위한 이벤트
    public event Action onItemChangedCallback;
    public void RefreshUI()
    {
        onItemChangedCallback?.Invoke();
    }

    private void Start()
    {
        // UI 초기 상태 설정
        if (inventoryUI != null)
            inventoryUI.SetActive(isInventoryOpen);

        // --- 테스트 아이템 지급 (테스트 후 삭제하세요) ---
        if (ItemDataCsvLoader.instance != null)
        {
            var wood = ItemDataCsvLoader.instance.GetItemDataByID("wood");
            var armor = ItemDataCsvLoader.instance.GetItemDataByID("Breastplate");
            var sword = ItemDataCsvLoader.instance.GetItemDataByID("Sword");
            if (wood != null)
            {
                // 나무 99개 넣기
                AddItem(new Item(wood, 99));
            }
            if (armor != null)
            {
                // 갑옷 1개 넣기
                AddItem(new Item(armor, 1));
            }

            if (sword != null)
            {
                // 갑옷 1개 넣기
                AddItem(new Item(sword, 1));
            }
        }
        // ---------------------------------------------
    }

    private void Update()
    {
        
    }

    /// <summary>
    /// 아이템 획득 메서드 (메인 인벤토리 우선)
    /// </summary>
    public bool AddItem(Item newItem)
    {
        if (newItem == null || newItem.data == null) return false;

        // 1. 스택 가능한 아이템이면 기존에 있는 것과 합치기 시도
        if (newItem.data.canStack)
        {
            for (int i = 0; i < items.Length; i++)
            {
                // 아이템이 존재하고 ID가 같다면
                if (items[i] != null && items[i].data.itemID == newItem.data.itemID)
                {
                    // 수량 증가 (Item 클래스의 AddQuantity 로직에 따라 최대치 처리 필요)
                    // 여기서는 단순하게 합칩니다. (초과분 처리 로직은 Item.cs에 따라 다름)
                    items[i].AddQuantity(newItem.quantity);

                    onItemChangedCallback?.Invoke();
                    return true;
                }
            }
        }

        // 2. 빈칸(null)을 찾아서 넣기
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = newItem;
                onItemChangedCallback?.Invoke();
                return true;
            }
        }

        // 3. 인벤토리가 가득 참
        Debug.Log("Inventory is full.");
        return false;
    }

    /// <summary>
    /// 특정 인덱스의 아이템을 제거 (UI 드래그나 버리기 등에서 사용)
    /// </summary>
    public void RemoveAt(int index)
    {
        if (IsIndexValid(index))
        {
            items[index] = null; // 리스트 삭제가 아니라 null로 비움
            onItemChangedCallback?.Invoke();
        }
    }
    public void ToggleUI()
    {
        // 1. 상태 값 반전 (True <-> False)
        isInventoryOpen = !isInventoryOpen;

        // 2. 실제 UI 오브젝트 켜고 끄기
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(isInventoryOpen);
        }

        // 3. (선택사항) 인벤토리가 켜질 때 UI 내용도 최신화하고 싶다면
        if (isInventoryOpen) RefreshUI(); 
    }

    /// <summary>
    /// 인벤 특정 index에서 1개를 꺼냄 (장착 등에 사용)
    /// - 수량이 많으면 1개 줄이고, 1개면 null로 만듦
    /// </summary>
    public bool TryTakeOneAt(int index, out Item taken)
    {
        taken = null;

        if (!IsIndexValid(index)) return false;

        var src = items[index];
        if (src == null) return false;

        // 스택 분리 (수량이 1보다 크고 스택 가능할 때)
        if (src.data.canStack && src.quantity > 1)
        {
            src.RemoveQuantity(1);
            taken = new Item(src.data, 1);
            onItemChangedCallback?.Invoke();
            return true;
        }

        // 통째로 꺼내기 (슬롯을 비움)
        items[index] = null;
        taken = src;
        onItemChangedCallback?.Invoke();
        return true;
    }

    // 인덱스 유효성 검사 헬퍼
    private bool IsIndexValid(int index)
    {
        return index >= 0 && index < items.Length;
    }

    // 편의용: Data만으로 추가
    public bool Add(ItemData data)
    {
        return AddItem(new Item(data));
    }
}
