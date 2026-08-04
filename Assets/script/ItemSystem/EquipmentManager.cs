using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager instance;

    // slotType별 착용 아이템
    private readonly Dictionary<EquipmentSlotType, Item> equipped = new();

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one EquipmentManager found!");
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public Item GetEquipped(EquipmentSlotType slotType)
    {
        equipped.TryGetValue(slotType, out var item);
        return item;
    }

    /// <summary>
    /// 해당 슬롯에 장착(교체 포함). 기존 장비는 반환.
    /// </summary>
    public Item Equip(EquipmentSlotType slotType, Item newItem)
    {
        if (newItem == null || newItem.data == null) return null;
        if (newItem.data.equipSlot != slotType) return newItem; // 슬롯 타입 안 맞으면 아이템 반환

        Item previous = null;

        // 이미 장착중인 아이템이 있으면 뺌
        if (equipped.TryGetValue(slotType, out var old))
        {
            previous = old;
        }

        // 새 아이템 장착
        equipped[slotType] = newItem;

        // ★ 이벤트 호출 필수 (이게 없으면 UI 안 바뀜)
        OnEquipmentChanged?.Invoke();

        return previous;
    }

    public Item Unequip(EquipmentSlotType slotType)
    {
        if (!equipped.TryGetValue(slotType, out var old))
            return null;

        equipped.Remove(slotType);
        OnEquipmentChanged?.Invoke();
        return old;
    }

    public bool HasEquipped(EquipmentSlotType slotType)
        => equipped.ContainsKey(slotType);

    // 방어구로 취급하는 슬롯들 (여기에 새 슬롯을 추가하면 GetTotalDefense()가 자동으로 합산)
    private static readonly EquipmentSlotType[] ArmorSlots =
    {
        EquipmentSlotType.Armor,
        EquipmentSlotType.Hat,
        EquipmentSlotType.Shoes,
        EquipmentSlotType.Accessory,
    };

    /// <summary>
    /// 방어구 슬롯(Armor/Hat/Shoes/Accessory)에 장착된 아이템의 def 합계
    /// </summary>
    public int GetTotalDefense()
    {
        int total = 0;
        foreach (var slot in ArmorSlots)
        {
            if (equipped.TryGetValue(slot, out var item) && item?.data != null)
                total += item.data.def;
        }
        return total;
    }
}
