using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EquipmentUI : MonoBehaviour
{
    [SerializeField] public Transform equipGrid;   // 오른쪽 2x4 그리드 부모

    private readonly List<ItemUI> slots = new();

    void Awake()
    {
        slots.Clear();
        // 2x4 그리드 안에 있는 ItemUI 전부 수집
        slots.AddRange(equipGrid.GetComponentsInChildren<ItemUI>(true));
    }

    void OnEnable()
    {
        if (EquipmentManager.instance == null)
        {
            Debug.LogWarning("EquipmentManager.instance 가 없습니다.");
            return;
        }

        EquipmentManager.instance.onEquipmentChangedCallback -= UpdateUI;
        EquipmentManager.instance.onEquipmentChangedCallback += UpdateUI;
        UpdateUI();   // 처음 켤 때 즉시 반영
    }

    void OnDisable()
    {
        if (EquipmentManager.instance == null) return;
        EquipmentManager.instance.onEquipmentChangedCallback -= UpdateUI;
    }

    public void UpdateUI()
    {
        // 먼저 모든 슬롯 비우기
        foreach (var s in slots)
            s.ClearSlot();

        if (EquipmentManager.instance == null) return;

        var equipped = EquipmentManager.instance.equippedItems; // 장비 데이터 배열/리스트

        // 인벤토리 UI 처럼, 장비 데이터를 슬롯에 그대로 매핑
        for (int i = 0; i < slots.Count && i < equipped.Length; i++)
        {
            var item = equipped[i];
            if (item != null)
                slots[i].SetItem(item);
        }
    }
}
