using UnityEngine;
using UnityEngine.Tilemaps;

// 작물 하나의 성장 데이터. 아이템(씨앗/수확물)은 복제하지 않고 ItemData를 그대로 참조한다.
[CreateAssetMenu(fileName = "NewCropData", menuName = "Farming/Crop Data")]
public class CropData : ScriptableObject
{
    public string cropID;

    [Header("연결 아이템")]
    public ItemData seedItemData;
    public ItemData harvestItemData;
    public int harvestYield = 1;

    [Header("성장 단계 타일 (심음 -> ... -> 수확 가능)")]
    public TileBase[] growStages;
    public float growTimePerStage = 5f;
}
