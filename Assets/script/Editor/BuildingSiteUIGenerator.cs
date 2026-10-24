using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 건설 현장 재료 패널(BuildingSiteUI)의 UI 계층을 코드로 조립해서 프리팹으로 굽고, 씬의 Canvas 아래에
// 배치한다. CraftingUIGenerator / PlayerHUDGenerator와 같은 방식이다 - 실제 아트가 들어오면 생성된
// 프리팹을 그대로 꾸미면 되고, 이 툴은 "동작하는 뼈대"만 만든다.
public static class BuildingSiteUIGenerator
{
    private const string PrefabFolder = "Assets/script/Prefabs";
    private const string PanelPrefabPath = PrefabFolder + "/BuildingSitePanel.prefab";

    // 제작창은 어두운 패널 + 밝은 글씨지만, 재료 한 줄(IngredientRowUI)의 글자색은 어두운 색이다.
    // 같은 줄을 재사용하므로 이 패널은 배경을 밝게 둬야 글씨가 보인다.
    private static readonly Color PanelColor = new Color(0.95f, 0.93f, 0.86f, 0.95f);
    private static readonly Color TitleColor = new Color(0.18f, 0.14f, 0.1f);
    private static readonly Color FooterColor = new Color(0.4f, 0.36f, 0.3f);

    // 다시 실행하면 코드 기준으로 프리팹을 덮어쓴다(손으로 꾸민 디자인이 있으면 날아간다).
    // 평소에는 "게임 UI 전체 생성"을 쓰고, 건설 패널만 되살릴 때 이 메뉴를 쓴다.
    [MenuItem("Tools/UI 다시 만들기/건설 현장 패널만", false, 22)]
    public static void GenerateBuildingSiteUI()
    {
        if (!EditorUIBuilder.EnsureFolder(PrefabFolder)) return;

        // 재료 한 줄은 제작창과 공용 프리팹이다(EditorUIBuilder가 소유).
        IngredientRowUI rowPrefab = EditorUIBuilder.BuildIngredientRowPrefab();
        if (rowPrefab == null)
        {
            Debug.LogError("[BuildingSiteUIGenerator] 재료 한 줄 프리팹을 만들지 못했습니다.");
            return;
        }

        GameObject panelPrefab = BuildPanel(rowPrefab);
        TryInstantiateIntoScene(panelPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>성공!</color> 건설 현장 재료 패널을 생성했습니다 ({PanelPrefabPath}).");
    }

    // ───────────────────────── 패널 ─────────────────────────

    private static GameObject BuildPanel(IngredientRowUI rowPrefab)
    {
        // 루트는 항상 켜진 상태로 둔다(PlayerMove가 FindFirstObjectByType으로 찾으므로).
        // 실제로 보이고 숨는 건 아래 Box이고, BuildingSiteUI가 그것만 켜고 끈다.
        var root = new GameObject("BuildingSitePanel", typeof(RectTransform));
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1, 1);   // 좌상단은 PlayerHUD, 하단은 퀵슬롯이 쓰므로 우상단
        rootRt.anchorMax = new Vector2(1, 1);
        rootRt.pivot = new Vector2(1, 1);
        rootRt.anchoredPosition = new Vector2(-28, -28);
        rootRt.sizeDelta = new Vector2(300, 0);

        var ui = root.AddComponent<BuildingSiteUI>();

        // Box: 높이는 내용에 맞춰 자동(ContentSizeFitter), 너비는 루트를 따라간다.
        var box = EditorUIBuilder.CreateChildRect("Box", root.transform);
        box.anchorMin = new Vector2(0, 1);
        box.anchorMax = new Vector2(1, 1);
        box.pivot = new Vector2(0.5f, 1);
        box.offsetMin = Vector2.zero;
        box.offsetMax = Vector2.zero;
        box.gameObject.AddComponent<Image>().color = PanelColor;

        var boxLayout = box.gameObject.AddComponent<VerticalLayoutGroup>();
        boxLayout.padding = new RectOffset(14, 14, 12, 12);
        boxLayout.spacing = 8;
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = true;
        boxLayout.childForceExpandWidth = true;
        boxLayout.childForceExpandHeight = false;

        var boxFitter = box.gameObject.AddComponent<ContentSizeFitter>();
        boxFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var title = EditorUIBuilder.CreateChildText("Title", box, "Building", 20, TitleColor, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

        // 재료 줄이 쌓이는 곳. 자기 VerticalLayoutGroup이 높이를 알려주므로 ContentSizeFitter는 필요 없다
        // (부모 Box가 childControlHeight로 이 값을 읽어간다).
        var rows = EditorUIBuilder.CreateChildRect("Rows", box);
        var rowsLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 4;
        rowsLayout.childControlWidth = true;
        // false면 LayoutElement를 보지 않고 줄 프리팹 루트의 sizeDelta.y를 쓴다(원래 주석은 반대로 적혀
        // 있었다). 그 기본값이 100이라 재료 줄이 42가 아니라 100으로 나오고 있었다. 지금은
        // EditorUIBuilder가 프리팹 루트 sizeDelta도 42로 맞춰두므로 어느 쪽을 읽어도 42가 된다.
        rowsLayout.childControlHeight = false;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;

        var footer = EditorUIBuilder.CreateChildText("Footer", box, "", 15, FooterColor, TextAlignmentOptions.MidlineLeft);
        footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;

        ui.contentRoot = box.gameObject;
        ui.titleText = title;
        ui.rowContainer = rows;
        ui.rowPrefab = rowPrefab;
        ui.footerText = footer;

        return EditorUIBuilder.SaveAndDestroy(root, PanelPrefabPath);
    }

    // ───────────────────────── 씬에 배치 ─────────────────────────

    private static void TryInstantiateIntoScene(GameObject panelPrefab)
    {
        var existing = Object.FindFirstObjectByType<BuildingSiteUI>();
        if (existing != null)
        {
            Debug.LogWarning("[BuildingSiteUIGenerator] 씬에 이미 BuildingSiteUI가 있어서 새로 배치하지 않았습니다. 필요하면 기존 것을 지우고 다시 실행하세요.");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[BuildingSiteUIGenerator] 씬에 Canvas가 없어서 프리팹만 만들고 배치하지 않았습니다. Canvas 아래에 BuildingSitePanel 프리팹을 직접 끌어넣어주세요.");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Create BuildingSitePanel");

        // 루트는 켜둔 채로 둔다. 패널 본체는 BuildingSiteUI.Awake가 꺼서 시작한다.
        // PlayerMove는 buildingSiteUI 필드가 비어 있으면 Start에서 직접 찾으므로 별도 연결이 필요 없다.
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Debug.Log("[BuildingSiteUIGenerator] BuildingSitePanel을 씬의 Canvas 아래(우상단)에 배치했습니다.");
    }
}
