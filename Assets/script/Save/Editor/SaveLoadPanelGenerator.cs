using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 탭 창에 Save 탭을 넣는다.
//
// 다른 탭 제너레이터(StatPanelGenerator 등)와 달리 <b>탭 자체가 없을 수도 있다</b>.
// MenuWindowGenerator가 창을 만들 때 Items/Craft/Stats/Skills 네 개만 만들었기 때문이다.
// 그래서 탭이 없으면 탭 버튼까지 만들어 끝에 붙인다.
//
// "게임 UI 전체 생성"을 다시 돌리지 않는 것이 요점이다. 전체 생성은 모든 UI의 fileID를
// 새로 발급해서 씬 diff가 수백 줄로 불어나고, 손으로 맞춰둔 참조가 끊긴다.
// 이 메뉴는 Save 탭 하나만 건드린다.
public static class SaveLoadPanelGenerator
{
    private const string TabLabel = "Save";

    private static readonly Color PanelColor = new Color(0.16f, 0.16f, 0.18f);
    private static readonly Color RowColor = new Color(0.22f, 0.22f, 0.24f);
    private static readonly Color LabelColor = new Color(0.92f, 0.9f, 0.84f);
    private static readonly Color SubColor = new Color(0.62f, 0.62f, 0.6f);
    private static readonly Color SaveColor = new Color(0.3f, 0.45f, 0.55f);
    private static readonly Color LoadColor = new Color(0.3f, 0.52f, 0.34f);
    private static readonly Color DeleteColor = new Color(0.46f, 0.3f, 0.3f);
    private static readonly Color DimColor = new Color(0.05f, 0.05f, 0.06f, 0.94f);
    private static readonly Color BoxColor = new Color(0.2f, 0.2f, 0.23f);

    private const float RowHeight = 64f;

    [MenuItem("Tools/UI 다시 만들기/저장 탭만", false, 25)]
    public static void FillSaveTab()
    {
        // using System이 없어도 Object는 UnityEngine.Object로 잡히지만, 다른 제너레이터와
        // 형태를 맞춰 명시한다.
        var window = UnityEngine.Object.FindFirstObjectByType<MenuWindow>(FindObjectsInactive.Include);
        if (window == null)
        {
            Debug.LogError("[SaveLoadPanelGenerator] 씬에 MenuWindow가 없습니다. " +
                           "Tools/게임 UI 전체 생성(Main)을 먼저 실행하세요.");
            return;
        }

        if (window.windowRoot == null)
        {
            Debug.LogError("[SaveLoadPanelGenerator] MenuWindow의 windowRoot가 비어 있습니다.");
            return;
        }

        var content = window.windowRoot.transform.Find("Content") as RectTransform;
        var tabBar = window.windowRoot.transform.Find("TabBar") as RectTransform;
        if (content == null || tabBar == null)
        {
            Debug.LogError("[SaveLoadPanelGenerator] 창 안에서 Content 또는 TabBar를 찾지 못했습니다.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fill Save Tab");

        int index = window.tabs.FindIndex(t => t != null && t.label == TabLabel);

        // 이미 있으면 내용만 새로 만든다. 탭 버튼은 그대로 두어 순서가 바뀌지 않게 한다.
        if (index >= 0 && window.tabs[index].content != null)
            Undo.DestroyObjectImmediate(window.tabs[index].content);

        GameObject panel = Build(content);
        Undo.RegisterCreatedObjectUndo(panel, "Create SaveLoadPanel");
        panel.SetActive(false); // 창은 닫힌 상태로 시작한다

        if (index >= 0)
        {
            window.tabs[index].content = panel;
        }
        else
        {
            // 탭 버튼까지 새로 만들어 맨 끝에 붙인다. MenuWindow.Awake가 인덱스로 클릭을 묶으므로
            // 리스트에 넣는 순서가 곧 화면 순서다.
            window.tabs.Add(MenuWindowGenerator.MakeTab(tabBar, TabLabel, panel));
        }

        EditorUtility.SetDirty(window);
        EditorSceneManager.MarkSceneDirty(window.gameObject.scene);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"<color=green>성공!</color> Save 탭을 {(index >= 0 ? "다시 만들었습니다" : "추가했습니다")}. " +
                  "I로 창을 열고 Save 탭을 보세요. 확인했으면 씬을 저장하세요. (되돌리려면 Ctrl+Z)");
    }

    // ───────────────────────── 패널 조립 ─────────────────────────

    /// <summary>저장/불러오기 패널을 만들어 돌려준다. 부모 영역을 꽉 채운다.</summary>
    public static GameObject Build(RectTransform parent)
    {
        var panel = EditorUIBuilder.CreateChildRect("SaveLoadPanel", parent);
        EditorUIBuilder.StretchFull(panel);
        panel.gameObject.AddComponent<Image>().color = PanelColor;

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 28, 28);
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var ui = panel.gameObject.AddComponent<SaveLoadPanelUI>();

        // 제목
        var title = EditorUIBuilder.CreateChildText("Title", panel, "Save / Load", 24, LabelColor,
                                                     TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;

        // 슬롯 줄
        for (int slot = 0; slot < SaveManager.SlotCount; slot++)
            ui.rows.Add(MakeSlotRow(panel, slot));

        // 결과 알림. 빈 문자열로 시작하므로 처음에는 아무것도 보이지 않는다.
        var status = EditorUIBuilder.CreateChildText("Status", panel, string.Empty, 17, SubColor,
                                                      TextAlignmentOptions.MidlineLeft);
        status.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        ui.statusText = status;

        // 저장 위치 안내. 파일이 어디 있는지 몰라 헤매는 일을 막는다.
        var hint = EditorUIBuilder.CreateChildText("Hint", panel,
            "Saves are stored in the game's persistent data folder.", 14,
            new Color(0.45f, 0.45f, 0.45f), TextAlignmentOptions.MidlineLeft);
        hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;

        BuildConfirm(panel, ui);

        return panel.gameObject;
    }

    private static SaveLoadPanelUI.SlotRow MakeSlotRow(RectTransform parent, int slot)
    {
        var row = EditorUIBuilder.CreateChildRect("Row_Slot" + slot, parent);
        row.gameObject.AddComponent<Image>().color = RowColor;

        var layoutElement = row.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = RowHeight;
        layoutElement.preferredHeight = RowHeight;

        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 14, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var titleText = EditorUIBuilder.CreateChildText("Name", row, $"Slot {slot + 1}", 21, LabelColor,
                                                         TextAlignmentOptions.MidlineLeft);
        titleText.fontStyle = FontStyles.Bold;
        titleText.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;

        // 실제 문구는 SaveLoadPanelUI.Refresh가 파일을 읽어 채운다.
        var infoText = EditorUIBuilder.CreateChildText("Info", row, "Empty", 17, SubColor,
                                                        TextAlignmentOptions.MidlineLeft);
        infoText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        return new SaveLoadPanelUI.SlotRow
        {
            slot = slot,
            titleText = titleText,
            infoText = infoText,
            saveButton = MakeButton(row, "Save", SaveColor, 104),
            loadButton = MakeButton(row, "Load", LoadColor, 104),
            deleteButton = MakeButton(row, "Delete", DeleteColor, 104)
        };
    }

    // 확인 창. 패널 전체를 덮어서 뒤쪽 버튼 클릭을 막는다.
    //
    // LayoutElement.ignoreLayout이 꼭 필요하다. 부모가 VerticalLayoutGroup이라
    // 그냥 두면 덮개가 슬롯 줄 아래에 한 칸으로 끼어들고, 켜고 끌 때마다 패널 전체가 출렁인다.
    private static void BuildConfirm(RectTransform parent, SaveLoadPanelUI ui)
    {
        var dim = EditorUIBuilder.CreateChildRect("Confirm", parent);
        dim.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        EditorUIBuilder.StretchFull(dim);

        // raycastTarget이 기본으로 켜져 있어 이 이미지가 뒤쪽 클릭을 막아준다.
        dim.gameObject.AddComponent<Image>().color = DimColor;

        var box = EditorUIBuilder.CreateChildRect("Box", dim);
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
        box.pivot = new Vector2(0.5f, 0.5f);
        box.sizeDelta = new Vector2(720f, 240f);
        box.anchoredPosition = Vector2.zero;
        box.gameObject.AddComponent<Image>().color = BoxColor;

        var boxLayout = box.gameObject.AddComponent<VerticalLayoutGroup>();
        boxLayout.padding = new RectOffset(28, 28, 28, 24);
        boxLayout.spacing = 20;
        boxLayout.childAlignment = TextAnchor.MiddleCenter;
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = true;
        boxLayout.childForceExpandWidth = true;
        boxLayout.childForceExpandHeight = false;

        var message = EditorUIBuilder.CreateChildText("Message", box, string.Empty, 19, LabelColor,
                                                       TextAlignmentOptions.Center);
        message.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

        var buttons = EditorUIBuilder.CreateChildRect("Buttons", box);
        buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
        var buttonLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 16;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = true;

        ui.confirmRoot = dim.gameObject;
        ui.confirmText = message;
        ui.confirmYesButton = MakeButton(buttons, "Yes", DeleteColor, 150);
        ui.confirmNoButton = MakeButton(buttons, "Cancel", new Color(0.35f, 0.35f, 0.38f), 150);

        dim.gameObject.SetActive(false);
    }

    private static Button MakeButton(RectTransform parent, string label, Color color, float width)
    {
        var image = EditorUIBuilder.CreateChildImage("Btn_" + label, parent, color, fixedWidth: width);

        var layoutElement = image.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        layoutElement.flexibleWidth = 0;

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var text = EditorUIBuilder.CreateChildText("Label", image.transform, label, 17, Color.white,
                                                    TextAlignmentOptions.Center);
        EditorUIBuilder.StretchFull(text.rectTransform);

        // 글자가 클릭을 가로채면 버튼이 안 눌린다.
        text.raycastTarget = false;

        return button;
    }
}
