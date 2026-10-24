using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

// 코드로 UI 계층을 조립하는 에디터 툴들(CraftingUIGenerator, PlayerHUDGenerator 등)이
// 공통으로 쓰는 GameObject/컴포넌트 생성 헬퍼. 제너레이터마다 각자 들고 있으면 똑같은
// 코드가 중복되므로 여기 하나로 모아둔다.
public static class EditorUIBuilder
{
    // UI 레이어. new GameObject(...)는 항상 Default(0)로 만들어지는데, 씬의 다른 UI는 전부
    // UI 레이어에 있다. Screen Space - Overlay 캔버스에서는 레이어가 그리기/클릭에 영향을 주지
    // 않아 지금은 티가 안 나지만, 캔버스를 카메라 모드로 바꾸거나 컬링 마스크를 쓰는 순간
    // 코드로 만든 UI만 통째로 사라진다. 만들 때 맞춰둔다.
    private static readonly int UiLayer = LayerMask.NameToLayer("UI");

    public static RectTransform CreateChildRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (UiLayer >= 0) go.layer = UiLayer;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    public static Image CreateChildImage(string name, Transform parent, Color color, float fixedWidth)
    {
        var rt = CreateChildRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (fixedWidth > 0) rt.sizeDelta = new Vector2(fixedWidth, fixedWidth);
        return img;
    }

    public static TextMeshProUGUI CreateChildText(string name, Transform parent, string text, int fontSize, Color color, TextAlignmentOptions align)
    {
        var rt = CreateChildRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        return tmp;
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// ScrollRect 오른쪽에 붙일 세로 스크롤바를 만든다. 반환값을 ScrollRect.verticalScrollbar에 넣어주면 된다.
    /// 뷰포트를 이 폭만큼 좁히는 것은 부르는 쪽 몫이다(Permanent로 쓰면 ScrollRect가 대신 해주지 않는다).
    /// </summary>
    public static Scrollbar CreateVerticalScrollbar(RectTransform parent, float width)
    {
        var root = CreateChildRect("Scrollbar", parent);
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        root.sizeDelta = new Vector2(width, 0f);
        root.anchoredPosition = Vector2.zero;
        root.gameObject.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.13f);

        // 손잡이의 부모가 곧 Scrollbar의 이동 범위(m_ContainerRect)가 된다. 한 단계 더 있어야 한다.
        var slidingArea = CreateChildRect("Sliding Area", root);
        StretchFull(slidingArea);

        var handle = CreateChildImage("Handle", slidingArea, new Color(0.45f, 0.45f, 0.45f), fixedWidth: 0);
        // 앵커는 Scrollbar.UpdateVisuals가 value/size로 매번 덮어쓴다. 오프셋만 0이면 된다.
        StretchFull(handle.rectTransform);

        var scrollbar = root.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        return scrollbar;
    }

    // "Assets/a/b" 경로의 폴더가 없으면 상위부터 차례로 만든다. 이미 있으면 아무것도 하지 않는다.
    public static bool EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return true;

        string[] parts = folderPath.Split('/');
        if (parts.Length < 2 || parts[0] != "Assets")
        {
            Debug.LogError($"[EditorUIBuilder] 폴더 경로는 Assets로 시작해야 합니다: {folderPath}");
            return false;
        }

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }

        return AssetDatabase.IsValidFolder(folderPath);
    }

    public const string IngredientRowPrefabFolder = "Assets/script/Crafting/Prefabs";
    public const string IngredientRowPrefabPath = IngredientRowPrefabFolder + "/IngredientRow.prefab";

    // 재료 한 줄의 높이. LayoutElement와 프리팹 루트의 sizeDelta가 반드시 같은 값을 써야 한다.
    // 부모 레이아웃 그룹의 childControlHeight가 켜져 있으면 LayoutElement를, 꺼져 있으면
    // sizeDelta를 읽기 때문에, 둘이 다르면 부모가 누구냐에 따라 높이가 조용히 달라진다.
    private const float IngredientRowHeight = 42f;
    // 프리팹을 단독으로 열었을 때 보기 좋으라고 넣는 값. 실제 폭은 부모 목록이 매번 다시 잡는다.
    private const float IngredientRowWidth = 824f;

    // 재료 한 줄(아이콘 + 이름 + "보유/필요" 숫자) 프리팹.
    // 제작창(CraftingUIGenerator)과 건설 현장 패널(BuildingSiteUIGenerator)이 같은 줄을 쓰므로
    // 레이아웃을 여기 한 곳에서만 만든다. 어느 쪽 제너레이터를 돌려도 같은 프리팹이 나온다.
    public static IngredientRowUI BuildIngredientRowPrefab()
    {
        if (!EnsureFolder(IngredientRowPrefabFolder)) return null;

        var root = new GameObject("IngredientRow", typeof(RectTransform));
        // 유니티가 새 RectTransform에 넣는 기본값은 100x100이다. 그대로 두면 부모가 높이를
        // 제어하지 않을 때(childControlHeight=false) 아래 LayoutElement가 아니라 이 100이 쓰인다.
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(IngredientRowWidth, IngredientRowHeight);

        var layoutElement = root.AddComponent<LayoutElement>();
        layoutElement.minHeight = IngredientRowHeight;
        layoutElement.preferredHeight = IngredientRowHeight;

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        var iconImg = CreateChildImage("Icon", root.transform, Color.white, fixedWidth: 32);
        iconImg.preserveAspect = true;

        var nameText = CreateChildText("Name", root.transform, "", 18, new Color(0.15f, 0.15f, 0.15f), TextAlignmentOptions.MidlineLeft);
        nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var countText = CreateChildText("Count", root.transform, "", 18, Color.black, TextAlignmentOptions.MidlineRight);
        countText.fontStyle = FontStyles.Bold;
        countText.gameObject.AddComponent<LayoutElement>().minWidth = 90;

        var ingredientUI = root.AddComponent<IngredientRowUI>();
        ingredientUI.iconImage = iconImg;
        ingredientUI.nameText = nameText;
        ingredientUI.countText = countText;

        return SaveAndDestroy(root, IngredientRowPrefabPath).GetComponent<IngredientRowUI>();
    }

    public static GameObject SaveAndDestroy(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
