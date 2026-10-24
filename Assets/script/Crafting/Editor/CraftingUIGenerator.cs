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
    // 재료 한 줄 프리팹과 같은 폴더를 쓴다(경로를 두 곳에 적어두면 한쪽만 바뀌어 어긋난다).
    private const string PrefabFolder = EditorUIBuilder.IngredientRowPrefabFolder;

    // 결과물 아이콘 한 변. 아이콘을 담는 줄(ResultHeader)보다 작아야 한다.
    private const float IconSize = 128f;

    // 레시피 한 줄의 높이. LayoutElement와 프리팹 루트의 sizeDelta가 반드시 같은 값을 써야 한다.
    // 부모(Content)의 childControlHeight가 켜져 있으면 LayoutElement를, 꺼져 있으면 sizeDelta를
    // 읽기 때문에, 둘이 어긋나면 어느 쪽이 쓰였는지 화면만 보고는 알 수 없다.
    private const float RecipeRowHeight = 56f;
    // 프리팹을 단독으로 열었을 때 보기 좋으라고 넣는 값. 실제 폭은 목록이 매번 다시 잡는다.
    private const float RecipeRowWidth = 396f;

    // 레시피 목록 세로 스크롤바의 폭. 뷰포트를 이만큼 좁혀서 내용과 겹치지 않게 한다.
    private const float ScrollbarWidth = 12f;

    // 다시 실행하면 코드 기준으로 프리팹을 덮어쓴다. 손으로 꾸민 디자인이 있다면 날아가므로
    // 평소에는 "게임 UI 전체 생성"을 쓰고, 제작창만 되살릴 때 이 메뉴를 쓴다.
    [MenuItem("Tools/UI 다시 만들기/제작창만", false, 20)]
    public static void GenerateCraftingUI()
    {
        GameObject panelPrefab = BuildPanelPrefab();
        if (panelPrefab == null) return;

        TryInstantiateIntoScene(panelPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>성공!</color> 제작 UI 테스트용 프리팹을 생성했습니다 (Assets/script/Crafting/Prefabs). " +
                   "실제 아트 에셋으로 나중에 교체해서 쓰시면 됩니다.");
    }

    /// <summary>
    /// 제작 패널 프리팹을 코드에서 다시 만들어 돌려준다. 탭 창 생성(MenuWindowGenerator)도
    /// 이 메서드를 쓴다 - 입구는 둘이지만 패널을 조립하는 코드는 하나뿐이어야 한다.
    /// </summary>
    public static GameObject BuildPanelPrefab()
    {
        if (!EditorUIBuilder.EnsureFolder(PrefabFolder)) return null;

        RecipeSlotUI recipeRowPrefab = BuildRecipeRowPrefab();
        // 재료 한 줄은 건설 현장 패널과 공용이라 EditorUIBuilder가 만든다.
        IngredientRowUI ingredientRowPrefab = EditorUIBuilder.BuildIngredientRowPrefab();
        return BuildCraftingPanel(recipeRowPrefab, ingredientRowPrefab);
    }

    // ───────────────────────── 레시피 목록 행 프리팹 ─────────────────────────

    private static RecipeSlotUI BuildRecipeRowPrefab()
    {
        var root = new GameObject("RecipeSlotRow", typeof(RectTransform));
        // 유니티가 새 RectTransform에 넣는 기본값은 100x100이다. 그대로 두면 부모가 높이를
        // 제어하지 않을 때(childControlHeight=false) 아래 LayoutElement가 아니라 이 100이 쓰인다.
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(RecipeRowWidth, RecipeRowHeight);

        var bg = root.AddComponent<Image>();
        bg.color = Color.white;

        var layoutElement = root.AddComponent<LayoutElement>();
        layoutElement.minHeight = RecipeRowHeight;
        layoutElement.preferredHeight = RecipeRowHeight;

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
        // childControlWidth는 반드시 켜야 한다. 끄면(유니티 기본값) 레이아웃 그룹이 자식 폭을
        // 건드리지 않아서, CreateChildRect가 만든 기본 sizeDelta.x = 0이 그대로 남는다.
        // 폭 0짜리 TMP는 단어마다 줄을 바꾸므로 제목이 "Crafting / (Test)"처럼 쪼개졌다.
        // (flexibleWidth도 제어하지 않는 축에는 적용되지 않아 무시된다.)
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false; // 남는 폭은 flexibleWidth를 준 제목만 가져가게
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;

        var title = EditorUIBuilder.CreateChildText("Title", header, "Crafting", 32, Color.white, TextAlignmentOptions.MidlineLeft);
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
        // 기본값(Elastic)은 목록 끝에서 튕긴다. 끝에서 멈추는 쪽이 "덜 만들어진 창"으로 안 보인다.
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = EditorUIBuilder.CreateChildRect("Viewport", scrollRt);
        EditorUIBuilder.StretchFull(viewport);
        viewport.offsetMax = new Vector2(-ScrollbarWidth, 0f); // 오른쪽에 스크롤바 자리를 비운다
        // Mask는 그래픽 알파가 낮으면 스텐실이 안 잡혀 내용이 통째로 안 보이는 함정이 있어서,
        // 그래픽이 필요 없는 RectMask2D를 쓴다 (ScrollRect 뷰포트의 표준 방식이기도 함).
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = EditorUIBuilder.CreateChildRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        // sizeDelta를 반드시 적어야 한다. 가로가 늘어나는 앵커(0..1)에서 sizeDelta.x는 뷰포트
        // 대비 "가산값"이라, 유니티 기본값 100이 남아 있으면 목록이 뷰포트보다 100 넓어진다.
        // pivot.x가 0.5라 그 100이 좌우로 50씩 나뉘고, 행 전체가 왼쪽으로 50만큼 밀려
        // 아이콘(여백10 + 44)이 RectMask2D에 잘려 5픽셀짜리 띠로만 보였다.
        // 세로는 ContentSizeFitter가 매번 다시 잡아줘서 이 문제가 가로에서만 드러났다.
        content.sizeDelta = Vector2.zero;

        var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(6, 6, 6, 6);
        contentLayout.spacing = 6;
        // 반드시 true여야 한다. false면 HorizontalOrVerticalLayoutGroup.GetChildSizes가
        // LayoutElement를 아예 보지 않고 child.sizeDelta.y를 쓴다. 행 프리팹 기본값 100이 채택되어
        // 56으로 맞춰둔 LayoutElement.preferredHeight가 죽은 코드가 됐고, 668짜리 뷰포트에
        // 10.7행이 아니라 6.25행만 들어가서 목록이 잘려 보였다.
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false; // 행이 뷰포트를 채우려 늘어나지 않게

        var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        // 레시피가 뷰포트보다 길어지면 마지막 줄이 잘리는데, 스크롤바가 없으면 그게 "스크롤되는
        // 목록"이 아니라 "깨진 UI"로 보인다. 레시피는 계속 늘어나므로 항상 띄워둔다.
        // Permanent면 ScrollRect가 뷰포트 크기를 건드리지 않아서, 위에서 비워둔 폭이 그대로 유지된다.
        scrollRect.verticalScrollbar = EditorUIBuilder.CreateVerticalScrollbar(scrollRt, ScrollbarWidth);
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

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
        resultHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 140;
        var resultHeaderLayout = resultHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
        resultHeaderLayout.spacing = 16;
        resultHeaderLayout.childAlignment = TextAnchor.MiddleLeft;
        // Header와 같은 이유로 childControlWidth가 필요하다. 이게 꺼져 있어서 결과물 이름이
        // 폭 0으로 줄바꿈되고("Sword / _wood") 아이콘 옆이 아니라 가운데 위로 떠 있었다.
        resultHeaderLayout.childControlWidth = true;
        // 끄지 않으면 아이콘(96 고정)까지 같이 늘어난다. 남는 폭은 이름/수량 칸만 가져간다.
        resultHeaderLayout.childForceExpandWidth = false;
        resultHeaderLayout.childControlHeight = true;
        resultHeaderLayout.childForceExpandHeight = true;

        // 결과물 미리보기. 오른쪽 패널이 900 가까이 넓어서 96은 너무 작아 보였다.
        var resultIcon = EditorUIBuilder.CreateChildImage("ResultIcon", resultHeader, Color.white, fixedWidth: IconSize);
        resultIcon.preserveAspect = true;

        // preferredWidth/Height를 반드시 적어야 한다. minWidth만 주고 preferred를 -1로 두면
        // LayoutUtility가 음수를 건너뛰면서 우선순위 슬롯도 선점하지 못해, 우선순위가 낮은
        // Image의 "스프라이트 원본 크기"가 대신 채택된다. 아이템 아이콘이 1024x1024(PPU 100)라
        // 1000유닛이 넘는 폭을 요구해서 아이콘 하나가 패널을 통째로 밀어냈다.
        var iconLayout = resultIcon.gameObject.AddComponent<LayoutElement>();
        iconLayout.minWidth = IconSize;
        iconLayout.preferredWidth = IconSize;
        iconLayout.minHeight = IconSize;
        iconLayout.preferredHeight = IconSize;

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
        ingredientList.sizeDelta = Vector2.zero; // 레시피 목록과 같은 이유. 빼먹으면 줄이 왼쪽으로 50 밀린다

        var ingListLayout = ingredientList.gameObject.AddComponent<VerticalLayoutGroup>();
        ingListLayout.spacing = 6;
        // 레시피 목록과 같은 이유. false면 재료 줄이 LayoutElement의 42가 아니라 sizeDelta의 100이 된다.
        ingListLayout.childControlHeight = true;
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

        // 제작 진행 바. 제작 중이 아닐 때는 CraftingUI가 통째로 꺼둔다.
        var progressTrack = EditorUIBuilder.CreateChildImage("ProgressTrack", right, new Color(0.15f, 0.15f, 0.15f), fixedWidth: 0);
        progressTrack.raycastTarget = false;
        progressTrack.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

        var progressFill = EditorUIBuilder.CreateChildImage("Fill", progressTrack.transform, new Color(0.35f, 0.75f, 0.4f), fixedWidth: 0);
        progressFill.raycastTarget = false;
        var fillRt = progressFill.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;   // 런타임에 anchorMax.x만 진행도로 바꾼다
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        var statusText = EditorUIBuilder.CreateChildText("StatusText", right, "", 18, new Color(0.85f, 0.35f, 0.35f), TextAlignmentOptions.Center);
        statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;

        craftingUI.craftButton = craftButton;
        craftingUI.craftStatusText = statusText;
        craftingUI.progressRoot = progressTrack.gameObject;
        craftingUI.progressFill = progressFill;
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

        instance.SetActive(false); // 탭 창 안의 내용이므로 기본은 꺼진 상태로 시작

        // 예전엔 여기서 PlayerMove.craftingUI를 채웠다. 지금은 제작창을 직접 열고 닫는 주체가
        // PlayerMove가 아니라 MenuWindow라서 그 필드 자체가 없어졌다.

        EditorSceneManager.MarkSceneDirty(instance.scene);
        Debug.Log("[CraftingUIGenerator] CraftingPanel을 씬의 Canvas 아래에 배치했습니다 (기본 비활성 상태).");
    }

    // GameObject/컴포넌트 생성 헬퍼(CreateChildRect 등)는 PlayerHUDGenerator 등 다른 에디터
    // 제너레이터와 공통으로 쓰기 위해 EditorUIBuilder로 옮겼다 (Assets/script/Editor).
}
