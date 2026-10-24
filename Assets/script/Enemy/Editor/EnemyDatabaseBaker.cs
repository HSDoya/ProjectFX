using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// EnemyDatabase.csv -> EnemyDataSO 들 + EnemyDatabaseSO 로 굽는다.
//
// 예전에는 컬럼을 위치로 읽었다(data[2], data[7] ...). 그래서 CSV 중간에 컬럼을 하나 끼우면
// 그 뒤의 모든 값이 조용히 한 칸씩 밀려서, 체력 자리에 공격력이 들어가는 식의 사고가 났다.
// 지금은 아이템/레시피 베이커와 똑같이 CsvBakeUtils로 헤더 이름을 보고 찾는다.
// (expReward 컬럼을 안전하게 추가할 수 있었던 것도 이 때문이다.)
//
// 파일 위치도 Editor 폴더로 옮겼다. 런타임 폴더에 있으면 #if UNITY_EDITOR로 감싸도
// 에디터 어셈블리의 CsvBakeUtils를 참조할 수 없다.
public class EnemyDatabaseBaker : EditorWindow
{
    private const string BakerName = "EnemyDatabaseBaker";

    // 이름으로 찾으므로 순서가 바뀌어도 되지만, 빠진 컬럼은 미리 알려준다.
    private static readonly string[] RequiredColumns =
    {
        "itemID", "enemyName", "maxHealth", "isAggressive", "detectRange",
        "attackRange", "moveSpeed", "attackDamage", "attackCooldown", "animType", "expReward"
    };

    [MenuItem("Tools/적 데이터 굽기(CSV -> SO)")]
    public static void BakeDatabase()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("EnemyDatabase");
        if (csvFile == null)
        {
            Debug.LogError($"[{BakerName}] Resources 폴더에서 EnemyDatabase.csv 를 찾을 수 없습니다!");
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

        const string enemyFolder = "Assets/Resources/Enemies";
        const string dbFolder = "Assets/Resources/Databases";
        if (!EditorUIBuilder.EnsureFolder(enemyFolder)) return;
        if (!EditorUIBuilder.EnsureFolder(dbFolder)) return;

        var baked = new List<EnemyDataSO>();
        var seenIds = new HashSet<string>();

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

            string id = Col("itemID");
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[{BakerName}] CSV {i + 1}번째 줄의 itemID가 비어 있어 건너뜁니다.");
                continue;
            }

            // 중복 id는 같은 .asset을 덮어써서 먼저 구운 데이터를 조용히 지운다.
            if (!seenIds.Add(id))
            {
                Debug.LogWarning($"[{BakerName}] 중복된 itemID '{id}' (CSV {i + 1}번째 줄)를 건너뜁니다. itemID는 고유해야 합니다.");
                continue;
            }

            string enemyName = Col("enemyName");
            string assetPath = $"{enemyFolder}/{id}_{enemyName}.asset";
            EnemyDataSO data = CsvBakeUtils.GetOrCreateAsset<EnemyDataSO>(assetPath);

            data.itemID = id;
            data.enemyName = enemyName;
            data.maxHealth = CsvBakeUtils.ParseIntOrWarn(Col("maxHealth"), id, "maxHealth", i + 1, BakerName);
            data.isAggressive = Col("isAggressive").ToLower() == "true";
            data.detectRange = CsvBakeUtils.ParseFloatOrWarn(Col("detectRange"), id, "detectRange", i + 1, BakerName);
            data.attackRange = CsvBakeUtils.ParseFloatOrWarn(Col("attackRange"), id, "attackRange", i + 1, BakerName);
            data.moveSpeed = CsvBakeUtils.ParseFloatOrWarn(Col("moveSpeed"), id, "moveSpeed", i + 1, BakerName);
            data.attackDamage = CsvBakeUtils.ParseFloatOrWarn(Col("attackDamage"), id, "attackDamage", i + 1, BakerName);
            data.attackCooldown = CsvBakeUtils.ParseFloatOrWarn(Col("attackCooldown"), id, "attackCooldown", i + 1, BakerName);
            data.animType = Col("animType");
            data.expReward = CsvBakeUtils.ParseIntOrWarn(Col("expReward"), id, "expReward", i + 1, BakerName);

            EditorUtility.SetDirty(data);
            baked.Add(data);
        }

        string dbPath = $"{dbFolder}/EnemyDatabase.asset";
        EnemyDatabaseSO database = CsvBakeUtils.GetOrCreateAsset<EnemyDatabaseSO>(dbPath);
        database.allEnemies = baked;
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>성공!</color> {baked.Count}개의 적 데이터가 ScriptableObject로 구워졌습니다.");
    }
}
