using UnityEngine;

public enum SkillCategory
{
    Experience,   // 경험치
    Combat,       // 전투
    Farming,      // 농사
    Development   // 발전 (테크트리/장비 등)
}

[CreateAssetMenu(fileName = "NewSkillData", menuName = "SkillTree/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillID;
    public string displayName;
    [TextArea]
    public string description;
    public SkillCategory category;
    public Sprite icon;

    [Header("Grid Position")]
    public int gridX;
    public int gridY;

    // 각 카테고리(사분면)의 진입점 스킬은 true - 게임 시작 시 포인트 소모 없이 자동 해금됨
    public bool unlockedByDefault;
}
