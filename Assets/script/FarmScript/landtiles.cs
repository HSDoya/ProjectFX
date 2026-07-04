using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class landtiles : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap farmTilemap;

    [Header("Tiles")]
    public TileBase farmableTile;
    public TileBase seedTile;
    public TileBase sproutTile;
    public TileBase grownTile;
    public TileBase harvestableTile;

    public void PlowSoil(Vector3Int tilePosition)
    {
        // 1. 해당 타일에 이미 농작물이 있거나 이미 갈아진 밭인지 확인 (방어 로직)
        TileBase currentTile = farmTilemap.GetTile(tilePosition);
        if (currentTile == farmableTile || currentTile == seedTile || currentTile == sproutTile || currentTile == grownTile || currentTile == harvestableTile)
        {
            Debug.Log("이미 농사를 짓고 있는 땅이거나 갈아진 땅입니다.");
            return;
        }

        // 2. 땅 갈기 (RefreshAllTiles는 불필요하여 제거)
        farmTilemap.SetTile(tilePosition, farmableTile);
        Debug.Log("땅을 갈았습니다.");
    }

    public void PlantSeed(Vector3Int tilePosition)
    {
        if (farmTilemap.GetTile(tilePosition) == farmableTile)
        {
            farmTilemap.SetTile(tilePosition, seedTile);
            Debug.Log("씨앗을 심었습니다.");
        }
    }

    public void WaterTile(Vector3Int tilePosition)
    {
        if (farmTilemap.GetTile(tilePosition) == seedTile)
        {
            farmTilemap.SetTile(tilePosition, sproutTile);
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
        // 1단계 성장
        yield return new WaitForSeconds(5f);

        // 성장 도중에 플레이어가 밭을 파괴했는지 안전 검사 (선택 사항)
        if (farmTilemap.GetTile(tilePosition) != sproutTile) yield break;

        farmTilemap.SetTile(tilePosition, grownTile);
        Debug.Log("작물이 자랐습니다!");

        // 2단계 성장 (수확 가능 상태)
        yield return new WaitForSeconds(5f);

        if (farmTilemap.GetTile(tilePosition) != grownTile) yield break;

        farmTilemap.SetTile(tilePosition, harvestableTile);
        Debug.Log("작물이 완전히 자랐습니다! 수확이 가능합니다.");
    }

    public void HarvestCrop(Vector3Int tilePosition)
    {
        if (farmTilemap.GetTile(tilePosition) == harvestableTile)
        {
            // 수확 후 다시 빈 밭으로 되돌림
            farmTilemap.SetTile(tilePosition, farmableTile);
            Debug.Log("작물을 수확했습니다!");

            // [!] 나중에 이 부분에 인벤토리에 수확물을 추가하는 코드를 넣어야 합니다.
            // 예: Inventory.instance.AddItem(수확물아이템데이터);
        }
    }
}