using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image icon;

    public void SetItem(ItemData data)
    {
        if (icon == null)
        {
            Debug.LogError("ItemUI: icon 연결 안됨!");
            return;
        }

        if (data == null)
        {
            Debug.LogError("ItemUI: ItemData가 null입니다!");
            return;
        }

        if (data.icon == null)
        {
            Debug.LogWarning("ItemUI: ItemData.icon이 null입니다! → " + data.displayName);
            return;
        }

        icon.sprite = data.icon;
        icon.enabled = true;

        Debug.Log("[ItemUI] 아이콘 적용됨: " + data.displayName);
    }

    public void ClearSlot()
    {
        icon.sprite = null;
        icon.enabled = false;
    }

}
