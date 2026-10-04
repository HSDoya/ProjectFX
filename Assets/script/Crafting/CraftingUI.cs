using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 제작 패널 컨트롤러. RecipeDatabaseSO를 직접 들고 있지 않고 CraftingManager를 통해서만
// 레시피 목록을 조회한다 (핫바/장비창 이원화 버그가 여기서 재발하지 않도록).
public class CraftingUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panelRoot;
    public Button closeButton;

    [Header("Recipe List")]
    public Transform recipeListContent;
    public RecipeSlotUI recipeRowPrefab;

    [Header("Detail")]
    public Image resultIcon;
    public TextMeshProUGUI resultNameText;
    public TextMeshProUGUI resultAmountText;
    public Transform ingredientListContent;
    public IngredientRowUI ingredientRowPrefab;
    public Button craftButton;
    public TextMeshProUGUI craftStatusText;

    [Header("Craft Progress")]
    public GameObject progressRoot;   // 제작 중일 때만 보이는 진행 바 전체
    public Image progressFill;        // anchorMax.x로 채워지는 막대 (PlayerHUD의 게이지와 동일한 방식)

    public bool IsOpen { get; private set; }

    private RecipeData selectedRecipe;

    // 제작이 끝나는 순간을 잡아내 목록/재료 표시를 갱신하기 위한 직전 프레임 상태
    private bool wasCrafting;
    private readonly List<RecipeSlotUI> spawnedRows = new List<RecipeSlotUI>();
    private readonly List<IngredientRowUI> spawnedIngredientRows = new List<IngredientRowUI>();

    private void Awake()
    {
        if (craftButton != null)
            craftButton.onClick.AddListener(OnCraftButtonClicked);

        // 에디터 생성기에서 AddListener로 걸면 프리팹에 저장이 안 되므로(직렬화 안 됨), 여기서 런타임에 직접 연결한다.
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (Inventory.instance != null)
            Inventory.instance.onItemChangedCallback += RefreshAll;

        BuildRecipeList();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (Inventory.instance != null)
            Inventory.instance.onItemChangedCallback -= RefreshAll;
    }

    // 진행 상태는 CraftingManager가 단일 소유자라, 여기서는 매 프레임 읽어서 그리기만 한다.
    private void Update()
    {
        var manager = CraftingManager.instance;
        if (manager == null) return;

        bool crafting = manager.IsCrafting;

        if (progressRoot != null) progressRoot.SetActive(crafting);
        if (crafting && progressFill != null)
        {
            var anchorMax = progressFill.rectTransform.anchorMax;
            anchorMax.x = manager.CraftProgress01;
            progressFill.rectTransform.anchorMax = anchorMax;
        }

        if (crafting)
        {
            if (craftButton != null) craftButton.interactable = false;
            if (craftStatusText != null)
                craftStatusText.text = $"Crafting... {manager.RemainingTime:0.0}s";
        }

        // 제작이 끝난 프레임에 버튼/재료 표시를 원래 상태로 되돌린다.
        if (wasCrafting && !crafting) RefreshAll();
        wasCrafting = crafting;
    }

    public void ToggleUI()
    {
        IsOpen = !IsOpen;
        if (panelRoot != null) panelRoot.SetActive(IsOpen);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void BuildRecipeList()
    {
        if (recipeListContent == null || recipeRowPrefab == null || CraftingManager.instance == null) return;

        foreach (var row in spawnedRows)
            if (row != null) Destroy(row.gameObject);
        spawnedRows.Clear();

        foreach (var recipe in CraftingManager.instance.GetAllRecipes())
        {
            var row = Instantiate(recipeRowPrefab, recipeListContent);
            row.Setup(recipe, this);
            spawnedRows.Add(row);
        }

        if (selectedRecipe == null && spawnedRows.Count > 0)
            selectedRecipe = spawnedRows[0].Recipe;
    }

    public void SelectRecipe(RecipeData recipe)
    {
        selectedRecipe = recipe;
        RefreshDetail();
    }

    private void RefreshAll()
    {
        foreach (var row in spawnedRows)
            row.RefreshCraftableState();
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        foreach (var row in spawnedIngredientRows)
            if (row != null) Destroy(row.gameObject);
        spawnedIngredientRows.Clear();

        if (selectedRecipe == null || selectedRecipe.resultItem == null)
        {
            if (resultIcon != null) resultIcon.enabled = false;
            if (resultNameText != null) resultNameText.text = "";
            if (resultAmountText != null) resultAmountText.text = "";
            if (craftButton != null) craftButton.interactable = false;
            if (craftStatusText != null) craftStatusText.text = "";
            return;
        }

        if (resultIcon != null)
        {
            resultIcon.sprite = selectedRecipe.resultItem.icon;
            resultIcon.enabled = resultIcon.sprite != null;
        }
        // TMP 기본 폰트에 한글 글리프가 없어서 displayName(한글) 대신 영문인 itemID로 표시 (테스트용)
        if (resultNameText != null) resultNameText.text = selectedRecipe.resultItem.itemID;
        if (resultAmountText != null) resultAmountText.text = $"x{selectedRecipe.resultAmount}";

        if (ingredientListContent != null && ingredientRowPrefab != null && Inventory.instance != null)
        {
            foreach (var ingredient in selectedRecipe.ingredients)
            {
                if (ingredient.item == null) continue;
                int have = Inventory.instance.GetItemCount(ingredient.item.itemID);
                var row = Instantiate(ingredientRowPrefab, ingredientListContent);
                row.Setup(ingredient.item, have, ingredient.amount);
                spawnedIngredientRows.Add(row);
            }
        }

        bool craftable = CraftingManager.instance != null && CraftingManager.instance.CanCraft(selectedRecipe);
        bool crafting = CraftingManager.instance != null && CraftingManager.instance.IsCrafting;

        // 제작 중에는 버튼을 잠근다. (예전엔 눌리기만 하고 아무 일도 안 일어나 먹통처럼 보였다.)
        if (craftButton != null) craftButton.interactable = craftable && !crafting;

        // 제작 중 남은 시간 표시는 Update가 담당하므로 여기서 덮어쓰지 않는다.
        if (craftStatusText != null && !crafting) craftStatusText.text = craftable ? "" : "Not enough materials";
    }

    private void OnCraftButtonClicked()
    {
        if (selectedRecipe == null || CraftingManager.instance == null) return;
        CraftingManager.instance.Craft(selectedRecipe);
    }
}
