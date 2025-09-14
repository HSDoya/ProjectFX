using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI countText;  // 수량 표시용 텍스트 (UI에서 연결 필요)
                                       // ItemUI.cs (선택 보강)
    private void Awake()
    {
        if (icon == null)
            icon = GetComponentInChildren<Image>(true);
        if (countText == null)
            countText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (icon != null) icon.preserveAspect = true; // 넘침 방지에 도움
    }

    public void SetItem(Item item)
    {
        if (icon == null || item == null || item.data == null)
            return;
        if (item.data.icon == null)
            Debug.LogError($"[{item.data.itemID}] 아이콘이 null입니다.");

        icon.sprite = item.data.icon;
        icon.enabled = true;

        if (countText != null)
        {
            countText.text = item.quantity.ToString(); // 항상 표시
        }

        Debug.Log($"[ItemUI] 아이콘 적용됨: {item.data.displayName}, 수량: {item.quantity}");
    }

    public void ClearSlot()
    {
        icon.sprite = null;
        icon.enabled = false;

        if (countText != null)
            countText.text = "";
    }
}
