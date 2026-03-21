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

// 유니티 메뉴에서 우클릭으로 생성할 수 있도록 속성 추가
[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject // ScriptableObject 상속으로 변경
{
    public string itemID;
    public string displayName;
    [TextArea] // 인스펙터에서 보기 편하게 속성 추가
    public string description;
    public Sprite icon;

    public bool canStack = true;
    public int maxStackAmount = 1;

    public ItemType itemType;
    public string type;
    public bool isConsumable;

    public EquipmentSlotType equipSlot;
    public int atk;
    public int def;
}

