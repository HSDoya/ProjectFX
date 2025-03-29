using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image icon;

    public void SetItem(ItemData data)
    {
        if (icon == null)
        {
            Debug.LogError("ItemUI: icon ���� �ȵ�!");
            return;
        }

        if (data == null)
        {
            Debug.LogError("ItemUI: ItemData�� null�Դϴ�!");
            return;
        }

        if (data.icon == null)
        {
            Debug.LogWarning("ItemUI: ItemData.icon�� null�Դϴ�! �� " + data.displayName);
            return;
        }

        icon.sprite = data.icon;
        icon.enabled = true;

        Debug.Log("[ItemUI] ������ �����: " + data.displayName);
    }

    public void ClearSlot()
    {
        icon.sprite = null;
        icon.enabled = false;
    }

}
