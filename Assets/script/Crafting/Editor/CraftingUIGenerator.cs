using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 실제 아트 에셋이 아직 없는 상태에서 제작 시스템을 기능적으로 테스트할 수 있도록,
// Canvas/스크롤뷰/버튼 등 UI 계층을 기본 유니티 UI(단색 Image, TMP 텍스트)로 코드에서 직접 조립한다.
// 디자인은 나중에 실제 에셋으로 교체하면 되고, 이 툴은 "동작하는 뼈대"만 만드는 용도.
public static class CraftingUIGenerator
{
    private const string PrefabFolder = "Assets/script/Crafting/Prefabs";

    [MenuItem("Tools/제작 UI 테스트용 생성")]
    public static void GenerateCraftingUI()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/script/Crafting"))
            {
                Debug.LogError("[CraftingUIGenerator] Assets/script/Crafting 폴더가 없습니다.");
                return;
            }
            AssetDatabase.CreateFolder("Assets/script/Crafting", "Prefabs");
        }

        RecipeSlotUI recipeRowPrefab = BuildRecipeRowPrefab();
        IngredientRowUI ingredientRowPrefab = BuildIngredientRowPrefab();
        GameObject panelPrefab = BuildCraftingPanel(recipeRowPrefab, ingredientRowPrefab);

        TryInstantiateIntoScene(panelPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>성공!</color> 제작 UI 테스트용 프리팹을 생성했습니다 (Assets/script/Crafting/Prefabs). " +
                   "실제 아트 에셋으로 나중에 교체해서 쓰시면 됩니다.");
    }

    // ───────────────────────── 레시피 목록 행 프리팹 ─────────────────────────

    private static RecipeSlotUI BuildRecipeRowPrefab()
    {
        var root = new GameObject("RecipeSlotRow", typeof(RectTransform));
        var bg = root.AddComponent<Image>();
        bg.color = Color.white;

        var layoutElement = root.AddComponent<LayoutElement>();
        layoutElement.minHeight = 56;
        layoutElement.preferredHeight = 56;

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        var iconImg = EditorUIBuilder.CreateChildImage("Icon", root.transform, Color.white, fixedWidth: 44);
        iconImg.preserveAspect = true;

        var nameText = EditorUIBuilder.CreateChildText("Name", root.transform, "", 20, new Color(0.1f, 0.1f, 0.1f), TextAlignmentOptions.MidlineLeft);
        nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        var slotUI = root.AddComponent<RecipeSlotUI>();
        slotUI.background = bg;
        slotUI.iconImage = iconImg;
        slotUI.nameText = nameText;
        slotUI.button = button;

        return EditorUIBuilder.SaveAndDestroy(root, $"{PrefabFolder}/RecipeSlotRow.prefab").GetComponent<RecipeSlotUI>();
    }

    // ───────────────────────── 재료 한 줄 프리팹 ─────────────────────────

    private static IngredientRowUI BuildIngredientRowPrefab()
    {
        var root = new GameObject("IngredientRow", typeof(RectTransform));

        var layoutElement = root.AddComponent<LayoutElement>();
        layoutElement.minHeight = 42;
        layoutElement.preferredHeight = 42;

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        var iconImg = EditorUIBuilder.CreateChildImage("Icon", root.transform, Color.white, fixedWidth: 32);
        iconImg.preserveAspect = true;

        var nameText = EditorUIBuilder.CreateChildText("Name", root.transform, "", 18, new Color(0.15f, 0.15f, 0.15f), TextAlignmentOptions.MidlineLeft);
        nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var countText = EditorUIBuilder.CreateChildText("Count", root.transform, "", 18, Color.black, TextAlignmentOptions.MidlineRight);
        countText.fontStyle = FontStyles.Bold;
        countText.gameObject.AddComponent<LayoutElement>().minWidth = 90;

        var ingredientUI = root.AddComponent<IngredientRowUI>();
        ingredientUI.iconImage = iconImg;
        ingredientUI.nameText = nameText;
        ingredientUI.countText = countText;

        return EditorUIBuilder.SaveAndDestroy(root, $"{PrefabFolder}/IngredientRow.prefab").GetComponent<IngredientRowUI>();
    }

    // ───────────────────────── 제작 패널 전체 ─────────────────────────

    private static GameObject BuildCraftingPanel(RecipeSlotUI recipeRowPrefab, IngredientRowUI ingredientRowPrefab)
    {
        var panel = new GameObject("CraftingPanel", typeof(RectTransform));
        var panelRt = panel.GetComponent<RectTransform>();
        // 1920x1080 기준(Constant Pixel Size라 실제 픽셀 그대로 적용됨)으로 1300x760 크기가 되도록,
        // 고정 픽셀 sizeDelta 대신 화면 비율(anchor) 기반으로 잡는다. 기존처럼 고정 픽셀이면 실제
        // 플레이/게임 뷰 해상도가 1920보다 좁을 때 패널이 화면 밖으로 잘려 나간다 - 비율 앵커를 쓰면
        // 어떤 해상도에서도 항상 화면 비율에 맞게 줄어들어서 잘리지 않는다.
        const float widthRatio = 1300f / 1920f;
        const float heightRatio = 760f / 1080f;
        panelRt.anchorMin = new Vector2(0.5f - widthRatio / 2f, 0.5f - heightRatio / 2f);
        panelRt.anchorMax = new Vector2(0.5f + widthRatio / 2f, 0.5f + heightRatio / 2f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panel.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.13f, 0.97f);

        var craftingUI = panel.AddComponent<CraftingUI>();
        craftingUI.panelRoot = panel;

        BuildHeader(panel.transform, craftingUI);
        BuildBody(panel.transform, craftingUI, recipeRowPrefab, ingredientRowPrefab);

        return EditorUIBuilder.SaveAndDestroy(panel, $"{PrefabFolder}/CraftingPanel.prefab");
    }

    private static void BuildHeader(Transform parent, CraftingUI craftingUI)
    {
        var header = EditorUIBuilder.CreateChildRect("Header", parent);
        header.anchorMin = new Vector2(0, 1);
        header.anchorMax = new Vector2(1, 1);
        header.pivot = new Vector2(0.5f, 1);
        header.sizeDelta = new Vector2(0, 64);

        var layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 10, 10);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;

        var title = EditorUIBuilder.CreateChildText("Title", header, "Crafting (Test)", 32, Color.white, TextAlignmentOptions.MidlineLeft);
        title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var closeButtonImage = EditorUIBuilder.CreateChildImage("CloseButton", header, new Color(0.35f, 0.35f, 0.35f), fixedWidth: 40);

        // Header의 HorizontalLayoutGroup이 위치/크기를 자동으로 잡지 못하게 제외시킨다.
        // (ignoreLayout=false면 유니티가 매 레이아웃 패스마다 자식을 재배치해서, 에디터에서 드래그로
        // 옮겨도 계속 제자리로 돌아간다 - 그래서 직접 위치를 바꿀 수 있으려면 이게 반드시 필요하다.)
        var closeLayoutElement = closeButtonImage.gameObject.AddComponent<LayoutElement>();
        closeLayoutElement.ignoreLayout = true;

        var closeRt = closeButtonImage.rectTransform;
        closeRt.anchorMin = new Vector2(1, 1);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 1);
        closeRt.sizeDelta = new Vector2(40, 40);
        closeRt.anchoredPosition = new Vector2(-10, -10);

        var closeButton = closeButtonImage.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeButtonImage;

        var closeText = EditorUIBuilder.CreateChildText("X", closeButtonImage.transform, "X", 20, Color.white, TextAlignmentOptions.Center);
        EditorUIBuilder.StretchFull(closeText.rectTransform);

        // onClick 연결은 여기서 AddListener로 걸어도 프리팹에 저장이 안 되므로 CraftingUI.Awake()에서 처리한다.
        craftingUI.closeButton = closeButton;
    }

    private static void BuildBody(Transform parent, CraftingUI craftingUI, RecipeSlotUI recipeRowPrefab, IngredientRowUI ingredientRowPrefab)
    {
        var body = EditorUIBuilder.CreateChildRect("Body", parent);
        body.anchorMin = Vector2.zero;
        body.anchorMax = Vector2.one;
        body.offsetMin = new Vector2(20, 20);
        body.offsetMax = new Vector2(-20, -70);

        var layout = body.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        BuildRecipeListPanel(body, craftingUI, recipeRowPrefab);
        BuildDetailPanel(body, craftingUI, ingredientRowPrefab);
    }

    private static void BuildRecipeListPanel(Transform parent, CraftingUI craftingUI, RecipeSlotUI recipeRowPrefab)
    {
        var left = EditorUIBuilder.CreateChildRect("LeftPanel", parent);
        left.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        left.gameObject.AddComponent<LayoutElement>().preferredWidth = 420;

        var scrollRt = EditorUIBuilder.CreateChildRect("RecipeScrollView", left);
        EditorUIBuilder.StretchFull(scrollRt);
        var scrollRect = scrollRt.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        var viewport = EditorUIBuilder.CreateChildRect("Viewport", scrollRt);
        EditorUIBuilder.StretchFull(viewport);
        // Mask는 그래픽 알파가 낮으면 스텐실이 안 잡혀 내용이 통째로 안 보이는 함정이 있어서,
        // 그래픽이 필요 없는 RectMask2D를 쓴다 (ScrollRect 뷰포트의 표준 방식이기도 함).
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = EditorUIBuilder.CreateChildRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;

        var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(6, 6, 6, 6);
        contentLayout.spacing = 6;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        craftingUI.recipeListContent = content;
        craftingUI.recipeRowPrefab = recipeRowPrefab;
    }

    private static void BuildDetailPanel(Transform parent, CraftingUI craftingUI, IngredientRowUI ingredientRowPrefab)
    {
        var right = EditorUIBuilder.CreateChildRect("RightPanel", parent);
        right.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f);
        right.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var layout = right.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true; // flexibleHeight(재료 스크롤 영역)가 실제로 적용되려면 true여야 한다
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false; // flexibleHeight를 준 자식만 남는 공간을 가져가게

        // 결과물 아이콘 + 이름/수량
        var resultHeader = EditorUIBuilder.CreateChildRect("ResultHeader", right);
        resultHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
        var resultHeaderLayout = resultHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
        resultHeaderLayout.spacing = 16;
        resultHeaderLayout.childAlignment = TextAnchor.MiddleLeft;
        resultHeaderLayout.childControlHeight = true;
        resultHeaderLayout.childForceExpandHeight = true;

        var resultIcon = EditorUIBuilder.CreateChildImage("ResultIcon", resultHeader, Color.white, fixedWidth: 96);
        resultIcon.preserveAspect = true;
        resultIcon.gameObject.AddComponent<LayoutElement>().minWidth = 96;

        var resultTexts = EditorUIBuilder.CreateChildRect("ResultTexts", resultHeader);
        resultTexts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var resultTextsLayout = resultTexts.gameObject.AddComponent<VerticalLayoutGroup>();
        resultTextsLayout.childControlWidth = true;
        resultTextsLayout.childForceExpandWidth = true;
        resultTextsLayout.childControlHeight = false;

        var resultName = EditorUIBuilder.CreateChildText("ResultName", resultTexts, "", 32, Color.white, TextAlignmentOptions.MidlineLeft);
        var resultAmount = EditorUIBuilder.CreateChildText("ResultAmount", resultTexts, "", 20, new Color(0.75f, 0.75f, 0.75f), TextAlignmentOptions.MidlineLeft);

        craftingUI.resultIcon = resultIcon;
        craftingUI.resultNameText = resultName;
        craftingUI.resultAmountText = resultAmount;

        // 재료 목록
        var ingLabel = EditorUIBuilder.CreateChildText("IngredientsLabel", right, "Ingredients", 18, new Color(0.65f, 0.65f, 0.65f), TextAlignmentOptions.MidlineLeft);
        ingLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;

        // 재료 목록도 레시피 목록과 동일하게 스크롤뷰로 감싼다 - 재료가 몇 개든 이 영역(flexibleHeight)
        // 안에서만 스크롤되고, 패널 전체 크기는 항상 고정이라 "일부 아이템만 튀어나오는" 문제가 없어진다.
        var ingredientScrollRt = EditorUIBuilder.CreateChildRect("IngredientScrollView", right);
        ingredientScrollRt.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        var ingredientScrollRect = ingredientScrollRt.gameObject.AddComponent<ScrollRect>();
        ingredientScrollRect.horizontal = false;
        ingredientScrollRect.vertical = true;

        var ingredientViewport = EditorUIBuilder.CreateChildRect("Viewport", ingredientScrollRt);
        EditorUIBuilder.StretchFull(ingredientViewport);
        ingredientViewport.gameObject.AddComponent<RectMask2D>();

        var ingredientList = EditorUIBuilder.CreateChildRect("Content", ingredientViewport);
        ingredientList.anchorMin = new Vector2(0, 1);
        ingredientList.anchorMax = new Vector2(1, 1);
        ingredientList.pivot = new Vector2(0.5f, 1);
        ingredientList.anchoredPosition = Vector2.zero;

        var ingListLayout = ingredientList.gameObject.AddComponent<VerticalLayoutGroup>();
        ingListLayout.spacing = 6;
        ingListLayout.childControlHeight = false;
        ingListLayout.childControlWidth = true;
        ingListLayout.childForceExpandWidth = true;
        ingListLayout.childForceExpandHeight = false;
        var ingListFitter = ingredientList.gameObject.AddComponent<ContentSizeFitter>();
        ingListFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ingredientScrollRect.viewport = ingredientViewport;
        ingredientScrollRect.content = ingredientList;

        craftingUI.ingredientListContent = ingredientList;
        craftingUI.ingredientRowPrefab = ingredientRowPrefab;

        var craftButtonImage = EditorUIBuilder.CreateChildImage("CraftButton", right, new Color(0.3f, 0.5f, 0.3f), fixedWidth: 0);
        craftButtonImage.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;
        var craftButton = craftButtonImage.gameObject.AddComponent<Button>();
        craftButton.targetGraphic = craftButtonImage;

        var craftButtonText = EditorUIBuilder.CreateChildText("Text", craftButtonImage.transform, "Craft", 26, Color.white, TextAlignmentOptions.Center);
        EditorUIBuilder.StretchFull(craftButtonText.rectTransform);

        var statusText = EditorUIBuilder.CreateChildText("StatusText", right, "", 18, new Color(0.85f, 0.35f, 0.35f), TextAlignmentOptions.Center);
        statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;

        craftingUI.craftButton = craftButton;
        craftingUI.craftStatusText = statusText;
    }

    // ───────────────────────── 씬에 배치 ─────────────────────────

    private static void TryInstantiateIntoScene(GameObject panelPrefab)
    {
        var existing = Object.FindFirstObjectByType<CraftingUI>();
        if (existing != null)
        {
            Debug.LogWarning("[CraftingUIGenerator] 씬에 이미 CraftingUI가 있어서 새로 배치하지 않았습니다. 필요하면 기존 것을 지우고 다시 실행하세요.");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[CraftingUIGenerator] 씬에 Canvas가 없어서 프리팹만 만들고 씬에는 배치하지 않았습니다. Canvas 아래에 CraftingPanel 프리팹을 직접 끌어넣어주세요.");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Create CraftingPanel");

        // 앵커/크기는 프리팹(BuildCraftingPanel)에서 이미 화면 비율 기반으로 잡아뒀으므로 여기서는
        // 건드리지 않는다 - 예전처럼 point anchor(0.5,0.5)+고정 sizeDelta로 덮어쓰면 화면 비율 대응이
        // 무효화되어 다시 화면 밖으로 잘려나갈 수 있다.

        instance.SetActive(false); // 인벤토리와 동일하게 기본은 닫힌 상태로 시작

        WireIntoPlayerMove(instance.GetComponent<CraftingUI>());

        EditorSceneManager.MarkSceneDirty(instance.scene);
        Debug.Log("[CraftingUIGenerator] CraftingPanel을 씬의 Canvas 아래에 배치했습니다 (기본 비활성 상태).");
    }

    // PlayerMove.craftingUI는 [SerializeField] private라 SerializedObject를 통해서만 채울 수 있다.
    private static void WireIntoPlayerMove(CraftingUI craftingUI)
    {
        var playerMove = Object.FindFirstObjectByType<PlayerMove>();
        if (playerMove == null)
        {
            Debug.LogWarning("[CraftingUIGenerator] 씬에서 PlayerMove를 찾지 못해 craftingUI 필드를 자동 연결하지 못했습니다. 수동으로 연결해주세요.");
            return;
        }

        var so = new SerializedObject(playerMove);
        var prop = so.FindProperty("craftingUI");
        if (prop == null)
        {
            Debug.LogWarning("[CraftingUIGenerator] PlayerMove에 craftingUI 필드가 없습니다.");
            return;
        }

        prop.objectReferenceValue = craftingUI;
        so.ApplyModifiedProperties();
    }

    // GameObject/컴포넌트 생성 헬퍼(CreateChildRect 등)는 PlayerHUDGenerator 등 다른 에디터
    // 제너레이터와 공통으로 쓰기 위해 EditorUIBuilder로 옮겼다 (Assets/script/Editor).
}
