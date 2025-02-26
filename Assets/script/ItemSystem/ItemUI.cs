using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image icon; // 아이템 이미지 표시
    private Item item; // 현재 슬롯의 아이템

    // 아이템 정보 설정
    public void SetItem(Item newItem, Sprite newIcon)
    {
        item = newItem;
        icon.sprite = newIcon;
        icon.enabled = true; // 아이콘 활성화
    }

    // 슬롯 비우기
    public void ClearSlot()
    {
        item = null;
        icon.sprite = null;
        icon.enabled = false; // 아이콘 비활성화
    }
}
