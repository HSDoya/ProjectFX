using System.Collections.Generic;
using UnityEngine;

// 화면 하단에 항상 떠 있는 HUD 퀵슬롯 바.
//
// 예전엔 이 역할이 InventoryUI 안에 같이 들어 있었다. 그런데 InventoryUI는 인벤토리 창 패널
// 위에 붙어 있어서, 창을 탭 창 안으로 옮겨 "닫힌 상태로 시작"하게 되자 Awake가 아예 실행되지
// 않았고(비활성 오브젝트는 Awake가 호출되지 않는다) 퀵슬롯 바가 통째로 비었다.
//
// 한 컴포넌트가 수명이 다른 두 UI(열릴 때만 보이는 창 / 항상 보이는 HUD)를 같이 담당하면
// 이런 함정이 반복된다. 그래서 HUD 쪽을 떼어내 항상 켜져 있는 오브젝트에 둔다.
public class QuickSlotBarUI : MonoBehaviour
{
    [Header("슬롯")]
    [Tooltip("퀵슬롯 칸들이 들어 있는 부모. 비우면 이 오브젝트 자신을 쓴다.")]
    [SerializeField] private Transform slotGrid;

    private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
    private Inventory subscribedInventory;

    public static QuickSlotBarUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        // 꺼져 있는 자식까지 포함해서 모은다(바가 숨겨진 상태에서도 목록은 있어야 한다).
        Transform grid = slotGrid != null ? slotGrid : transform;
        slots.Clear();
        slots.AddRange(grid.GetComponentsInChildren<InventorySlotUI>(true));

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].slotIndex = i;
            slots[i].isQuickSlot = true;
        }

        // Inventory.instance가 아직 비어 있는 순간에 Awake가 돌 수 있어 직접 찾아 쥐고,
        // 그 참조로 해제까지 한다.
        subscribedInventory = Inventory.instance != null ? Inventory.instance : FindFirstObjectByType<Inventory>();
        if (subscribedInventory != null)
        {
            subscribedInventory.onItemChangedCallback -= Refresh;
            subscribedInventory.onItemChangedCallback += Refresh;
        }
    }

    private void Start()
    {
        // 모든 Awake가 끝난 뒤라 Inventory.instance가 확실히 존재한다.
        Refresh();
    }

    private void OnDestroy()
    {
        if (subscribedInventory != null)
            subscribedInventory.onItemChangedCallback -= Refresh;

        if (Instance == this) Instance = null;
    }

    public void Refresh()
    {
        if (Inventory.instance == null) return;

        var quickSlots = Inventory.instance.quickSlots;
        if (quickSlots == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            slots[i].BindItem(i < quickSlots.Length ? quickSlots[i] : null);
        }
    }

    /// <summary>
    /// 쿨타임 오버레이처럼 특정 칸 위에 뭔가를 띄워야 할 때 쓴다.
    /// 슬롯 목록 수집은 이 클래스가 소유하고, 밖에서는 조회만 하게 한다.
    /// </summary>
    public RectTransform GetSlotRect(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index] != null ? slots[index].transform as RectTransform : null;
    }
}
