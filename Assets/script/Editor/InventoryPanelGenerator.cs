using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

// 아이템 UI를 코드에서 전부 조립한다.
//   - 탭 창의 Items 탭 내용 (인벤토리 칸 + 장비 칸 + 창 안 퀵슬롯 줄)
//   - 화면 하단에 항상 떠 있는 HUD 퀵슬롯 바
//   - 드래그 중인 아이콘(ItemDrag)
//   - 아이템 데이터 소유자(Inventory) - UI가 아니므로 씬 루트에 둔다
//
// 씬에 손으로 만들어 둔 UI를 찾아 옮겨 쓰지 않는다. 옮겨 쓰면 "그 UI가 씬에 남아 있을 때만"
// 결과가 맞아서, 한 번 지우고 나면 탭이 빈 채로 만들어진다. 여기서는 빈 씬에서도 메뉴 한 번으로
// 같은 결과가 나오게 전부 새로 만든다.
//
// 칸 하나는 에셋 팩의 Icon_inv 프리팹을 쓴다(아이콘 Image, 수량 텍스트, ItemUI, InventorySlotUI가
// 이미 연결돼 있다). 팩이 없는 환경이면 BuildPlainSlot이 같은 구성을 단색으로 대신 만든다.
public static class InventoryPanelGenerator
{
    public const string SlotPrefabPath = "Assets/Assets pack/EQuipment/Asset/inventoryPrefab/Icon_inv.prefab";

    // 칸 크기와 간격은 기존 씬에서 쓰던 값 그대로다.
    private static readonly Vector2 Cell = new Vector2(74.6f, 75f);
    private static readonly Vector2 Spacing = new Vector2(10f, 10f);

    private const float Pad = 28f;         // 패널 안쪽 여백
    private const float ColumnGap = 40f;   // 인벤토리 격자와 장비 칸 사이. 칸 간격(10)보다 확실히 넓어야
                                           // 장비 칸이 인벤토리의 한 열로 보이지 않는다
    private const float TitleHeight = 44f;
    private const float SectionGap = 20f;
    private const float LabelHeight = 24f;
    private const float BarPad = 14f;      // HUD 바 안쪽 여백

    private static readonly Color PanelColor = new Color(0.14f, 0.14f, 0.16f, 0.98f);
    private static readonly Color BarColor = new Color(0.1f, 0.1f, 0.11f, 0.85f);
    private static readonly Color LabelColor = new Color(0.92f, 0.9f, 0.84f);
    private static readonly Color SubColor = new Color(0.62f, 0.62f, 0.6f);
    private static readonly Color CaptionColor = new Color(0.2f, 0.19f, 0.17f);
    private static readonly Color EquipSlotTint = new Color(0.62f, 0.73f, 0.86f);
    private static readonly Color HighlightColor = new Color(1f, 0.85f, 0.35f, 0.45f);

    private static bool warnedAboutSlotPrefab;

    // ───────────────────────── Items 탭 ─────────────────────────

    /// <summary>
    /// Items 탭 패널이 차지하는 크기. 칸 수(Inventory의 상수)에서 계산되므로 인벤토리를
    /// 14x5가 아닌 크기로 바꾸면 이 값도 같이 바뀐다. 창 크기를 여기에 맞춰야 패널이
    /// 창 밖으로 삐져나가지 않는다.
    /// </summary>
    public static Vector2 ItemsPanelSize
    {
        get
        {
            float bodyW = Mathf.Max(GridWidth(Inventory.COL_COUNT), GridWidth(Inventory.QUICK_SLOT_SIZE))
                          + ColumnGap + Cell.x;
            float bodyH = TitleHeight + GridHeight(Inventory.ROW_COUNT) + SectionGap + LabelHeight + Cell.y;
            return new Vector2(bodyW + Pad * 2f, bodyH + Pad * 2f);
        }
    }

    /// <summary>탭 창의 Items 탭 내용을 만들어 돌려준다.</summary>
    public static GameObject BuildItemsPanel(RectTransform parent)
    {
        warnedAboutSlotPrefab = false;

        float gridW = GridWidth(Inventory.COL_COUNT);
        float gridH = GridHeight(Inventory.ROW_COUNT);
        float quickW = GridWidth(Inventory.QUICK_SLOT_SIZE);

        Vector2 panelSize = ItemsPanelSize;
        float bodyW = panelSize.x - Pad * 2f;

        var panel = EditorUIBuilder.CreateChildRect("InventoryPanel", parent);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = panelSize;
        panel.anchoredPosition = Vector2.zero;
        panel.gameObject.AddComponent<Image>().color = PanelColor;

        var ui = panel.gameObject.AddComponent<InventoryUI>();

        // 라벨은 영문이다. TMP 기본 폰트(LiberationSans)에 한글 글리프가 없어 한글은 깨져 보인다.
        var title = EditorUIBuilder.CreateChildText("Title", panel, "Inventory", 24, LabelColor,
                                                    TextAlignmentOptions.MidlineLeft);
        title.raycastTarget = false; // 장식용 글자가 칸 위를 덮어 드롭을 가로채지 않게
        PlaceTopLeft(title.rectTransform, Pad, Pad);
        title.rectTransform.sizeDelta = new Vector2(gridW, TitleHeight - 8f);

        // 인벤토리 14 x 5
        var itemGrid = MakeGrid("slot_inventory", panel, Inventory.COL_COUNT);
        itemGrid.sizeDelta = new Vector2(gridW, gridH);
        PlaceTopLeft(itemGrid, Pad, Pad + TitleHeight);
        for (int i = 0; i < Inventory.INVENTORY_SIZE; i++) NewSlot(itemGrid, "Slot_" + i);

        // 장비 칸: 격자 오른쪽에 세로 한 줄. 몇 칸을 만들지는 EquipmentManager가 정한다.
        // 이름은 칸 쪽(slot_Armor 등)과 겹치지 않게 둔다 - 계층에서 둘을 구분할 수 없으면
        // 하이라키에서 무엇을 보고 있는지 헷갈린다.
        float equipX = Pad + gridW + ColumnGap;

        var equipLabel = EditorUIBuilder.CreateChildText("EquipmentLabel", panel, "Equip", 16, LabelColor,
                                                         TextAlignmentOptions.Center);
        equipLabel.raycastTarget = false;
        PlaceTopLeft(equipLabel.rectTransform, equipX, Pad);
        equipLabel.rectTransform.sizeDelta = new Vector2(Cell.x, TitleHeight - 8f);

        var armorGrid = MakeGrid("EquipmentSlots", panel, 1);
        armorGrid.sizeDelta = new Vector2(Cell.x, GridHeight(EquipmentManager.OwnedSlots.Count));
        PlaceTopLeft(armorGrid, equipX, Pad + TitleHeight);
        BuildEquipmentSlots(armorGrid);

        // 창 안에서 보는 퀵슬롯 줄 (하단 HUD 바와 같은 데이터를 본다)
        float quickTop = Pad + TitleHeight + gridH + SectionGap;
        var quickLabel = EditorUIBuilder.CreateChildText("QuickSlotLabel", panel, "Quick slots", 17, SubColor,
                                                         TextAlignmentOptions.MidlineLeft);
        quickLabel.raycastTarget = false;
        PlaceTopLeft(quickLabel.rectTransform, Pad, quickTop);
        quickLabel.rectTransform.sizeDelta = new Vector2(bodyW, LabelHeight);

        var quickGrid = MakeGrid("slot_equipment", panel, Inventory.QUICK_SLOT_SIZE);
        quickGrid.sizeDelta = new Vector2(quickW, Cell.y);
        PlaceTopLeft(quickGrid, Pad, quickTop + LabelHeight);
        for (int i = 0; i < Inventory.QUICK_SLOT_SIZE; i++) NewSlot(quickGrid, "Quick_" + i);

        ui.itemGrid = itemGrid;
        ui.quickSlotGrid = quickGrid;

        return panel.gameObject;
    }

    private static void BuildEquipmentSlots(RectTransform grid)
    {
        foreach (var type in EquipmentManager.OwnedSlots)
        {
            var go = NewSlot(grid, "slot_" + type);

            // 칸 프리팹에는 인벤토리용 InventorySlotUI가 붙어 있다. 장비 칸은 받을 수 있는
            // 아이템 규칙이 달라서 EquipmentSlotUI로 바꿔 단다. 프리팹 인스턴스에서는
            // 컴포넌트를 지울 수 없으므로 연결을 먼저 푼다.
            if (PrefabUtility.IsPartOfPrefabInstance(go))
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var itemUI = go.GetComponent<ItemUI>();
            var inventorySlot = go.GetComponent<InventorySlotUI>();
            if (inventorySlot != null) Object.DestroyImmediate(inventorySlot);

            var equipmentSlot = go.AddComponent<EquipmentSlotUI>();
            equipmentSlot.slotType = type;
            equipmentSlot.itemUI = itemUI;

            // 장비 칸을 인벤토리 칸과 다른 색으로 물들인다. 같은 칸 프리팹을 쓰기 때문에
            // 색까지 같으면 인벤토리 격자의 한 열처럼 보여서, 방어구를 Armor가 아닌
            // 옆 칸(Hat 등)에 놓고 "안 껴진다"고 보게 된다.
            var slotImage = go.GetComponent<Image>();
            if (slotImage != null) slotImage.color = EquipSlotTint;

            // 어떤 칸인지 칸 안에 적는다. 아이템이 들어오면 아이콘이 위를 덮는다.
            var caption = EditorUIBuilder.CreateChildText("Caption", go.transform, type.ToString(), 14,
                                                           CaptionColor, TextAlignmentOptions.Center);
            caption.raycastTarget = false;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Overflow;
            // 칸 전체를 채우고 가운데 정렬한다. 예전엔 아래쪽 14px 띠에 11pt로 넣었는데
            // 실제 화면에서는 몇 픽셀이라 글자가 보이지 않았다.
            EditorUIBuilder.StretchFull(caption.rectTransform);
            caption.rectTransform.SetAsFirstSibling(); // 아이콘보다 뒤에 그려지게
        }
    }

    // ───────────────────────── HUD 퀵슬롯 바 ─────────────────────────

    /// <summary>
    /// 화면 하단에 항상 떠 있는 퀵슬롯 바. 루트(QickSlot)는 켜진 채로 두고, 창이 열릴 때는
    /// 자식 BG만 숨긴다 - 루트를 끄면 QuickSlotBarUI까지 멈춰서 다시 켜도 칸이 비어 있게 된다.
    /// </summary>
    public static GameObject BuildQuickSlotBar(Transform canvasRoot)
    {
        warnedAboutSlotPrefab = false;

        float barW = GridWidth(Inventory.QUICK_SLOT_SIZE) + BarPad * 2f;
        float barH = Cell.y + BarPad * 2f;

        var root = EditorUIBuilder.CreateChildRect("QickSlot", canvasRoot);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.sizeDelta = new Vector2(barW, barH);
        root.anchoredPosition = new Vector2(0f, 18f);

        var bg = EditorUIBuilder.CreateChildRect("BG", root);
        EditorUIBuilder.StretchFull(bg);
        bg.gameObject.AddComponent<Image>().color = BarColor;

        var grid = MakeGrid("Slots", bg, Inventory.QUICK_SLOT_SIZE);
        grid.sizeDelta = new Vector2(GridWidth(Inventory.QUICK_SLOT_SIZE), Cell.y);
        grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 0.5f);
        grid.pivot = new Vector2(0.5f, 0.5f);
        grid.anchoredPosition = Vector2.zero;

        var highlights = new List<Image>();
        for (int i = 0; i < Inventory.QUICK_SLOT_SIZE; i++)
        {
            var go = NewSlot(grid, "Quick_" + i);
            highlights.Add(AddHighlight(go.transform));
        }

        var bar = root.gameObject.AddComponent<QuickSlotBarUI>();
        SetPrivateReference(bar, "slotGrid", grid);

        WireHighlightsIntoQuickSlot(highlights);

        return root.gameObject;
    }

    private static Image AddHighlight(Transform slot)
    {
        var image = EditorUIBuilder.CreateChildImage("Highlight", slot, HighlightColor, fixedWidth: 0);
        EditorUIBuilder.StretchFull(image.rectTransform);
        image.raycastTarget = false;
        image.enabled = false;              // 선택된 칸만 PlayerQuickSlot이 켠다
        image.rectTransform.SetAsFirstSibling(); // 아이콘 뒤에 깔리도록
        return image;
    }

    // PlayerQuickSlot.slotHighlightImages는 [SerializeField] private라 SerializedObject로만 채울 수 있다.
    private static void WireHighlightsIntoQuickSlot(List<Image> highlights)
    {
        var quickSlot = Object.FindFirstObjectByType<PlayerQuickSlot>(FindObjectsInactive.Include);
        if (quickSlot == null)
        {
            Debug.LogWarning("[InventoryPanelGenerator] 씬에서 PlayerQuickSlot을 찾지 못해 선택 표시(하이라이트)를 연결하지 못했습니다.");
            return;
        }

        var so = new SerializedObject(quickSlot);
        var prop = so.FindProperty("slotHighlightImages");
        if (prop == null) return;

        prop.arraySize = highlights.Count;
        for (int i = 0; i < highlights.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = highlights[i];

        so.ApplyModifiedProperties();
    }

    // ───────────────────────── 드래그 아이콘 ─────────────────────────

    /// <summary>
    /// 드래그 중인 아이템 아이콘. ItemDragController는 이 오브젝트의 localPosition을 캔버스
    /// 좌표로 직접 옮기므로, 캔버스 바로 아래에 중앙 앵커로 두어야 좌표가 맞는다.
    /// </summary>
    public static GameObject BuildItemDrag(Canvas canvas)
    {
        var root = EditorUIBuilder.CreateChildRect("ItemDrag", canvas.transform);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = Vector2.zero;

        var icon = EditorUIBuilder.CreateChildImage("DragImage", root, Color.white, fixedWidth: 0);
        var iconRt = icon.rectTransform;
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(64f, 64f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        var controller = root.gameObject.AddComponent<ItemDragController>();
        controller.dragIconRoot = root;
        controller.dragIconImage = icon;
        controller.canvas = canvas;

        return root.gameObject;
    }

    // ───────────────────────── 아이템 데이터 ─────────────────────────

    /// <summary>
    /// 아이템 데이터를 소유하는 Inventory 컴포넌트를 씬 루트에 둔다. UI가 아니므로 Canvas 안에
    /// 있을 이유가 없고, Canvas 안에 있으면 쓸모없는 RectTransform까지 따라다닌다.
    /// 오브젝트를 새로 만들지 않고 부모만 바꾼다 - 새로 만들면 fileID가 달라져 PlayerQuickSlot의
    /// 직렬화 참조가 조용히 끊긴다(Start에 폴백이 있긴 하지만 끊을 이유가 없다).
    /// </summary>
    public static Inventory EnsureInventoryData()
    {
        var inventory = Object.FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (inventory == null)
        {
            var go = new GameObject("Inventory");
            Undo.RegisterCreatedObjectUndo(go, "Create Inventory");
            inventory = Undo.AddComponent<Inventory>(go);
            Debug.Log("[InventoryPanelGenerator] 아이템 데이터(Inventory)를 씬 루트에 만들었습니다.");
            return inventory;
        }

        Transform tf = inventory.transform;
        if (tf.parent != null && tf.GetComponentInParent<Canvas>() != null)
        {
            if (tf.childCount > 0)
            {
                Debug.LogWarning($"[InventoryPanelGenerator] {tf.name} 아래에 자식이 남아 있어 Canvas 밖으로 옮기지 않았습니다.");
            }
            else
            {
                Undo.SetTransformParent(tf, null, "Move Inventory data out of Canvas");
                Debug.Log("[InventoryPanelGenerator] 아이템 데이터(Inventory)를 Canvas 밖 씬 루트로 옮겼습니다.");
            }
        }

        return inventory;
    }

    // ───────────────────────── 공통 ─────────────────────────

    private static float GridWidth(int columns) => columns * Cell.x + (columns - 1) * Spacing.x;
    private static float GridHeight(int rows) => rows * Cell.y + (rows - 1) * Spacing.y;

    private static RectTransform MakeGrid(string name, Transform parent, int columns)
    {
        var rt = EditorUIBuilder.CreateChildRect(name, parent);
        var grid = rt.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = Cell;
        grid.spacing = Spacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        // 열 수를 고정한다. Flexible로 두면 칸 수가 아니라 rect 너비가 줄 바꿈을 결정해서,
        // 패널 크기를 조금만 건드려도 14x5가 조용히 12x6으로 바뀐다.
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        return rt;
    }

    private static void PlaceTopLeft(RectTransform rt, float x, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
    }

    private static GameObject NewSlot(Transform parent, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);

        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }
        else
        {
            if (!warnedAboutSlotPrefab)
            {
                warnedAboutSlotPrefab = true;
                Debug.LogWarning($"[InventoryPanelGenerator] 칸 프리팹을 찾지 못해({SlotPrefabPath}) 단색 칸으로 대체합니다.");
            }
            go = BuildPlainSlot(parent);
        }

        go.name = name;
        return go;
    }

    // 에셋 팩이 없는 환경용 대체 칸. Icon_inv 프리팹과 같은 구성을 단색으로 만든다.
    private static GameObject BuildPlainSlot(Transform parent)
    {
        var rt = EditorUIBuilder.CreateChildRect("Slot", parent);
        rt.gameObject.AddComponent<Image>().color = new Color(0.26f, 0.26f, 0.29f);

        var icon = EditorUIBuilder.CreateChildImage("Icon", rt, Color.white, fixedWidth: 0);
        EditorUIBuilder.StretchFull(icon.rectTransform);
        icon.rectTransform.offsetMin = new Vector2(6f, 6f);
        icon.rectTransform.offsetMax = new Vector2(-6f, -6f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        var count = EditorUIBuilder.CreateChildText("item_count", rt, "", 15, Color.white,
                                                     TextAlignmentOptions.BottomRight);
        EditorUIBuilder.StretchFull(count.rectTransform);
        count.rectTransform.offsetMin = new Vector2(0f, 2f);
        count.rectTransform.offsetMax = new Vector2(-4f, 0f);
        count.raycastTarget = false;

        var itemUI = rt.gameObject.AddComponent<ItemUI>();
        itemUI.icon = icon;
        itemUI.countText = count;

        var slot = rt.gameObject.AddComponent<InventorySlotUI>();
        slot.itemUI = itemUI;

        return rt.gameObject;
    }

    private static void SetPrivateReference(Object target, string propertyName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[InventoryPanelGenerator] {target.GetType().Name}에 {propertyName} 필드가 없습니다.");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
