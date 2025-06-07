using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI countText;  // ���� ǥ�ÿ� �ؽ�Ʈ (UI���� ���� �ʿ�)

    public void SetItem(Item item)
    {
        if (icon == null || item == null || item.data == null)
            return;

        icon.sprite = item.data.icon;
        icon.enabled = true;

        if (countText != null)
        {
            countText.text = item.quantity.ToString(); // �׻� ǥ��
        }

        Debug.Log($"[ItemUI] ������ �����: {item.data.displayName}, ����: {item.quantity}");
    }

    public void ClearSlot()
    {
        icon.sprite = null;
        icon.enabled = false;

        if (countText != null)
            countText.text = "";
    }
}
