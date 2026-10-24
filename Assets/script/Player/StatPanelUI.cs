using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 탭 창의 Stats 탭 내용. 스탯을 보여주고 남은 포인트로 찍게 한다.
//
// 값을 따로 들고 있지 않고 PlayerStats에서 매번 읽으므로 화면과 실제 값이 어긋날 수 없다.
// (건설 현장 재료 패널과 같은 방침이다.)
//
// 라벨이 영문인 이유: TMP 기본 폰트(LiberationSans)에 한글 글리프가 없다. 한글 폰트를 넣으면
// 인스펙터에서 각 줄의 이름 텍스트만 바꾸면 된다.
public class StatPanelUI : MonoBehaviour
{
    [Serializable]
    public class Row
    {
        public StatType stat;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI valueText;
        public Button plusButton;
    }

    [Header("구성")]
    public List<Row> rows = new List<Row>();
    public TextMeshProUGUI statPointsText;
    public TextMeshProUGUI skillPointsText;
    [Tooltip("스탯이 실제로 무엇을 바꾸는지 보여주는 요약")]
    public TextMeshProUGUI derivedText;

    [Header("색")]
    public Color plusEnabledColor = new Color(0.3f, 0.55f, 0.32f);
    public Color plusDisabledColor = new Color(0.3f, 0.3f, 0.3f);

    private PlayerStats stats;
    private PlayerMove player;

    private void Awake()
    {
        foreach (var row in rows)
        {
            if (row == null || row.plusButton == null) continue;

            var captured = row.stat; // 람다가 루프 변수를 잡으면 전부 마지막 값이 된다
            row.plusButton.onClick.AddListener(() => OnPlusClicked(captured));
        }
    }

    private void OnEnable()
    {
        Resolve();

        if (stats != null)
        {
            stats.OnStatsChanged -= Refresh;
            stats.OnStatsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnStatsChanged -= Refresh;
    }

    // PlayerStats는 런타임에 PlayerMove가 붙이므로, 패널이 먼저 깨어났을 수 있다.
    // 그래서 보여질 때마다 다시 찾아본다.
    private void Resolve()
    {
        if (stats == null) stats = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
        if (player == null) player = FindFirstObjectByType<PlayerMove>();
    }

    private void OnPlusClicked(StatType stat)
    {
        Resolve();
        if (stats == null) return;

        stats.Spend(stat); // 남은 포인트가 없으면 아무것도 하지 않는다. 갱신은 OnStatsChanged가 처리한다.
    }

    private void Refresh()
    {
        Resolve();

        bool canSpend = stats != null && stats.availableStatPoints > 0;

        foreach (var row in rows)
        {
            if (row == null) continue;

            if (row.nameText != null) row.nameText.text = row.stat.ToString();

            if (row.valueText != null)
            {
                int value = stats != null ? stats.Value(row.stat) : PlayerStats.BaseValue;
                int spent = stats != null ? stats.Allocated(row.stat) : 0;
                // 찍은 포인트가 있으면 "10 (+3)" 형태로 기본값과 투자분을 구분해서 보여준다.
                row.valueText.text = spent > 0 ? $"{value}  (+{spent})" : value.ToString();
            }

            if (row.plusButton != null)
            {
                row.plusButton.interactable = canSpend;
                if (row.plusButton.targetGraphic != null)
                    row.plusButton.targetGraphic.color = canSpend ? plusEnabledColor : plusDisabledColor;
            }
        }

        if (statPointsText != null)
            statPointsText.text = stats != null ? $"Stat points : {stats.availableStatPoints}" : "Stat points : -";

        if (skillPointsText != null)
            skillPointsText.text = stats != null ? $"Skill points : {stats.availableSkillPoints}" : "Skill points : -";

        if (derivedText != null) derivedText.text = BuildDerivedText();
    }

    // 스탯이 실제로 바꾼 값을 그대로 읽어서 보여준다. 여기서 다시 계산하면 공식이 두 벌이 된다.
    private string BuildDerivedText()
    {
        if (player == null) return string.Empty;

        var parts = new List<string>
        {
            $"HP {player.MaxHealth:0}",
            $"Stamina {player.MaxStamina:0}",
            $"DEF {player.TotalDefense}  ({player.DamageReductionPercent:0}% cut)",
            $"Speed {player.Speed:0.0}"
        };

        if (stats != null)
        {
            parts.Insert(2, $"ATK +{stats.AttackBonus}");
            parts.Add($"Drop +{stats.DropChanceBonus:0}%");
        }

        return string.Join("    ", parts);
    }
}
