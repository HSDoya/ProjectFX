using System.Collections.Generic;
using UnityEngine;

public class ItemDataCsvLoader : MonoBehaviour
{
    public static ItemDataCsvLoader instance;
    public Dictionary<string, ItemData> itemDataDict = new();

    public string csvFileName = "itemDB";  // Resources/itemDB.csv

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadItemDataFromCSV();
    }

    void LoadItemDataFromCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>(csvFileName);
        if (csvFile == null)
        {
            Debug.LogError($"CSV 파일 {csvFileName}.csv을 찾을 수 없습니다!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');

        // 0번 줄은 헤더이므로 1부터 시작
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Split(',');
            if (cols.Length < 11)
            {
                Debug.LogWarning($"[{i}] 번째 줄의 컬럼 수가 11개보다 적습니다. (len={cols.Length})");
                continue;
            }

            // 공통 필드
            string itemID = cols[0].Trim();
            string itemTypeStr = cols[1].Trim();
            string name = cols[2].Trim();
            string desc = cols[3].Trim();
            string canStackStr = cols[4].Trim();
            string maxStackStr = cols[5].Trim();
            string typeStr = cols[6].Trim();
            string isConsumStr = cols[7].Trim();
            string equipSlotStr = cols[8].Trim();
            string atkStr = cols[9].Trim();
            string defStr = cols[10].Trim();

            // ItemData 생성
            ItemData item = new ItemData
            {
                itemID = itemID,
                displayName = name,
                description = desc,
                icon = Resources.Load<Sprite>($"icon/{itemID}")
            };

            // bool / int 파싱
            item.canStack = canStackStr.ToLower() == "true";
            if (!int.TryParse(maxStackStr, out item.maxStackAmount))
                item.maxStackAmount = 1;

            item.type = typeStr;
            item.isConsumable = isConsumStr.ToLower() == "true";

            if (!int.TryParse(atkStr, out item.atk))
                item.atk = 0;

            if (!int.TryParse(defStr, out item.def))
                item.def = 0;

            // enum 파싱 (실패하면 None)
            if (!System.Enum.TryParse<ItemType>(itemTypeStr, true, out item.itemType))
                item.itemType = ItemType.None;

            if (!System.Enum.TryParse<EquipmentSlotType>(equipSlotStr, true, out item.equipSlot))
                item.equipSlot = EquipmentSlotType.None;

            itemDataDict[item.itemID] = item;
        }

        Debug.Log($"CSV에서 {itemDataDict.Count}개의 아이템 로드 완료");
    }

    public ItemData GetItemDataByID(string itemID)
    {
        itemDataDict.TryGetValue(itemID, out var item);
        return item;
    }
}
