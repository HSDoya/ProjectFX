using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CropDatabase", menuName = "Farming/Crop Database")]
public class CropDatabaseSO : ScriptableObject
{
    public List<CropData> allCrops = new List<CropData>();

    private Dictionary<string, CropData> cropBySeedID = new Dictionary<string, CropData>();

    public void Initialize()
    {
        cropBySeedID.Clear();
        foreach (var crop in allCrops)
        {
            if (crop != null && crop.seedItemData != null && !cropBySeedID.ContainsKey(crop.seedItemData.itemID))
            {
                cropBySeedID.Add(crop.seedItemData.itemID, crop);
            }
        }
    }

    public CropData GetCropBySeedID(string seedItemID)
    {
        if (cropBySeedID.Count == 0) Initialize();

        cropBySeedID.TryGetValue(seedItemID, out var crop);
        return crop;
    }
}
