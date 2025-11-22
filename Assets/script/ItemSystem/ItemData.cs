using UnityEngine;

public enum ItemType
{
    None,
    Consumable,   // 소비 아이템
    Equipment,    // 장비
    Material,     // 재료
    Etc           // 기타
}

// CSV의 equipSlot 컬럼과 1:1 매핑
public enum EquipmentSlotType
{
    None,
    Weapon,
    Armor,
    Hat,
    Shoes,
    Accessory
}

public class ItemData
{
    public string itemID;        // CSV: itemID
    public string displayName;   // CSV: Name
    public string description;   // CSV: Description
    public Sprite icon;

    public bool canStack = true; // CSV: canStack
    public int maxStackAmount = 1; // CSV: MaxStack

    // ▼ 새로 추가한 필드들 (CSV와 매핑)
    public ItemType itemType;        // CSV: ItemType
    public string type;              // CSV: type (세부 분류용 문자열, 예: Food, Material 등)
    public bool isConsumable;        // CSV: isConsumable

    public EquipmentSlotType equipSlot; // CSV: equipSlot
    public int atk;                  // CSV: atk
    public int def;                  // CSV: def
}

