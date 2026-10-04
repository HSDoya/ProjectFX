using UnityEngine;

public enum ItemType
{
    None,
    Consumable,   // 소비 아이템
    Equipment,    // 장비
    Material,     // 재료
    Etc,          // 기타
    Furniture     // 가구(월드에 설치하는 아이템)
}

// CSV의 equipSlot 컬럼과 1:1 매핑
public enum EquipmentSlotType
{
    None,
    Weapon,
    Armor,
    Hat,
    Shoes,
    Tool,         
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

    // 가구처럼 월드에 설치하는 아이템이 생성할 프리팹. 설치 대상이 아닌 아이템은 비어 있다.
    // 설치 시 세부 규칙(차지 칸 수, 통행 차단 여부 등)은 이 프리팹에 붙는 컴포넌트가 들고 있다.
    public GameObject placedPrefab;
}

