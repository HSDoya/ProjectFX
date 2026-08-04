using UnityEngine;
using UnityEditor; // 에디터 전용 네임스페이스
using System.Collections.Generic;

public class ItemDataBaker : EditorWindow
{
    // CSV에 반드시 있어야 하는 컬럼들. 순서가 바뀌거나 새 컬럼이 끼어들어도
    // 이름으로 찾기 때문에 안전하다.
    private static readonly string[] RequiredColumns =
    {
        "itemID", "ItemType", "Name", "Description", "canStack",
        "MaxStack", "type", "isConsumable", "equipSlot", "atk", "def"
    };

    // 유니티 상단 메뉴에 'Tools > 아이템 데이터 굽기(CSV -> SO)' 버튼을 생성합니다.
    [MenuItem("Tools/아이템 데이터 굽기(CSV -> SO)")]
    public static void BakeItemData()
    {
        // 1. CSV 파일 로드 (기존 ItemDataCsvLoader와 동일한 위치)
        TextAsset csvFile = Resources.Load<TextAsset>("ItemDatabase");
        if (csvFile == null)
        {
            Debug.LogError("Resources 폴더에서 ItemDatabase.csv 파일을 찾을 수 없습니다!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        if (lines.Length == 0)
        {
            Debug.LogError("[ItemDataBaker] CSV 파일이 비어 있습니다.");
            return;
        }

        // 헤더 줄을 컬럼 이름 -> 인덱스로 매핑 (위치가 아니라 이름으로 값을 찾기 위함)
        string[] header = lines[0].Split(',');
        var colIndex = new Dictionary<string, int>();
        for (int c = 0; c < header.Length; c++)
        {
            colIndex[header[c].Trim()] = c;
        }

        foreach (var required in RequiredColumns)
        {
            if (!colIndex.ContainsKey(required))
            {
                Debug.LogError($"[ItemDataBaker] CSV 헤더에 '{required}' 컬럼이 없습니다. 굽기를 중단합니다.");
                return;
            }
        }

        // 2. 에셋을 저장할 폴더 확인 및 생성
        string folderPath = "Assets/Resources/ItemDataAssets";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            // 폴더가 없으면 새로 만듭니다.
            AssetDatabase.CreateFolder("Assets/Resources", "ItemDataAssets");
        }

        List<ItemData> bakedItems = new List<ItemData>();
        HashSet<string> seenIds = new HashSet<string>();

        // 3. CSV 파싱 및 ScriptableObject 생성/업데이트
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Split(',');
            if (cols.Length < header.Length)
            {
                Debug.LogWarning($"[ItemDataBaker] CSV {i + 1}번째 줄의 컬럼 수가 부족합니다 (필요 {header.Length}개, 실제 {cols.Length}개). 이 줄을 건너뜁니다.");
                continue;
            }

            string Col(string name) => cols[colIndex[name]].Trim();

            string itemID = Col("itemID");
            if (string.IsNullOrEmpty(itemID))
            {
                Debug.LogWarning($"[ItemDataBaker] CSV {i + 1}번째 줄의 itemID가 비어 있어 건너뜁니다.");
                continue;
            }

            // 중복 itemID는 같은 .asset을 덮어써서 먼저 구운 아이템 데이터를 조용히 지워버리므로 건너뛴다.
            if (!seenIds.Add(itemID))
            {
                Debug.LogWarning($"[ItemDataBaker] 중복된 itemID '{itemID}' (CSV {i + 1}번째 줄)를 건너뜁니다. itemID는 고유해야 합니다.");
                continue;
            }

            string assetPath = $"{folderPath}/{itemID}.asset";

            // 이미 해당 ID의 에셋이 있는지 확인
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            if (itemData == null)
            {
                // 없으면 새로 생성
                itemData = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(itemData, assetPath);
            }

            // 데이터 덮어쓰기
            itemData.itemID = itemID;
            itemData.itemType = ParseEnumOrWarn<ItemType>(Col("ItemType"), itemID, "ItemType", i + 1);
            itemData.displayName = Col("Name");
            itemData.description = Col("Description");
            itemData.canStack = Col("canStack").ToLower() == "true";
            itemData.maxStackAmount = ParseIntOrWarn(Col("MaxStack"), itemID, "MaxStack", i + 1);
            itemData.type = Col("type");
            itemData.isConsumable = Col("isConsumable").ToLower() == "true";
            itemData.equipSlot = ParseEnumOrWarn<EquipmentSlotType>(Col("equipSlot"), itemID, "equipSlot", i + 1);
            itemData.atk = ParseIntOrWarn(Col("atk"), itemID, "atk", i + 1);
            itemData.def = ParseIntOrWarn(Col("def"), itemID, "def", i + 1);

            // 아이콘 연결
            itemData.icon = Resources.Load<Sprite>($"icon/{itemID}");

            // 변경사항이 있다고 에디터에 알림
            EditorUtility.SetDirty(itemData);
            bakedItems.Add(itemData);
        }

        // 4. ItemDatabaseSO(통합 DB) 업데이트
        string dbPath = "Assets/Resources/ItemDatabase.asset";
        ItemDatabaseSO database = AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(dbPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ItemDatabaseSO>();
            AssetDatabase.CreateAsset(database, dbPath);
        }

        // 구워진 아이템 리스트를 DB에 갱신
        database.allItems = bakedItems;
        EditorUtility.SetDirty(database);

        // 5. 실제 파일로 저장하고 프로젝트 창 새로고침
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>성공!</color> {bakedItems.Count}개의 아이템 데이터가 ScriptableObject로 구워졌습니다.");
    }

    // 값이 비어 있으면 조용히 0, 값이 있는데 숫자가 아니면 경고 후 0
    private static int ParseIntOrWarn(string raw, string itemID, string columnName, int lineNumber)
    {
        if (string.IsNullOrEmpty(raw)) return 0;
        if (int.TryParse(raw, out int value)) return value;

        Debug.LogWarning($"[ItemDataBaker] '{itemID}'의 {columnName} 값 '{raw}'이(가) 숫자가 아니어서 0으로 처리됩니다. (CSV {lineNumber}번째 줄)");
        return 0;
    }

    // 값이 비어 있으면 조용히 기본값, 값이 있는데 enum 이름과 안 맞으면 경고 후 기본값
    private static T ParseEnumOrWarn<T>(string raw, string itemID, string columnName, int lineNumber) where T : struct, System.Enum
    {
        if (string.IsNullOrEmpty(raw)) return default;
        if (System.Enum.TryParse<T>(raw, true, out T value)) return value;

        Debug.LogWarning($"[ItemDataBaker] '{itemID}'의 {columnName} 값 '{raw}'을(를) 알 수 없어 {default(T)}(으)로 처리됩니다. (CSV {lineNumber}번째 줄)");
        return default;
    }
}
