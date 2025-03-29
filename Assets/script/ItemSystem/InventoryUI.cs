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
        // 기존 슬롯들 받아서 저장 (Start에서 한 번만)
        foreach (Transform child in itemGrid)
        {
            ItemUI slot = child.GetComponent<ItemUI>();
            if (slot != null)
            {
                slots.Add(slot);
            }
        }

        Inventory.instance.onItemChangedCallback += UpdateUI;
    }

    public void UpdateUI()
    {
        // 모든 슬롯 초기화
        foreach (ItemUI slot in slots)
        {
            slot.ClearSlot();  // 아이콘 제거
        }

        // 현재 인벤토리 아이템만큼 슬롯에 표시
        for (int i = 0; i < Inventory.instance.items.Count; i++)
        {
            if (i < slots.Count)
            {
                slots[i].SetItem(Inventory.instance.items[i].data);
            }
        }
    }

}
