using UnityEngine;
using UnityEditor; // 에디터 전용 네임스페이스
using System.Collections.Generic;

public class ItemDataBaker : EditorWindow
{
    // 유니티 상단 메뉴에 'Tools > 아이템 데이터 굽기(CSV -> SO)' 버튼을 생성합니다.
    [MenuItem("Tools/아이템 데이터 굽기(CSV -> SO)")]
    public static void BakeItemData()
    {
        // 1. CSV 파일 로드 (기존 ItemDataCsvLoader와 동일한 위치)
        TextAsset csvFile = Resources.Load<TextAsset>("itemDB");
        if (csvFile == null)
        {
            Debug.LogError("Resources 폴더에서 itemDB.csv 파일을 찾을 수 없습니다!");
            return;
        }

        // 2. 에셋을 저장할 폴더 확인 및 생성
        string folderPath = "Assets/Resources/ItemDataAssets";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            // 폴더가 없으면 새로 만듭니다.
            AssetDatabase.CreateFolder("Assets/Resources", "ItemDataAssets");
        }

        string[] lines = csvFile.text.Split('\n');
        List<ItemData> bakedItems = new List<ItemData>();

        // 3. CSV 파싱 및 ScriptableObject 생성/업데이트
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Split(',');
            if (cols.Length < 11) continue;

            string itemID = cols[0].Trim();
            string assetPath = $"{folderPath}/{itemID}.asset";

            // 이미 해당 ID의 에셋이 있는지 확인
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            if (itemData == null)
            {
                // 없으면 새로 생성
                itemData = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(itemData, assetPath);
            }

            // 데이터 덮어쓰기 (기존 파싱 로직 적용)
            itemData.itemID = itemID;
            System.Enum.TryParse(cols[1].Trim(), true, out itemData.itemType);
            itemData.displayName = cols[2].Trim();
            itemData.description = cols[3].Trim();
            itemData.canStack = cols[4].Trim().ToLower() == "true";
            int.TryParse(cols[5].Trim(), out itemData.maxStackAmount);
            itemData.type = cols[6].Trim();
            itemData.isConsumable = cols[7].Trim().ToLower() == "true";
            System.Enum.TryParse(cols[8].Trim(), true, out itemData.equipSlot);
            int.TryParse(cols[9].Trim(), out itemData.atk);
            int.TryParse(cols[10].Trim(), out itemData.def);

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
}