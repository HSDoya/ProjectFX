using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 게임 플레이 UI 전체를 코드에서 만든다. 메뉴 한 번이면 빈 씬에서도 같은 결과가 나온다.
//
//   Canvas
//   ├─ MenuWindow            창 열림 상태와 현재 탭의 소유자. 항상 켜져 있다
//   │  └─ Window             실제로 켜고 끄는 본체
//   │     ├─ TabBar          Items / Craft / Stats / Skills
//   │     └─ Content         탭 내용 (한 번에 하나만 켜진다)
//   ├─ QickSlot              하단 HUD 퀵슬롯 바. 항상 켜져 있다
//   ├─ PlayerHUD             없으면 PlayerHUDGenerator가 만든다
//   ├─ BuildingSitePanel     없으면 BuildingSiteUIGenerator가 만든다
//   └─ ItemDrag              드래그 중인 아이콘. 항상 맨 위
//   Inventory                아이템 데이터. UI가 아니므로 Canvas 밖 씬 루트
//
// 예전에는 씬에 손으로 만들어 둔 인벤토리/제작 패널을 찾아 창 안으로 "옮겼다". 그러면 그 패널이
// 씬에 남아 있을 때만 결과가 맞아서, 한 번 지우고 나면 탭이 빈 채로 만들어진다
// ("InventoryUI를 찾지 못해 아이템 탭을 비워둡니다"). 지금은 전부 새로 만든다.
//
// 되돌리기: Ctrl+Z 한 번으로 전체가 복구된다. Main.unity는 커밋돼 있으므로 git checkout으로도 돌아간다.
public static class MenuWindowGenerator
{
    private static readonly Color WindowColor = new Color(0.1f, 0.1f, 0.11f, 0.98f);
    private static readonly Color TabBarColor = new Color(0.16f, 0.16f, 0.17f);
    private static readonly Color ActiveTab = new Color(0.95f, 0.93f, 0.86f);
    private static readonly Color InactiveTab = new Color(0.45f, 0.43f, 0.4f);

    private const float TabBarHeight = 52f;

    // 창의 최소 크기. 실제 크기는 담아야 하는 탭 내용에 맞춰 이보다 커질 수 있다.
    private const float WindowWidth = 1360f;
    private const float WindowHeight = 810f;
    private const float ContentMargin = 18f; // 탭 내용과 창 테두리 사이 여백

    // 캔버스 기준 해상도. 패널 크기를 전부 이 좌표계의 픽셀로 적는다.
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    // 스킬 트리는 예전엔 별도 씬(SkillTree)으로 넘어가는 탭이었다. 그러면 Main이 언로드되어
    // 세이브 없는 지금은 진행 상황이 전부 날아간다. 지금은 다른 탭과 같은 패널이다.
    // MenuWindow.Tab.sceneToLoad 기능 자체는 남아 있다(필요한 탭이 생기면 다시 쓰면 된다).

    [MenuItem("Tools/게임 UI 전체 생성(Main)", false, 0)]
    public static void GenerateGameUI()
    {
        if (!ConfirmRebuild()) return;

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate Game UI");

        Canvas canvas = EnsureCanvas();
        if (canvas == null) return;

        BuildEverything(canvas);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log("<color=green>성공!</color> 게임 UI를 전부 새로 만들었습니다. " +
                  "I로 창 열기, Tab으로 탭 전환, ESC로 닫기. 확인했으면 씬을 저장하세요. (되돌리려면 Ctrl+Z)");
    }

    private static void BuildEverything(Canvas canvas)
    {
        ClearGeneratedUI();

        // 아이템 데이터(Inventory)는 UI가 아니라서 지우지 않는다. 씬 루트에 있는지만 확인한다.
        InventoryPanelGenerator.EnsureInventoryData();

        MenuWindow window = BuildWindow(canvas, out RectTransform tabBar, out RectTransform content);

        window.tabs.Clear();
        // 라벨은 영문이다. TMP 기본 폰트(LiberationSans)에 한글 글리프가 없어 한글은 깨져 보인다.
        window.tabs.Add(MakeTab(tabBar, "Items", InventoryPanelGenerator.BuildItemsPanel(content)));
        window.tabs.Add(MakeTab(tabBar, "Craft", BuildCraftingPanel(content)));
        window.tabs.Add(MakeTab(tabBar, "Stats", StatPanelGenerator.Build(content)));
        window.tabs.Add(MakeTab(tabBar, "Skills", SkillTreePanelGenerator.Build(content)));

        GameObject quickSlotBar = InventoryPanelGenerator.BuildQuickSlotBar(canvas.transform);
        // 창이 열리면 바 전체가 아니라 자식 BG만 숨긴다. 루트를 끄면 QuickSlotBarUI까지 멈춘다.
        window.hudQuickSlotUI = quickSlotBar.transform.childCount > 0
            ? quickSlotBar.transform.GetChild(0).gameObject
            : quickSlotBar;

        EnsurePlayerHud();
        EnsureBuildingSitePanel();

        GameObject itemDrag = InventoryPanelGenerator.BuildItemDrag(canvas);

        // 그리기 순서를 명시적으로 정한다. 유니티 UI는 "뒤쪽 형제가 위에" 그려지는데,
        // HUD와 건설 패널은 창보다 나중에 만들어져서 그대로 두면 창 위를 덮는다
        // (HUD는 화면 좌상단, 창의 탭 바도 좌상단이라 실제로 겹친다).
        //   HUD류  ->  창  ->  퀵슬롯 바  ->  드래그 아이콘(항상 맨 위)
        window.transform.SetAsLastSibling(); // MenuWindow 루트 (컴포넌트가 루트에 붙어 있다)
        quickSlotBar.transform.SetAsLastSibling();
        itemDrag.transform.SetAsLastSibling();

        WirePlayerReferences(window);

        window.windowRoot.SetActive(false); // 닫힌 상태로 시작 (MenuWindow.Awake도 같은 상태를 적용한다)
    }

    private static bool ConfirmRebuild()
    {
        bool hasExisting =
            Object.FindFirstObjectByType<MenuWindow>(FindObjectsInactive.Include) != null ||
            Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include) != null ||
            Object.FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include) != null;

        if (!hasExisting) return true;

        return EditorUtility.DisplayDialog(
            "게임 UI 전체 생성",
            "씬에 있는 인벤토리 / 제작 / 탭 창 UI를 지우고 코드에서 새로 만듭니다.\n" +
            "손으로 꾸며 둔 디자인이 있다면 사라집니다.\n\n" +
            "아이템 데이터(Inventory)와 플레이어는 건드리지 않습니다.\n" +
            "Ctrl+Z 한 번으로 전부 되돌릴 수 있습니다.",
            "새로 만들기", "취소");
    }

    // ───────────────────────── 기존 UI 정리 ─────────────────────────

    // 새로 만들기 전에 이 제너레이터가 만드는 것들을 지운다. 지우지 않으면 ItemDragController처럼
    // 싱글턴인 컴포넌트가 둘이 되어, 나중에 깨어난 쪽이 자기 오브젝트를 스스로 지워버린다.
    private static void ClearGeneratedUI()
    {
        DestroyAll<MenuWindow>();      // 창을 먼저 지우면 그 안의 탭 내용도 같이 사라진다
        DestroyAll<InventoryUI>();
        DestroyAll<StatPanelUI>();
        DestroyAll<QuickSlotBarUI>();
        DestroyAll<ItemDragController>();

        foreach (var crafting in Object.FindObjectsByType<CraftingUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (crafting == null) continue;
            DestroyObject(crafting.panelRoot != null ? crafting.panelRoot : crafting.gameObject);
        }

        // 컴포넌트가 떨어져 나간 껍데기만 남은 경우까지 치운다(이름은 기존 씬에서 쓰던 것 그대로).
        DestroyByName("QickSlot");
        DestroyByName("ItemDrag");
        DestroyByName("ItemDrage");
    }

    private static void DestroyAll<T>() where T : Component
    {
        foreach (var found in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (found == null) continue;
            DestroyObject(found.gameObject);
        }
    }

    private static void DestroyByName(string name)
    {
        foreach (var rootGo in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform found = FindByNameRecursive(rootGo.transform, name);
            if (found != null) DestroyObject(found.gameObject);
        }
    }

    private static void DestroyObject(GameObject go)
    {
        if (go == null) return;
        Undo.DestroyObjectImmediate(go);
    }

    // ───────────────────────── 캔버스 ─────────────────────────

    private static Canvas EnsureCanvas()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            go.layer = LayerMask.NameToLayer("UI");

            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }

        // 패널 크기를 1920x1080 기준 픽셀로 적을 수 있게 스케일 모드를 맞춘다.
        // Constant Pixel Size로 두면 화면이 좁을 때 패널이 그대로 화면 밖으로 잘려 나간다.
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);

        Undo.RecordObject(scaler, "Configure CanvasScaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        EditorUtility.SetDirty(scaler);

        EnsureEventSystem();
        return canvas;
    }

    // 버튼 클릭과 드래그는 EventSystem이 있어야 동작한다. 이 프로젝트는 Input System을 쓰므로
    // 입력 모듈도 그쪽 것을 붙인다(StandaloneInputModule을 붙이면 재생 시 에러가 난다).
    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    // ───────────────────────── 창 ─────────────────────────

    private static MenuWindow BuildWindow(Canvas canvas, out RectTransform tabBar, out RectTransform content)
    {
        var root = new GameObject("MenuWindow", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Create MenuWindow");
        root.layer = canvas.gameObject.layer;
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.SetParent(canvas.transform, false);
        EditorUIBuilder.StretchFull(rootRt); // 루트는 보이는 것이 없고 항상 켜져 있어야 한다

        var ui = root.AddComponent<MenuWindow>();
        ui.activeTabColor = ActiveTab;
        ui.inactiveTabColor = InactiveTab;

        // 창 본체. 크기는 "담아야 하는 것"에서 계산한 고정 픽셀이다.
        //
        // 예전엔 화면 비율(1360/1920)로 앵커를 잡았는데, 그러면 창이 캔버스 너비를 따라간다.
        // 캔버스는 Scale With Screen Size라 너비가 화면 비율에 따라 1663(4:3) ~ 1920(16:9) ~
        // 2217(21:9)로 변하고, 4:3에서는 콘텐츠 영역이 1177까지 좁아진다. 그 안에 1323짜리
        // 아이템 패널을 넣으니 장비 칸이 있는 오른쪽이 창 밖으로 삐져나갔다.
        // 고정 픽셀로 두면 어떤 화면 비율에서도 창 안쪽 크기가 같다.
        Vector2 itemsPanel = InventoryPanelGenerator.ItemsPanelSize;
        float windowW = Mathf.Max(WindowWidth, itemsPanel.x + ContentMargin * 2f);
        float windowH = Mathf.Max(WindowHeight, itemsPanel.y + ContentMargin * 2f + TabBarHeight);

        var window = EditorUIBuilder.CreateChildRect("Window", rootRt);
        window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(windowW, windowH);
        window.anchoredPosition = Vector2.zero;
        window.gameObject.AddComponent<Image>().color = WindowColor;

        tabBar = EditorUIBuilder.CreateChildRect("TabBar", window);
        tabBar.anchorMin = new Vector2(0f, 1f);
        tabBar.anchorMax = new Vector2(1f, 1f);
        tabBar.pivot = new Vector2(0.5f, 1f);
        tabBar.sizeDelta = new Vector2(0f, TabBarHeight);
        tabBar.gameObject.AddComponent<Image>().color = TabBarColor;

        var tabLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.padding = new RectOffset(8, 8, 8, 6);
        tabLayout.spacing = 6;
        tabLayout.childAlignment = TextAnchor.LowerLeft;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = false;
        tabLayout.childForceExpandHeight = true;

        // 콘텐츠 영역: 탭 바 아래 전부
        content = EditorUIBuilder.CreateChildRect("Content", window);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = new Vector2(0f, -TabBarHeight);

        ui.windowRoot = window.gameObject;
        return ui;
    }

    private static MenuWindow.Tab MakeTab(RectTransform tabBar, string label, GameObject panel, string sceneToLoad = null)
    {
        var buttonImage = EditorUIBuilder.CreateChildImage("Tab_" + label, tabBar, InactiveTab, fixedWidth: 0);
        buttonImage.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;
        var button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var text = EditorUIBuilder.CreateChildText("Label", buttonImage.transform, label, 20,
                                                   new Color(0.12f, 0.12f, 0.12f), TextAlignmentOptions.Center);
        EditorUIBuilder.StretchFull(text.rectTransform);

        // 창은 닫힌 상태로 시작하므로 탭 내용도 전부 꺼둔다. 여는 순간 MenuWindow가 고른 하나만 켠다.
        if (panel != null) panel.SetActive(false);

        return new MenuWindow.Tab
        {
            label = label,
            content = panel,
            button = button,
            sceneToLoad = sceneToLoad
        };
    }

    // 제작 패널은 프리팹이 따로 있고 조립 코드는 CraftingUIGenerator가 소유한다. 여기서는
    // 그 프리팹을 콘텐츠 영역에 올려놓기만 한다.
    private static GameObject BuildCraftingPanel(RectTransform content)
    {
        GameObject prefab = CraftingUIGenerator.BuildPanelPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[MenuWindowGenerator] 제작 패널 프리팹을 만들지 못해 Craft 탭을 비워둡니다.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, content);
        Undo.RegisterCreatedObjectUndo(instance, "Create CraftingPanel");
        EditorUIBuilder.StretchFull((RectTransform)instance.transform);
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    // ───────────────────────── 나머지 UI ─────────────────────────

    private static void EnsurePlayerHud()
    {
        if (Object.FindFirstObjectByType<PlayerHUD>(FindObjectsInactive.Include) != null) return;
        PlayerHUDGenerator.GeneratePlayerHUD();
    }

    private static void EnsureBuildingSitePanel()
    {
        if (Object.FindFirstObjectByType<BuildingSiteUI>(FindObjectsInactive.Include) != null) return;
        BuildingSiteUIGenerator.GenerateBuildingSiteUI();
    }

    // ───────────────────────── 플레이어 연결 ─────────────────────────

    // 창을 여닫는 키 입력은 PlayerMove가 받고, 스탯은 PlayerStats가 소유한다. 둘 다
    // [SerializeField] private라 SerializedObject로만 채울 수 있다.
    private static void WirePlayerReferences(MenuWindow window)
    {
        var playerMove = Object.FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
        if (playerMove == null)
        {
            Debug.LogWarning("[MenuWindowGenerator] 씬에서 PlayerMove를 찾지 못해 창 열기 키(I)를 연결하지 못했습니다.");
            return;
        }

        var stats = playerMove.GetComponent<PlayerStats>();
        if (stats == null) stats = Undo.AddComponent<PlayerStats>(playerMove.gameObject);

        var so = new SerializedObject(playerMove);
        SetReference(so, "menuWindow", window);
        SetReference(so, "playerStats", stats);
        so.ApplyModifiedProperties();

        var quickSlot = playerMove.GetComponent<PlayerQuickSlot>();
        if (quickSlot != null)
        {
            var inventory = Object.FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
            var quickSo = new SerializedObject(quickSlot);
            SetReference(quickSo, "inventory", inventory);
            quickSo.ApplyModifiedProperties();
        }
    }

    private static void SetReference(SerializedObject so, string propertyName, Object value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[MenuWindowGenerator] {so.targetObject.GetType().Name}에 {propertyName} 필드가 없습니다.");
            return;
        }

        prop.objectReferenceValue = value;
    }

    // ───────────────────────── 찾기 헬퍼 ─────────────────────────

    private static Transform FindByNameRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindByNameRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
