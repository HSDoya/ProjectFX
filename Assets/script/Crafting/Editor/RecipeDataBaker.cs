using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RecipeDataBaker : EditorWindow
{
    private const string BakerName = "RecipeDataBaker";

    // CSV에 반드시 있어야 하는 컬럼들. 재료는 최대 3개까지 고정 컬럼으로 받는다 (빈 칸이면 그 재료 슬롯은 없는 것으로 스킵).
    private static readonly string[] RequiredColumns =
    {
        "recipeID", "resultItemID", "resultAmount", "craftTime",
        "ingredient1ID", "ingredient1Amount",
        "ingredient2ID", "ingredient2Amount",
        "ingredient3ID", "ingredient3Amount"
    };

    [MenuItem("Tools/레시피 데이터 굽기(CSV -> SO)")]
    public static void BakeRecipeData()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("RecipeDatabase");
        if (csvFile == null)
        {
            Debug.LogError("Resources 폴더에서 RecipeDatabase.csv 파일을 찾을 수 없습니다!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        if (lines.Length == 0)
        {
            Debug.LogError($"[{BakerName}] CSV 파일이 비어 있습니다.");
            return;
        }

        var colIndex = CsvBakeUtils.ParseHeader(lines[0]);
        if (!CsvBakeUtils.HasRequiredColumns(colIndex, RequiredColumns, BakerName)) return;

        string folderPath = "Assets/Resources/RecipeDataAssets";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "RecipeDataAssets");
        }

        List<RecipeData> bakedRecipes = new List<RecipeData>();
        HashSet<string> seenIds = new HashSet<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Split(',');
            if (cols.Length < colIndex.Count)
            {
                Debug.LogWarning($"[{BakerName}] CSV {i + 1}번째 줄의 컬럼 수가 부족합니다 (필요 {colIndex.Count}개, 실제 {cols.Length}개). 이 줄을 건너뜁니다.");
                continue;
            }

            string Col(string name) => cols[colIndex[name]].Trim();

            string recipeID = Col("recipeID");
            if (string.IsNullOrEmpty(recipeID))
            {
                Debug.LogWarning($"[{BakerName}] CSV {i + 1}번째 줄의 recipeID가 비어 있어 건너뜁니다.");
                continue;
            }

            // 중복 recipeID는 같은 .asset을 덮어써서 먼저 구운 레시피를 조용히 지워버리므로 건너뛴다.
            if (!seenIds.Add(recipeID))
            {
                Debug.LogWarning($"[{BakerName}] 중복된 recipeID '{recipeID}' (CSV {i + 1}번째 줄)를 건너뜁니다. recipeID는 고유해야 합니다.");
                continue;
            }

            string resultItemID = Col("resultItemID");
            ItemData resultItem = LoadItemDataAsset(resultItemID);
            if (resultItem == null)
            {
                Debug.LogWarning($"[{BakerName}] '{recipeID}'의 resultItemID '{resultItemID}'에 해당하는 아이템 에셋을 찾을 수 없어 건너뜁니다. " +
                                  $"(아이템을 먼저 '아이템 데이터 굽기'로 구웠는지 확인하세요, CSV {i + 1}번째 줄)");
                continue;
            }

            var ingredients = new List<RecipeData.Ingredient>();
            for (int slot = 1; slot <= 3; slot++)
            {
                string idCol = $"ingredient{slot}ID";
                string amtCol = $"ingredient{slot}Amount";

                string ingredientID = Col(idCol);
                if (string.IsNullOrEmpty(ingredientID)) continue; // 빈 재료 슬롯은 스킵 (재료 3개 미만인 레시피용)

                ItemData ingredientItem = LoadItemDataAsset(ingredientID);
                if (ingredientItem == null)
                {
                    Debug.LogWarning($"[{BakerName}] '{recipeID}'의 {idCol} '{ingredientID}'에 해당하는 아이템 에셋을 찾을 수 없어 이 재료를 건너뜁니다. (CSV {i + 1}번째 줄)");
                    continue;
                }

                int ingredientAmount = CsvBakeUtils.ParseIntOrWarn(Col(amtCol), recipeID, amtCol, i + 1, BakerName);
                if (ingredientAmount <= 0)
                {
                    Debug.LogWarning($"[{BakerName}] '{recipeID}'의 {amtCol}이(가) 0 이하라 이 재료를 건너뜁니다. (CSV {i + 1}번째 줄)");
                    continue;
                }

                ingredients.Add(new RecipeData.Ingredient { item = ingredientItem, amount = ingredientAmount });
            }

            if (ingredients.Count == 0)
            {
                Debug.LogWarning($"[{BakerName}] '{recipeID}'에 유효한 재료가 하나도 없어 건너뜁니다. (CSV {i + 1}번째 줄)");
                continue;
            }

            string assetPath = $"{folderPath}/{recipeID}.asset";
            RecipeData recipeData = CsvBakeUtils.GetOrCreateAsset<RecipeData>(assetPath);

            recipeData.recipeID = recipeID;
            recipeData.resultItem = resultItem;
            int parsedAmount = CsvBakeUtils.ParseIntOrWarn(Col("resultAmount"), recipeID, "resultAmount", i + 1, BakerName);
            recipeData.resultAmount = parsedAmount > 0 ? parsedAmount : 1;
            recipeData.craftTime = CsvBakeUtils.ParseFloatOrWarn(Col("craftTime"), recipeID, "craftTime", i + 1, BakerName);
            recipeData.ingredients = ingredients;

            EditorUtility.SetDirty(recipeData);
            bakedRecipes.Add(recipeData);
        }

        string dbPath = "Assets/Resources/RecipeDatabase.asset";
        RecipeDatabaseSO database = CsvBakeUtils.GetOrCreateAsset<RecipeDatabaseSO>(dbPath);
        database.allRecipes = bakedRecipes;
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>성공!</color> {bakedRecipes.Count}개의 레시피 데이터가 ScriptableObject로 구워졌습니다.");
    }

    // itemID로 이미 구워진 ItemData 에셋을 찾는다 (ItemDataBaker가 저장하는 경로 규칙을 그대로 재사용).
    // 그래서 레시피를 굽기 전에 아이템 데이터가 먼저 구워져 있어야 한다.
    private static ItemData LoadItemDataAsset(string itemID)
    {
        return AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/Resources/ItemDataAssets/{itemID}.asset");
    }
}
