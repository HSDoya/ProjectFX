using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 탭 창의 Stats 탭 내용을 조립한다.
//
// 입구가 둘이다.
//   - MenuWindowGenerator가 창을 처음 만들 때 Build()를 불러서 바로 채운다
//   - 창을 이미 만들어 둔 뒤라면 아래 메뉴로 Stats 탭만 따로 채운다
// 어느 쪽이든 패널을 만드는 코드는 Build() 하나뿐이다.
public static class StatPanelGenerator
{
    private static readonly Color PanelColor = new Color(0.16f, 0.16f, 0.18f);
    private static readonly Color RowColor = new Color(0.22f, 0.22f, 0.24f);
    private static readonly Color LabelColor = new Color(0.92f, 0.9f, 0.84f);
    private static readonly Color ValueColor = Color.white;
    private static readonly Color SubColor = new Color(0.62f, 0.62f, 0.6f);
    private static readonly Color PlusColor = new Color(0.3f, 0.55f, 0.32f);

    [MenuItem("Tools/UI 다시 만들기/스탯 탭만", false, 23)]
    public static void FillStatTab()
    {
        // using System 때문에 Object가 모호해지므로 UnityEngine.Object로 명시한다.
        var window = UnityEngine.Object.FindFirstObjectByType<MenuWindow>(FindObjectsInactive.Include);
        if (window == null)
        {
            Debug.LogError("[StatPanelGenerator] 씬에 MenuWindow가 없습니다. Tools/게임 UI 전체 생성(Main)을 먼저 실행하세요.");
            return;
        }

        int tabIndex = window.tabs.FindIndex(t => t != null && t.label == "Stats");
        if (tabIndex < 0)
        {
            Debug.LogError("[StatPanelGenerator] MenuWindow에 'Stats' 탭이 없습니다.");
            return;
        }

        var tab = window.tabs[tabIndex];

        if (tab.content != null && tab.content.GetComponent<StatPanelUI>() != null)
        {
            Debug.LogWarning("[StatPanelGenerator] Stats 탭에 이미 스탯 패널이 있습니다. 다시 만들려면 기존 패널을 지우고 실행하세요.");
            return;
        }

        // 자리만 잡아둔 플레이스홀더가 있으면 그 부모(콘텐츠 영역)를 쓰고 플레이스홀더는 지운다.
        Transform parent = tab.content != null ? tab.content.transform.parent : null;
        if (parent == null)
        {
            var contentTf = window.windowRoot != null ? window.windowRoot.transform.Find("Content") : null;
            parent = contentTf;
        }
        if (parent == null)
        {
            Debug.LogError("[StatPanelGenerator] 콘텐츠 영역(Content)을 찾지 못했습니다.");
            return;
        }

        if (tab.content != null) Undo.DestroyObjectImmediate(tab.content);

        var panel = Build(parent as RectTransform);
        Undo.RegisterCreatedObjectUndo(panel, "Create StatPanel");

        tab.content = panel;
        panel.SetActive(false); // 창은 닫힌 상태로 시작하므로 탭 내용도 꺼둔다

        EditorUtility.SetDirty(window);
        EditorSceneManager.MarkSceneDirty(window.gameObject.scene);

        Debug.Log("<color=green>성공!</color> Stats 탭에 스탯 패널을 넣었습니다. I로 창을 열고 Stats 탭을 보세요.");
    }

    // ───────────────────────── 패널 조립 ─────────────────────────

    /// <summary>스탯 패널을 만들어 돌려준다. 부모 영역을 꽉 채운다.</summary>
    public static GameObject Build(RectTransform parent)
    {
        var panel = EditorUIBuilder.CreateChildRect("StatPanel", parent);
        EditorUIBuilder.StretchFull(panel);
        panel.gameObject.AddComponent<Image>().color = PanelColor;

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 28, 28);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var ui = panel.gameObject.AddComponent<StatPanelUI>();
        ui.plusEnabledColor = PlusColor;

        // 남은 포인트
        var header = EditorUIBuilder.CreateChildRect("Header", panel);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
        var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 28;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandWidth = true;

        ui.statPointsText = EditorUIBuilder.CreateChildText("StatPoints", header, "Stat points : 0", 22,
                                                            LabelColor, TextAlignmentOptions.MidlineLeft);
        ui.statPointsText.fontStyle = FontStyles.Bold;
        ui.skillPointsText = EditorUIBuilder.CreateChildText("SkillPoints", header, "Skill points : 0", 22,
                                                             SubColor, TextAlignmentOptions.MidlineRight);

        // 스탯 6줄
        foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            ui.rows.Add(MakeRow(panel, stat));

        // 파생 수치 요약
        var derived = EditorUIBuilder.CreateChildText("Derived", panel, "", 17, SubColor, TextAlignmentOptions.MidlineLeft);
        derived.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        ui.derivedText = derived;

        var hint = EditorUIBuilder.CreateChildText("Hint", panel,
            "Level up grants 3 stat points and 1 skill point.", 15, new Color(0.45f, 0.45f, 0.45f),
            TextAlignmentOptions.MidlineLeft);
        hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;

        return panel.gameObject;
    }

    private static StatPanelUI.Row MakeRow(RectTransform parent, StatType stat)
    {
        var row = EditorUIBuilder.CreateChildRect("Row_" + stat, parent);
        row.gameObject.AddComponent<Image>().color = RowColor;

        var layoutElement = row.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 48;
        layoutElement.preferredHeight = 48;

        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 14, 6, 6);
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        // 스탯 이름. 한글 폰트를 넣으면 이 텍스트만 바꾸면 된다.
        var nameText = EditorUIBuilder.CreateChildText("Name", row, stat.ToString(), 21, LabelColor,
                                                        TextAlignmentOptions.MidlineLeft);
        nameText.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;

        var effectText = EditorUIBuilder.CreateChildText("Effect", row, EffectLabel(stat), 15, SubColor,
                                                          TextAlignmentOptions.MidlineLeft);
        effectText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var valueText = EditorUIBuilder.CreateChildText("Value", row, "10", 22, ValueColor,
                                                         TextAlignmentOptions.MidlineRight);
        valueText.fontStyle = FontStyles.Bold;
        valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;

        var plusImage = EditorUIBuilder.CreateChildImage("Plus", row, PlusColor, fixedWidth: 36);
        plusImage.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
        var plusButton = plusImage.gameObject.AddComponent<Button>();
        plusButton.targetGraphic = plusImage;

        var plusText = EditorUIBuilder.CreateChildText("+", plusImage.transform, "+", 24, Color.white,
                                                        TextAlignmentOptions.Center);
        EditorUIBuilder.StretchFull(plusText.rectTransform);

        return new StatPanelUI.Row
        {
            stat = stat,
            nameText = nameText,
            valueText = valueText,
            plusButton = plusButton
        };
    }

    // 각 스탯이 무엇을 올리는지. PlayerStats의 포인트당 값과 짝이므로 그쪽을 바꾸면 여기도 맞춰야 한다.
    private static string EffectLabel(StatType stat)
    {
        switch (stat)
        {
            case StatType.STR: return "Attack  +1 / point";
            case StatType.VIT: return "Max HP  +5 / point";
            case StatType.DEF: return "Defense  +2 / point";
            case StatType.AGI: return "Move speed  +2% / point";
            case StatType.END: return "Max stamina  +5 / point";
            case StatType.LUK: return "Drop chance  +1% / point";
            default: return string.Empty;
        }
    }
}
