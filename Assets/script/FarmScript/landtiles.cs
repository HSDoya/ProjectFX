using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class landtiles : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap farmTilemap; // 지형/갈아진 땅 (MapGenerator와 공유하는 레이어)
    public Tilemap cropTilemap; // 씨앗/성장 단계 전용 오버레이 레이어 (farmTilemap 위에 겹쳐 그려짐)

    [Header("Tiles")]
    public TileBase farmableTile; // 갈아둔 빈 땅 (작물 공통, 심기 전 상태)

    [Header("Database")]
    public CropDatabaseSO cropDatabase;

    // 타일 좌표별 작물 상태. 이 딕셔너리가 곧 밭의 상태 자체이며, farmTilemap의 TileBase는
    // 여기 저장된 상태를 화면에 보여주기 위한 결과물일 뿐이다(타일 종류로 상태를 역추론하지 않는다).
    private class FarmTileState
    {
        public CropData crop;   // null이면 갈아둔 빈 땅(아직 안 심음)
        public int stageIndex;  // crop.growStages의 인덱스
    }

    private readonly Dictionary<Vector3Int, FarmTileState> farmTiles = new Dictionary<Vector3Int, FarmTileState>();

    // 마우스 하이라이트 등 외부에서 "이 칸이 밭인지"만 알고 싶을 때 쓰는 조회용 메서드.
    public bool HasFarmTile(Vector3Int tilePosition) => farmTiles.ContainsKey(tilePosition);

    public void PlowSoil(Vector3Int tilePosition)
    {
        // 이미 갈았거나(빈 땅) 작물이 심겨 있으면 무시
        if (farmTiles.ContainsKey(tilePosition))
        {
            Debug.Log("이미 농사를 짓고 있는 땅이거나 갈아진 땅입니다.");
            return;
        }

        farmTiles[tilePosition] = new FarmTileState { crop = null, stageIndex = 0 };
        farmTilemap.SetTile(tilePosition, farmableTile);
        Debug.Log("땅을 갈았습니다.");
    }

    // seedItem으로 어떤 작물을 심을지 CropDatabase에서 조회한다. 심기에 성공하면 true를 반환하며,
    // 호출 측(PlayerMove)은 이때만 인벤토리에서 씨앗을 소모해야 한다.
    public bool PlantSeed(Vector3Int tilePosition, ItemData seedItem)
    {
        if (seedItem == null) return false;
        if (!farmTiles.TryGetValue(tilePosition, out var state) || state.crop != null)
        {
            Debug.Log("갈아진 빈 땅에만 씨앗을 심을 수 있습니다.");
            return false;
        }
        if (cropDatabase == null) return false;

        CropData crop = cropDatabase.GetCropBySeedID(seedItem.itemID);
        if (crop == null || crop.growStages == null || crop.growStages.Length == 0)
        {
            Debug.LogWarning($"[landtiles] '{seedItem.itemID}'에 대응하는 CropData가 없습니다.");
            return false;
        }

        state.crop = crop;
        state.stageIndex = 0;
        cropTilemap.SetTile(tilePosition, crop.growStages[0]);
        Debug.Log($"{crop.cropID} 씨앗을 심었습니다.");
        return true;
    }

    public void WaterTile(Vector3Int tilePosition)
    {
        // 심은 직후(0단계)에만 물을 줄 수 있고, 물을 주면 자동 성장이 시작된다.
        if (farmTiles.TryGetValue(tilePosition, out var state) && state.crop != null && state.stageIndex == 0)
        {
            Debug.Log("물을 주었습니다! 작물이 자라기 시작합니다.");
            StartCoroutine(GrowCrop(tilePosition));
        }
        else
        {
            Debug.Log("여기엔 물을 줄 수 없거나 심어진 씨앗이 없습니다.");
        }
    }

    private IEnumerator GrowCrop(Vector3Int tilePosition)
    {
        while (farmTiles.TryGetValue(tilePosition, out var state) && state.crop != null
               && state.stageIndex < state.crop.growStages.Length - 1)
        {
            yield return new WaitForSeconds(state.crop.growTimePerStage);

            // 성장 도중에 플레이어가 밭을 갈아엎었거나 다른 작물로 바뀌었는지 안전 검사
            if (!farmTiles.TryGetValue(tilePosition, out state) || state.crop == null) yield break;

            state.stageIndex++;
            cropTilemap.SetTile(tilePosition, state.crop.growStages[state.stageIndex]);

            if (state.stageIndex == state.crop.growStages.Length - 1)
                Debug.Log($"{state.crop.cropID}이(가) 완전히 자랐습니다! 수확이 가능합니다.");
        }
    }

    public bool IsHarvestable(Vector3Int tilePosition)
    {
        return farmTiles.TryGetValue(tilePosition, out var state)
            && state.crop != null
            && state.stageIndex == state.crop.growStages.Length - 1;
    }

    public void HarvestCrop(Vector3Int tilePosition)
    {
        if (!IsHarvestable(tilePosition)) return;

        var state = farmTiles[tilePosition];
        CropData crop = state.crop;

        if (Inventory.instance != null)
        {
            Inventory.instance.AddItem(new Item(crop.harvestItemData, crop.harvestYield));
        }
        Debug.Log($"{crop.cropID}을(를) 수확했습니다!");

        // 수확 후 작물 그림만 지움 (밑의 갈아진 땅은 farmTilemap에 그대로 남아있음)
        state.crop = null;
        state.stageIndex = 0;
        cropTilemap.SetTile(tilePosition, null);
    }
}
