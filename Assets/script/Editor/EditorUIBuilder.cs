using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

// 코드로 UI 계층을 조립하는 에디터 툴들(CraftingUIGenerator, PlayerHUDGenerator 등)이
// 공통으로 쓰는 GameObject/컴포넌트 생성 헬퍼. 제너레이터마다 각자 들고 있으면 똑같은
// 코드가 중복되므로 여기 하나로 모아둔다.
public static class EditorUIBuilder
{
    public static RectTransform CreateChildRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
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

    // 재료 한 줄(아이콘 + 이름 + "보유/필요" 숫자) 프리팹.
    // 제작창(CraftingUIGenerator)과 건설 현장 패널(BuildingSiteUIGenerator)이 같은 줄을 쓰므로
    // 레이아웃을 여기 한 곳에서만 만든다. 어느 쪽 제너레이터를 돌려도 같은 프리팹이 나온다.
    public static IngredientRowUI BuildIngredientRowPrefab()
    {
        if (!EnsureFolder(IngredientRowPrefabFolder)) return null;

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
