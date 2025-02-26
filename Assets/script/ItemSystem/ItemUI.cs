using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image icon; // ������ �̹��� ǥ��
    private Item item; // ���� ������ ������

    // ������ ���� ����
    public void SetItem(Item newItem, Sprite newIcon)
    {
        item = newItem;
        icon.sprite = newIcon;
        icon.enabled = true; // ������ Ȱ��ȭ
    }

    // ���� ����
    public void ClearSlot()
    {
        item = null;
        icon.sprite = null;
        icon.enabled = false; // ������ ��Ȱ��ȭ
    }
}
