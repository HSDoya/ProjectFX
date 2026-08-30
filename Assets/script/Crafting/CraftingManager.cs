using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 아이템 시스템은 ItemDataManager(조회 전용)와 Inventory(실제 보유 데이터)가 분리되어 있지만,
// 그건 아이템 조회가 필요한 곳이 FieldItem/ItemPickup/PlayerMove 등 여러 군데라서 그렇다.
// 레시피 조회가 필요한 곳은 이 매니저(그리고 이걸 통해 묻는 UI)뿐이라, 별도의 RecipeDataManager를
// 새로 만들지 않고 이 클래스가 RecipeDatabaseSO를 직접 들고 있는다 - 굳이 나누면 의미 없는 이중 계층만 생긴다.
public class CraftingManager : MonoBehaviour
{
    public static CraftingManager instance;

    [SerializeField] private RecipeDatabaseSO database;

    // 연타/중복 실행 방지 (Tile_Fishing의 isFishing과 동일한 패턴)
    private bool isCrafting = false;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one CraftingManager found!");
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (database != null) database.Initialize();
    }

    public RecipeData GetRecipeByID(string recipeID)
    {
        return database != null ? database.GetRecipeByID(recipeID) : null;
    }

    // UI가 목록을 그릴 때 사용. CraftingUI는 이 메서드를 통해서만 레시피 목록에 접근하고,
    // RecipeDatabaseSO 참조를 따로 들고 있지 않는다 (EquipmentManager/핫바 이원화 버그의 재발 방지).
    public List<RecipeData> GetAllRecipes()
    {
        return database != null ? database.allRecipes : new List<RecipeData>();
    }

    // 재료를 실제로 깎지 않고 충족 여부만 확인 (UI의 목록 회색 처리, 제작 버튼 활성/비활성에 사용)
    public bool CanCraft(RecipeData recipe)
    {
        if (recipe == null || Inventory.instance == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) continue;
            if (Inventory.instance.GetItemCount(ingredient.item.itemID) < ingredient.amount)
                return false;
        }
        return true;
    }

    public void Craft(RecipeData recipe)
    {
        if (isCrafting) return;
        if (recipe == null || recipe.resultItem == null) return;
        if (!CanCraft(recipe)) return;

        StartCoroutine(CraftRoutine(recipe));
    }

    private IEnumerator CraftRoutine(RecipeData recipe)
    {
        isCrafting = true;

        if (recipe.craftTime > 0f)
            yield return new WaitForSeconds(recipe.craftTime);

        // 대기 시간 동안 재료를 다른 데 써버렸을 수 있으니 지급 직전에 다시 확인
        if (!CanCraft(recipe))
        {
            Debug.Log("제작 중 재료가 부족해져서 제작이 취소되었습니다.");
            isCrafting = false;
            yield break;
        }

        // ★ 결과물을 먼저 지급 시도 - 인벤토리가 가득 차서 실패하면 재료를 소모하지 않는다.
        //   (반대 순서로 하면 인벤토리가 꽉 찼을 때 재료만 사라지고 결과물은 못 받는 소실 버그가 생긴다.)
        var resultItem = new Item(recipe.resultItem, recipe.resultAmount);
        if (!Inventory.instance.AddItem(resultItem))
        {
            Debug.Log("인벤토리가 가득 차서 제작할 수 없습니다.");
            isCrafting = false;
            yield break;
        }

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) continue;
            Inventory.instance.TryConsumeItems(ingredient.item.itemID, ingredient.amount);
        }

        Debug.Log($"{recipe.resultItem.displayName} 제작 완료!");
        isCrafting = false;
    }
}
