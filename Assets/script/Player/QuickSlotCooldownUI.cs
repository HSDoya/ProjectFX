using UnityEngine;
using UnityEngine.UI;

// 선택된 퀵슬롯 칸 위에 공격/도구 쿨타임을 어두운 막으로 덮어 보여준다.
// 막은 위에서부터 걷히며, 완전히 사라지면 다시 사용할 수 있다는 뜻이다.
//
// 오버레이를 슬롯 프리팹에 넣지 않고 런타임에 하나만 만들어 선택된 칸으로 옮겨 다니게 했다.
// 슬롯 UI(ItemUI)는 인벤토리/퀵슬롯/HUD 퀵슬롯이 공용으로 쓰는 프리팹이라, 거기에 넣으면
// 인벤토리 칸에도 오버레이가 따라붙기 때문이다.
public class QuickSlotCooldownUI : MonoBehaviour
{
    [Header("표시 설정")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.6f);

    private PlayerMove playerMove;
    private PlayerQuickSlot quickSlot;
    private InventoryUI inventoryUI;

    private RectTransform overlay;
    private int attachedSlotIndex = -1;

    private void Awake()
    {
        playerMove = FindFirstObjectByType<PlayerMove>();
        quickSlot = FindFirstObjectByType<PlayerQuickSlot>();
        inventoryUI = FindFirstObjectByType<InventoryUI>();

        CreateOverlay();
    }

    private void CreateOverlay()
    {
        var go = new GameObject("QuickSlotCooldownOverlay");
        overlay = go.AddComponent<RectTransform>();

        var image = go.AddComponent<Image>();
        image.color = overlayColor;
        image.raycastTarget = false; // 슬롯 클릭/드래그를 가로채지 않도록

        go.SetActive(false);
    }

    private void Update()
    {
        if (overlay == null || playerMove == null || quickSlot == null || inventoryUI == null) return;

        float ratio = playerMove.ActionCooldownRatio01;
        int index = quickSlot.selectedQuickSlotIndex;

        if (ratio <= 0f || index < 0)
        {
            if (overlay.gameObject.activeSelf) overlay.gameObject.SetActive(false);
            return;
        }

        RectTransform slot = inventoryUI.GetHudQuickSlotRect(index);
        if (slot == null)
        {
            if (overlay.gameObject.activeSelf) overlay.gameObject.SetActive(false);
            return;
        }

        // 선택된 칸이 바뀌었을 때만 부모를 옮긴다. 자식으로 붙여두면 칸 크기가 바뀌어도
        // 앵커가 알아서 따라가므로 좌표 계산이 필요 없다.
        if (attachedSlotIndex != index || overlay.parent != slot)
        {
            overlay.SetParent(slot, false);
            overlay.SetAsLastSibling(); // 아이콘 위에 그려지도록
            attachedSlotIndex = index;
        }

        // 남은 비율만큼 위쪽을 덮는다(아래에서 위로 걷히는 모양).
        overlay.anchorMin = new Vector2(0f, 1f - ratio);
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;

        if (!overlay.gameObject.activeSelf) overlay.gameObject.SetActive(true);
    }
}
