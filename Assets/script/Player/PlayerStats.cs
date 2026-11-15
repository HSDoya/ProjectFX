using System;
using UnityEngine;

public enum StatType
{
    STR, // 공격력
    VIT, // 최대 체력
    DEF, // 방어력
    AGI, // 이동 속도
    END, // 최대 스태미너
    LUK  // 드랍 확률
}

// 플레이어가 찍은 스탯 포인트의 단일 소유자.
//
// 여기서는 "찍은 포인트"와 "남은 포인트"만 저장한다. 공격력·최대체력·방어력 같은 최종 수치는
// 저장하지 않고 매번 계산해서 돌려준다. 최종 수치를 들고 있으면 장비/스킬이 끼어들 때마다
// 같은 값이 두 곳에 생기고, 어느 쪽이 맞는지 알 수 없게 된다.
//
// 방어력은 기여자가 둘이다 (장비 def + DEF 스탯). 둘은 서로 다른 것을 소유하고,
// 합산은 PlayerMove.TotalDefense 한 곳에서만 한다.
public class PlayerStats : MonoBehaviour
{
    // 모든 스탯의 시작값. 보너스는 "이 값을 넘긴 만큼"만 붙으므로 1레벨에 공짜 보너스가 없다.
    public const int BaseValue = 10;

    [Header("레벨업 보상")]
    public int statPointsPerLevel = 3;
    public int skillPointsPerLevel = 1;

    [Header("스탯 상한")]
    [Tooltip("한 스탯에 찍을 수 있는 최대 포인트. 0 이하로 두면 상한이 없다. 표시값 상한은 기본값(10) + 이 값.")]
    public int maxAllocatedPerStat = 90;

    // 포인트 1점당 효과. 밸런스는 여기만 고치면 전부 반영된다.
    [Header("포인트당 효과")]
    public int attackPerStr = 1;
    public float healthPerVit = 5f;
    public int defensePerDef = 2;
    public float speedPercentPerAgi = 2f;
    public float staminaPerEnd = 5f;
    public float dropChancePerLuk = 1f;

    [Header("찍은 포인트 (StatType 순서)")]
    [SerializeField] private int[] allocated = new int[6];

    // 해금한 스킬이 주는 보너스의 합계. 소유자는 SkillTreeManager이고 여기는 통보받아 들고만 있는다.
    // 직접 더하지 않고 통째로 덮어쓰는 이유: 패널을 여닫을 때마다 다시 통보해도 중복 적용되지 않는다.
    [SerializeField] private int[] skillBonus = new int[6];

    [Header("남은 포인트")]
    [Tooltip("테스트용으로 인스펙터에서 올려도 된다. 정식 지급은 레벨업(GrantLevelUpPoints)이 한다.")]
    public int availableStatPoints = 0;
    [Tooltip("스킬트리에서 쓸 포인트. 소모는 SkillTreeManager가 SpendSkillPoint로 한다.")]
    public int availableSkillPoints = 0;

    /// <summary>스탯을 찍거나 포인트가 지급될 때. UI가 이걸 듣고 다시 그린다.</summary>
    public event Action OnStatsChanged;

    // 참조를 들고 다니지 않아도 물어볼 수 있게 (DropTable처럼 static에서 쓰는 곳이 있다).
    public static PlayerStats Instance { get; private set; }

    private static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

    private void Awake()
    {
        Instance = this;

        // StatType에 스탯을 추가해도 기존 저장값이 깨지지 않게 길이를 맞춰준다.
        allocated = Resize(allocated);
        skillBonus = Resize(skillBonus);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ───────────────────────── 조회 ─────────────────────────

    /// <summary>해당 스탯에 찍은 포인트 수.</summary>
    public int Allocated(StatType stat)
    {
        int i = (int)stat;
        return (allocated != null && i >= 0 && i < allocated.Length) ? allocated[i] : 0;
    }

    /// <summary>해금한 스킬이 올려준 포인트 수.</summary>
    public int SkillBonus(StatType stat)
    {
        int i = (int)stat;
        return (skillBonus != null && i >= 0 && i < skillBonus.Length) ? skillBonus[i] : 0;
    }

    /// <summary>
    /// 실제로 능력치 계산에 쓰이는 포인트 수 (찍은 것 + 스킬이 준 것).
    /// 파생 수치는 전부 이걸 통해 계산하므로, 보너스 출처가 늘어나도 공식은 한 곳만 고치면 된다.
    /// </summary>
    public int Effective(StatType stat) => Allocated(stat) + SkillBonus(stat);

    /// <summary>화면에 보이는 스탯 값 (기본 10 + 찍은 포인트 + 스킬 보너스).</summary>
    public int Value(StatType stat) => BaseValue + Effective(stat);

    /// <summary>이 스탯이 상한에 닿았는지. 상한이 0 이하면 항상 false.</summary>
    public bool AtCap(StatType stat) => maxAllocatedPerStat > 0 && Allocated(stat) >= maxAllocatedPerStat;

    /// <summary>지금 이 스탯에 1포인트를 더 찍을 수 있는지. UI가 버튼을 끄는 판단에 쓴다.</summary>
    public bool CanSpend(StatType stat) => availableStatPoints > 0 && !AtCap(stat);

    // ───────────────────────── 파생 수치 (저장하지 않는다) ─────────────────────────

    public int AttackBonus => Effective(StatType.STR) * attackPerStr;
    public float MaxHealthBonus => Effective(StatType.VIT) * healthPerVit;
    public int DefenseBonus => Effective(StatType.DEF) * defensePerDef;
    public float SpeedMultiplier => 1f + Effective(StatType.AGI) * speedPercentPerAgi / 100f;
    public float MaxStaminaBonus => Effective(StatType.END) * staminaPerEnd;
    public float DropChanceBonus => Effective(StatType.LUK) * dropChancePerLuk;

    // ───────────────────────── 포인트 사용 / 지급 ─────────────────────────

    /// <summary>스탯 1포인트 투자. 남은 포인트가 없으면 아무것도 하지 않고 false.</summary>
    public bool Spend(StatType stat)
    {
        if (availableStatPoints <= 0) return false;

        int i = (int)stat;
        if (allocated == null || i < 0 || i >= allocated.Length) return false;

        // 상한에 닿으면 포인트를 삼키지 않고 거절한다.
        if (AtCap(stat)) return false;

        allocated[i]++;
        availableStatPoints--;
        OnStatsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 스킬 1포인트 사용. 남은 포인트가 없으면 아무것도 하지 않고 false.
    ///
    /// 예전엔 SkillTreeManager가 skillPoints를 따로 들고 있었다(하드코딩 16). 그래서 레벨업으로
    /// 번 포인트와 트리에서 쓰는 포인트가 아예 다른 값이었다 - 레벨을 올려도 트리가 열리지 않고,
    /// 트리에서 다 써도 여기 숫자는 그대로였다. 포인트의 소유자는 여기 하나다.
    /// </summary>
    public bool SpendSkillPoint()
    {
        if (availableSkillPoints <= 0) return false;

        availableSkillPoints--;
        OnStatsChanged?.Invoke();
        return true;
    }

    /// <summary>레벨업 보상 지급. 경험치 시스템(Phase B)이 레벨이 오를 때 호출한다.</summary>
    public void GrantLevelUpPoints(int levels = 1)
    {
        if (levels <= 0) return;

        availableStatPoints += statPointsPerLevel * levels;
        availableSkillPoints += skillPointsPerLevel * levels;
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 찍은 스탯을 전부 되돌리고 포인트를 돌려준다. 돌려준 포인트 수를 반환한다.
    ///
    /// 스킬이 준 보너스는 스탯 포인트가 아니므로 건드리지 않는다 - 그건 스킬을 되돌려야 한다.
    /// </summary>
    public int ResetStats()
    {
        if (allocated == null) return 0;

        int refunded = 0;
        for (int i = 0; i < allocated.Length; i++)
        {
            refunded += allocated[i];
            allocated[i] = 0;
        }

        if (refunded <= 0) return 0;

        availableStatPoints += refunded;
        OnStatsChanged?.Invoke();
        return refunded;
    }

    /// <summary>
    /// 해금한 스킬들이 주는 보너스 합계를 통보받는다. SkillTreeManager가 유일한 호출자다.
    ///
    /// 더하는 게 아니라 통째로 덮어쓴다. 그래서 패널을 여닫으며 몇 번을 다시 통보해도
    /// 같은 결과가 되고, 스킬을 되돌리는 기능이 생겨도 이 함수만 다시 부르면 된다.
    /// </summary>
    public void SetSkillBonuses(int[] totals)
    {
        if (skillBonus == null) skillBonus = new int[StatCount];

        bool changed = false;
        for (int i = 0; i < skillBonus.Length; i++)
        {
            int v = (totals != null && i < totals.Length) ? totals[i] : 0;
            if (skillBonus[i] == v) continue;
            skillBonus[i] = v;
            changed = true;
        }

        if (changed) OnStatsChanged?.Invoke();
    }

    /// <summary>테스트용. 인스펙터에서 남은 포인트를 직접 바꿨을 때 UI를 갱신시킨다.</summary>
    public void NotifyChanged() => OnStatsChanged?.Invoke();

    // StatType이 늘어나도 기존 값이 유지되도록 길이만 맞춰준다.
    private static int[] Resize(int[] source)
    {
        if (source != null && source.Length == StatCount) return source;

        var resized = new int[StatCount];
        if (source != null) Array.Copy(source, resized, Mathf.Min(source.Length, StatCount));
        return resized;
    }
}
