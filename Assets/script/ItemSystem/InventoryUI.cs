using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public Transform itemGrid;               // slot 오브젝트
    public GameObject itemSlotPrefab;        // 안 씀! (재사용 방식)

    private List<ItemUI> slots = new List<ItemUI>();

    void Start()
    {
        Debug.Log("InventoryUI Start() 실행됨");

        // 슬롯은 AutoSlotGenerator에서 InitSlots()로 초기화한다고 가정하고, 여기서는 생략 가능
        if (slots.Count == 0)
        {
            InitSlots();  // 만약 빠져 있으면 방어적으로 초기화
        }

        // 콜백 중복 방지 후 등록
        Inventory.instance.onItemChangedCallback -= UpdateUI;
        Inventory.instance.onItemChangedCallback += UpdateUI;

        // 최초 UI 강제 갱신
        UpdateUI();
    }

    public void UpdateUI()
    {
        // 슬롯 초기화
        foreach (ItemUI slot in slots)
        {
            slot.ClearSlot();
        }

        // 인벤토리 아이템 표시
        for (int i = 0; i < Inventory.instance.items.Count; i++)
        {
            if (i < slots.Count)
            {
                slots[i].SetItem(Inventory.instance.items[i]);
            }
        }
    }
    public void InitSlots()
    {
        slots.Clear();

        foreach (Transform child in itemGrid)
        {
            ItemUI slot = child.GetComponent<ItemUI>();
            if (slot != null)
            {
                slots.Add(slot);
            }
        }
    }

}
