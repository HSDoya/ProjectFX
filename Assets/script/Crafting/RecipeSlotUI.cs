using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 왼쪽 레시피 목록의 개별 행. 결과 아이템 아이콘+이름 표시, 제작 가능 여부에 따라 배경색만 다르게 처리한다.
// (테스트용 UI라 실제 아트 에셋 없이 기본 Image/Text로만 구성됨 - 나중에 디자인 붙일 때 교체)
public class RecipeSlotUI : MonoBehaviour
{
    public Image background;
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public Button button;

    public RecipeData Recipe { get; private set; }

    private static readonly Color CraftableColor = Color.white;
    private static readonly Color LockedColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);

    public void Setup(RecipeData recipe, CraftingUI owner)
    {
        Recipe = recipe;

        if (iconImage != null)
        {
            iconImage.sprite = recipe.resultItem != null ? recipe.resultItem.icon : null;
            iconImage.enabled = iconImage.sprite != null;
        }
        // TMP 기본 폰트에 한글 글리프가 없어서 displayName(한글) 대신 영문인 itemID로 표시 (테스트용)
        if (nameText != null)
            nameText.text = recipe.resultItem != null ? recipe.resultItem.itemID : recipe.recipeID;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => owner.SelectRecipe(Recipe));
        }

        RefreshCraftableState();
    }

    public void RefreshCraftableState()
    {
        bool craftable = CraftingManager.instance != null && CraftingManager.instance.CanCraft(Recipe);
        if (background != null)
            background.color = craftable ? CraftableColor : LockedColor;
    }
}
