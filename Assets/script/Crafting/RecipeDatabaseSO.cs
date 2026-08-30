using System.Collections.Generic;
using UnityEngine;

// ItemDatabaseSO와 동일한 패턴: 리스트로 관리하다가 런타임에 Dictionary로 초기화해서 빠르게 조회한다.
[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Crafting/Recipe Database")]
public class RecipeDatabaseSO : ScriptableObject
{
    public List<RecipeData> allRecipes = new List<RecipeData>();

    private Dictionary<string, RecipeData> recipeDict = new Dictionary<string, RecipeData>();

    public void Initialize()
    {
        recipeDict.Clear();
        foreach (var recipe in allRecipes)
        {
            if (recipe != null && !recipeDict.ContainsKey(recipe.recipeID))
            {
                recipeDict.Add(recipe.recipeID, recipe);
            }
        }
    }

    public RecipeData GetRecipeByID(string id)
    {
        if (recipeDict.Count == 0) Initialize(); // 초기화 보장

        recipeDict.TryGetValue(id, out var recipe);
        return recipe;
    }
}
