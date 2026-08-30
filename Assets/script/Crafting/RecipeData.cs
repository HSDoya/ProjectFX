using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewRecipeData", menuName = "Crafting/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [System.Serializable]
    public class Ingredient
    {
        public ItemData item;
        public int amount = 1;
    }

    public string recipeID;

    [Header("결과물")]
    public ItemData resultItem;
    public int resultAmount = 1;

    [Header("재료")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Header("제작 시간(초, 0이면 즉시 제작)")]
    public float craftTime = 0f;
}
