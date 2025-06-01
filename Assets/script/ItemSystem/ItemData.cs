using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string displayName; // 아이템 이름
    public string description; // 아이템 설명
    public Sprite icon; // 아이템 아이콘
    public string itemID; //아이템 식별 ID
    
    
    
    
    [Header("설정")]
    public bool canStack = true; // 중첩 가능 여부
    public int maxStackAmount = 1; // 최대 중첩 개수
}

