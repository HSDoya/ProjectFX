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
            Debug.LogError($"CSV ���� {csvFileName}.csv�� ã�� �� �����ϴ�!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Split(',');

            ItemData item = new ItemData
            {
                itemID = cols[0].Trim(),
                displayName = cols[1].Trim(),
                description = cols[2].Trim(),
                canStack = cols[3].Trim().ToLower() == "true",
                maxStackAmount = int.Parse(cols[4].Trim()),
                icon = Resources.Load<Sprite>($"icon/{cols[0].Trim()}")
            };

            itemDataDict[item.itemID] = item;
        }

        Debug.Log($"CSV���� {itemDataDict.Count}���� ������ �ε� �Ϸ�");
    }

    public ItemData GetItemDataByID(string itemID)
    {
        itemDataDict.TryGetValue(itemID, out var item);
        return item;
    }
}
