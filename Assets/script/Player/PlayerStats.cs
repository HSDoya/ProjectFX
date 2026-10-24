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
        if (allocated == null || allocated.Length != StatCount)
        {
            var resized = new int[StatCount];
            if (allocated != null)
                Array.Copy(allocated, resized, Mathf.Min(allocated.Length, StatCount));
            allocated = resized;
        }
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

    /// <summary>화면에 보이는 스탯 값 (기본 10 + 찍은 포인트).</summary>
    public int Value(StatType stat) => BaseValue + Allocated(stat);

    // ───────────────────────── 파생 수치 (저장하지 않는다) ─────────────────────────

    public int AttackBonus => Allocated(StatType.STR) * attackPerStr;
    public float MaxHealthBonus => Allocated(StatType.VIT) * healthPerVit;
    public int DefenseBonus => Allocated(StatType.DEF) * defensePerDef;
    public float SpeedMultiplier => 1f + Allocated(StatType.AGI) * speedPercentPerAgi / 100f;
    public float MaxStaminaBonus => Allocated(StatType.END) * staminaPerEnd;
    public float DropChanceBonus => Allocated(StatType.LUK) * dropChancePerLuk;

    // ───────────────────────── 포인트 사용 / 지급 ─────────────────────────

    /// <summary>스탯 1포인트 투자. 남은 포인트가 없으면 아무것도 하지 않고 false.</summary>
    public bool Spend(StatType stat)
    {
        if (availableStatPoints <= 0) return false;

        int i = (int)stat;
        if (allocated == null || i < 0 || i >= allocated.Length) return false;

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

    /// <summary>테스트용. 인스펙터에서 남은 포인트를 직접 바꿨을 때 UI를 갱신시킨다.</summary>
    public void NotifyChanged() => OnStatsChanged?.Invoke();
}
