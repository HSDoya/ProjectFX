using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI countText;  // 수량 표시용 텍스트 (UI에서 연결 필요)

    public void SetItem(Item item)
    {
        if (icon == null || item == null || item.data == null)
            return;

        icon.sprite = item.data.icon;
        icon.enabled = true;

        if (countText != null)
        {
            countText.text = item.quantity > 1 ? item.quantity.ToString() : ""; // 수량이 1 초과일 때만 표시
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
