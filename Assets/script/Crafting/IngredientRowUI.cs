using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 상세 패널의 재료 한 줄. "보유/필요" 색깔 텍스트가 ItemUI(icon+count 하나만 표시)로는 표현이 안 되는
// 모양이라 별도 컴포넌트로 뺐다 - ItemUI를 억지로 확장하면 인벤토리/장비 슬롯 쪽에 안 쓰는 필드만 늘어난다.
public class IngredientRowUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;

    private static readonly Color EnoughColor = new Color(0.25f, 0.55f, 0.25f);
    private static readonly Color LackingColor = new Color(0.75f, 0.25f, 0.25f);

    public void Setup(ItemData item, int have, int need)
    {
        if (iconImage != null)
        {
            iconImage.sprite = item != null ? item.icon : null;
            iconImage.enabled = iconImage.sprite != null;
        }
        // TMP 기본 폰트에 한글 글리프가 없어서 displayName(한글) 대신 영문인 itemID로 표시 (테스트용)
        if (nameText != null)
            nameText.text = item != null ? item.itemID : "?";

        if (countText != null)
        {
            countText.text = $"{have} / {need}";
            countText.color = have >= need ? EnoughColor : LackingColor;
        }
    }
}
