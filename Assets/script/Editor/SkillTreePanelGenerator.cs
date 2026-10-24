using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 탭 창의 Skills 탭 내용을 조립한다.
//
// 예전에는 Skills만 "씬 전환 탭"이었다. SceneManager.LoadScene이 단일 모드라 Main이 통째로
// 언로드됐고, 세이브가 없으니 인벤토리/레벨/건설이 전부 날아갔다. 게다가 SkillTree 씬에는
// 돌아올 버튼이 없었다. (실제로는 Build Settings에 그 씬이 등록돼 있지 않아 탭이 아무 동작도
// 하지 않았고, 그 덕분에 사고가 나지 않고 있었을 뿐이다.)
//
// 이제 다른 탭들과 똑같이 Content 안의 패널로 만든다. 씬 전환이 없으니 상태가 날아가지 않고,
// 같은 씬이라 PlayerStats의 스킬 포인트를 바로 쓸 수 있다.
//
// 입구가 둘인 것은 StatPanelGenerator와 같다 - 조립 코드는 Build() 하나뿐이다.
public static class SkillTreePanelGenerator
{
    public const string NodePrefabPath = "Assets/All_Asset/asset/skill/SkillNodePrefab.prefab";
    public const string DatabasePath = "Assets/All_Asset/asset/skill/SkillDatabase.asset";

    private static readonly Color PanelColor = new Color(0.16f, 0.16f, 0.18f);
    private static readonly Color LabelColor = new Color(0.92f, 0.9f, 0.84f);
    private static readonly Color SubColor = new Color(0.62f, 0.62f, 0.6f);

    private const float HeaderHeight = 44f;
    private const float Pad = 28f;

    [MenuItem("Tools/UI 다시 만들기/스킬 탭만", false, 24)]
    public static void FillSkillTab()
    {
        var window = UnityEngine.Object.FindFirstObjectByType<MenuWindow>(FindObjectsInactive.Include);
        if (window == null)
        {
            Debug.LogError("[SkillTreePanelGenerator] 씬에 MenuWindow가 없습니다. Tools/게임 UI 전체 생성(Main)을 먼저 실행하세요.");
            return;
        }

        int tabIndex = window.tabs.FindIndex(t => t != null && t.label == "Skills");
        if (tabIndex < 0)
        {
            Debug.LogError("[SkillTreePanelGenerator] MenuWindow에 'Skills' 탭이 없습니다.");
            return;
        }

        var tab = window.tabs[tabIndex];
        if (tab.content != null && tab.content.GetComponent<SkillTreeManager>() != null)
        {
            Debug.LogWarning("[SkillTreePanelGenerator] Skills 탭에 이미 스킬 패널이 있습니다. 다시 만들려면 기존 패널을 지우고 실행하세요.");
            return;
        }

        Transform parent = tab.content != null ? tab.content.transform.parent : null;
        if (parent == null && window.windowRoot != null) parent = window.windowRoot.transform.Find("Content");
        if (parent == null)
        {
            Debug.LogError("[SkillTreePanelGenerator] 콘텐츠 영역(Content)을 찾지 못했습니다.");
            return;
        }

        if (tab.content != null) Undo.DestroyObjectImmediate(tab.content);

        var panel = Build(parent as RectTransform);
        Undo.RegisterCreatedObjectUndo(panel, "Create SkillTreePanel");

        tab.content = panel;
        tab.sceneToLoad = string.Empty; // 더 이상 씬 전환 탭이 아니다
        panel.SetActive(false);

        EditorUtility.SetDirty(window);
        EditorSceneManager.MarkSceneDirty(window.gameObject.scene);

        Debug.Log("<color=green>성공!</color> Skills 탭에 스킬 트리를 넣었습니다. 이제 씬이 전환되지 않습니다.");
    }

    // ───────────────────────── 패널 조립 ─────────────────────────

    /// <summary>스킬 트리 패널을 만들어 돌려준다. 부모 영역을 꽉 채운다.</summary>
    public static GameObject Build(RectTransform parent)
    {
        var panel = EditorUIBuilder.CreateChildRect("SkillTreePanel", parent);
        EditorUIBuilder.StretchFull(panel);
        panel.gameObject.AddComponent<Image>().color = PanelColor;

        // 남은 포인트
        var points = EditorUIBuilder.CreateChildText("SkillPoints", panel, "Skill points : 0", 22,
                                                      LabelColor, TextAlignmentOptions.MidlineLeft);
        points.fontStyle = FontStyles.Bold;
        points.raycastTarget = false;
        points.rectTransform.anchorMin = new Vector2(0f, 1f);
        points.rectTransform.anchorMax = new Vector2(1f, 1f);
        points.rectTransform.pivot = new Vector2(0.5f, 1f);
        points.rectTransform.offsetMin = new Vector2(Pad, -(Pad + HeaderHeight));
        points.rectTransform.offsetMax = new Vector2(-Pad, -Pad);

        var hint = EditorUIBuilder.CreateChildText("Hint", panel,
            "Unlock a node next to one you already have. Each unlock costs 1 skill point.", 15,
            new Color(0.45f, 0.45f, 0.45f), TextAlignmentOptions.MidlineLeft);
        hint.raycastTarget = false;
        hint.rectTransform.anchorMin = new Vector2(0f, 0f);
        hint.rectTransform.anchorMax = new Vector2(1f, 0f);
        hint.rectTransform.pivot = new Vector2(0.5f, 0f);
        hint.rectTransform.offsetMin = new Vector2(Pad, Pad);
        hint.rectTransform.offsetMax = new Vector2(-Pad, Pad + 24f);

        // 노드가 놓이는 영역. 레이아웃 그룹을 붙이면 안 된다 - SkillTreeManager가 gridX/gridY로
        // 직접 좌표를 잡는데, GridLayoutGroup이 있으면 그 좌표를 매 프레임 덮어쓴다.
        // (기존 SkillTree 씬의 SkillTreePanel에 실제로 GridLayoutGroup이 붙어 있어서
        //  스킬 좌표가 통째로 무시되고 있었다.)
        var container = EditorUIBuilder.CreateChildRect("NodeContainer", panel);
        container.anchorMin = container.anchorMax = new Vector2(0.5f, 0.5f);
        container.pivot = new Vector2(0.5f, 0.5f);
        container.anchoredPosition = Vector2.zero;
        container.sizeDelta = new Vector2(400f, 560f); // SkillTreeManager가 실제 범위에 맞춰 다시 잡는다

        var manager = panel.gameObject.AddComponent<SkillTreeManager>();
        WireManager(manager, container, points);

        return panel.gameObject;
    }

    // SkillTreeManager의 필드는 전부 [SerializeField] private라 SerializedObject로만 채울 수 있다.
    private static void WireManager(SkillTreeManager manager, RectTransform container, TextMeshProUGUI points)
    {
        var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NodePrefabPath);
        if (nodePrefab == null)
            Debug.LogWarning($"[SkillTreePanelGenerator] 노드 프리팹을 찾지 못했습니다: {NodePrefabPath}");

        var database = AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>(DatabasePath);
        if (database == null)
            Debug.LogWarning($"[SkillTreePanelGenerator] 스킬 데이터베이스를 찾지 못했습니다: {DatabasePath}");

        var so = new SerializedObject(manager);
        Set(so, "nodePrefab", nodePrefab);
        Set(so, "gridContainer", container);
        Set(so, "skillDatabase", database);
        Set(so, "pointsText", points);
        so.ApplyModifiedProperties();
    }

    private static void Set(SerializedObject so, string propertyName, Object value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[SkillTreePanelGenerator] SkillTreeManager에 {propertyName} 필드가 없습니다.");
            return;
        }

        prop.objectReferenceValue = value;
    }
}
