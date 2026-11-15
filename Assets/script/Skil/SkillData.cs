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

    // ───────────────────────── 해금 효과 ─────────────────────────
    //
    // 스킬이 올려주는 스탯. 스탯 포인트를 찍은 것과 같은 경로로 합산되므로
    // 최종 수치 계산식이 한 벌로 유지된다 (PlayerStats.Effective).
    //
    // 효과가 없는 스킬(진입점 등)은 bonusAmount를 0으로 두면 된다.
    [Header("해금 효과")]
    [Tooltip("해금 시 올려줄 스탯")]
    public StatType bonusStat = StatType.STR;

    [Tooltip("올려줄 포인트 수. 0이면 효과 없음(진입점 스킬 등).")]
    public int bonusAmount = 0;

    /// <summary>실제로 능력치를 올려주는 스킬인지.</summary>
    public bool HasBonus => bonusAmount > 0;
}
