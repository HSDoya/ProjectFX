using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 실제 HUD 아트 에셋이 아직 없는 상태에서 HP/스태미너/EXP 표시를 기능적으로 테스트할 수 있도록,
// 화면 좌상단에 얹는 "미니멀 씬 바" 스타일 HUD를 코드로 직접 조립한다 (CraftingUIGenerator와
// 동일한 방식 - 공용 헬퍼는 EditorUIBuilder를 그대로 재사용해서 코드 중복이 생기지 않게 했다).
public static class PlayerHUDGenerator
{
    private const string PrefabFolder = "Assets/script/Player/Prefabs";

    private static readonly Color LevelColor = new Color(1f, 0.804f, 0.235f);      // #FFCD3C
    private static readonly Color HpLabelColor = new Color(1f, 0.702f, 0.671f);    // #FFB3AB
    private static readonly Color HpFillColor = new Color(0.937f, 0.325f, 0.314f); // #EF5350
    private static readonly Color StaLabelColor = new Color(0.718f, 0.910f, 0.643f); // #B7E8A4
    private static readonly Color StaFillColor = new Color(0.400f, 0.733f, 0.416f);  // #66BB6A
    private static readonly Color ExpLabelColor = new Color(0.804f, 0.741f, 0.969f); // #CDBDF7
    private static readonly Color ExpFillColor = new Color(0.584f, 0.459f, 0.827f);  // #9575CD
    private static readonly Color TrackColor = new Color(0f, 0f, 0f, 0.5f);
    private static readonly Color TextShadowColor = new Color(0f, 0f, 0f, 0.8f);
    private static readonly Color PanelColor = new Color(0.06f, 0.06f, 0.07f, 0.6f);

    [MenuItem("Tools/플레이어 HUD 테스트용 생성")]
    public static void GeneratePlayerHUD()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/script/Player"))
            {
                Debug.LogError("[PlayerHUDGenerator] Assets/script/Player 폴더가 없습니다.");
                return;
            }
            AssetDatabase.CreateFolder("Assets/script/Player", "Prefabs");
        }

        GameObject hudPrefab = BuildHud();

        TryInstantiateIntoScene(hudPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>성공!</color> 플레이어 HUD 테스트용 프리팹을 생성했습니다 (Assets/script/Player/Prefabs). " +
                   "HP는 PlayerMove 실제 값을 바로 반영하고, 스태미너/EXP는 아직 시스템이 없어서 테스트용 기본값만 표시합니다.");
    }

    private static GameObject BuildHud()
    {
        var root = EditorUIBuilder.CreateChildRect("PlayerHUD", null);
        root.anchorMin = new Vector2(0, 1);
        root.anchorMax = new Vector2(0, 1);
        root.pivot = new Vector2(0, 1);
        root.anchoredPosition = new Vector2(28, -28);
        root.sizeDelta = new Vector2(340, 0);

        // 뒤에 반투명 판을 깔아서 배경이 밝은 지형(하늘/물 등) 위에서도 바가 잘 보이게 한다.
        var panelImage = root.gameObject.AddComponent<Image>();
        panelImage.color = PanelColor;
        panelImage.raycastTarget = false;

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        // childControlHeight를 켜야 각 줄이 실제로 자기 LayoutElement/텍스트 높이만큼 자리를 차지한다.
        // (꺼두면 자식들이 방금 만들어진 RectTransform의 기본 높이인 0으로 남아 전부 겹쳐 보인다.)
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var hud = root.gameObject.AddComponent<PlayerHUD>();

        var levelText = EditorUIBuilder.CreateChildText("LevelText", root, "LV. 1", 22, LevelColor, TextAlignmentOptions.MidlineLeft);
        levelText.fontStyle = FontStyles.Bold;
        levelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
        AddTextShadow(levelText);
        hud.levelText = levelText;

        hud.hpFill = BuildStatRow(root, "HPRow", "HP", HpLabelColor, HpFillColor, 14, showValue: true, out var hpValue);
        hud.hpValueText = hpValue;

        hud.staminaFill = BuildStatRow(root, "StaminaRow", "STA", StaLabelColor, StaFillColor, 14, showValue: true, out var staValue);
        hud.staminaValueText = staValue;

        hud.expFill = BuildStatRow(root, "ExpRow", "EXP", ExpLabelColor, ExpFillColor, 10, showValue: false, out _);

        return EditorUIBuilder.SaveAndDestroy(root.gameObject, $"{PrefabFolder}/PlayerHUD.prefab");
    }

    // 라벨 + 바(Track/Fill) + (선택)수치 텍스트로 이루어진 한 줄을 만들고, 채움 정도를 나타내는
    // Fill 이미지를 돌려준다. 실제 채움은 PlayerHUD.SetFillRatio가 런타임에 anchorMax.x로 조절한다.
    private static Image BuildStatRow(RectTransform parent, string rowName, string label, Color labelColor, Color fillColor,
        float barHeight, bool showValue, out TextMeshProUGUI valueText)
    {
        var row = EditorUIBuilder.CreateChildRect(rowName, parent);
        // 줄(row) 자체의 높이는 지정하지 않는다 - HorizontalLayoutGroup이 라벨 텍스트의 자연스러운
        // 높이를 기준으로 알아서 계산해준다. 대신 얇은 바(Track)만 barHeight로 따로 고정한다.

        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        // false로 둬야 Track이 barHeight 그대로 얇게 유지되고, 라벨 텍스트 높이에 맞춰 세로 중앙 정렬된다.
        rowLayout.childForceExpandHeight = false;

        var labelText = EditorUIBuilder.CreateChildText("Label", row, label, 13, labelColor, TextAlignmentOptions.MidlineLeft);
        labelText.fontStyle = FontStyles.Bold;
        var labelLayoutEl = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayoutEl.minWidth = 40;
        labelLayoutEl.preferredWidth = 40;
        AddTextShadow(labelText);

        var track = EditorUIBuilder.CreateChildImage("Track", row, TrackColor, fixedWidth: 0);
        track.raycastTarget = false; // 화면 구석 HUD가 게임 클릭(공격/채집 등)을 가로채면 안 되므로 꺼둔다
        var trackLayoutEl = track.gameObject.AddComponent<LayoutElement>();
        trackLayoutEl.flexibleWidth = 1;
        trackLayoutEl.minHeight = barHeight;
        trackLayoutEl.preferredHeight = barHeight;

        var fill = EditorUIBuilder.CreateChildImage("Fill", track.transform, fillColor, fixedWidth: 0);
        fill.raycastTarget = false;
        var fillRt = fill.rectTransform;
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        var valueLayoutEl = EditorUIBuilder.CreateChildRect("Value", row).gameObject.AddComponent<LayoutElement>();
        valueLayoutEl.minWidth = 38;
        valueLayoutEl.preferredWidth = 38;

        if (showValue)
        {
            var value = valueLayoutEl.gameObject.AddComponent<TextMeshProUGUI>();
            value.text = "0";
            value.fontSize = 13;
            value.fontStyle = FontStyles.Bold;
            value.color = Color.white;
            value.alignment = TextAlignmentOptions.MidlineRight;
            AddTextShadow(value);
            valueText = value;
        }
        else
        {
            valueText = null;
        }

        return fill;
    }

    private static void AddTextShadow(TextMeshProUGUI text)
    {
        text.raycastTarget = false; // 화면 구석 HUD가 게임 클릭(공격/채집 등)을 가로채면 안 되므로 꺼둔다
        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = TextShadowColor;
        shadow.effectDistance = new Vector2(1, -1);
    }

    // ───────────────────────── 씬에 배치 ─────────────────────────

    private static void TryInstantiateIntoScene(GameObject hudPrefab)
    {
        var existing = Object.FindFirstObjectByType<PlayerHUD>();
        if (existing != null)
        {
            Debug.LogWarning("[PlayerHUDGenerator] 씬에 이미 PlayerHUD가 있어서 새로 배치하지 않았습니다. 필요하면 기존 것을 지우고 다시 실행하세요.");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[PlayerHUDGenerator] 씬에 Canvas가 없어서 프리팹만 만들고 씬에는 배치하지 않았습니다. Canvas 아래에 PlayerHUD 프리팹을 직접 끌어넣어주세요.");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Create PlayerHUD");
        // 인벤토리/제작창과 달리 HUD는 항상 떠 있어야 하는 요소라 기본 활성 상태로 둔다.

        WireIntoPlayerMove(instance.GetComponent<PlayerHUD>());

        EditorSceneManager.MarkSceneDirty(instance.scene);
        Debug.Log("[PlayerHUDGenerator] PlayerHUD를 씬의 Canvas 아래에 배치했습니다.");
    }

    // PlayerHUD.playerMove는 [SerializeField] private라 SerializedObject를 통해서만 채울 수 있다.
    private static void WireIntoPlayerMove(PlayerHUD hud)
    {
        var playerMove = Object.FindFirstObjectByType<PlayerMove>();
        if (playerMove == null)
        {
            Debug.LogWarning("[PlayerHUDGenerator] 씬에서 PlayerMove를 찾지 못해 playerMove 필드를 자동 연결하지 못했습니다. 수동으로 연결해주세요.");
            return;
        }

        var so = new SerializedObject(hud);
        var prop = so.FindProperty("playerMove");
        if (prop == null)
        {
            Debug.LogWarning("[PlayerHUDGenerator] PlayerHUD에 playerMove 필드가 없습니다.");
            return;
        }

        prop.objectReferenceValue = playerMove;
        so.ApplyModifiedProperties();
    }
}
