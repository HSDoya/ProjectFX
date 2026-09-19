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

    public static GameObject SaveAndDestroy(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
