using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class landtiles : MonoBehaviour
{
    public Tilemap farmTilemap;
    public TileBase farmableTile;
    public TileBase seedTile;
    public TileBase sproutTile;
    public TileBase grownTile;
    public TileBase harvestableTile;

    public void PlowSoil(Vector3Int tilePosition)
    {
        farmTilemap.SetTile(tilePosition, farmableTile);
        farmTilemap.RefreshAllTiles();
        Debug.Log("땅을 갈았습니다.");
    }



    public void PlantSeed(Vector3Int tilePosition)
    {
        if (farmTilemap.GetTile(tilePosition) == farmableTile)
        {
            farmTilemap.SetTile(tilePosition, seedTile);
            farmTilemap.RefreshAllTiles();
            Debug.Log("씨앗을 심었습니다.");
        }
    }

    public void WaterTile(Vector3Int tilePosition)
    {
        if (farmTilemap.GetTile(tilePosition) == seedTile)
        {
            farmTilemap.SetTile(tilePosition, sproutTile);
            farmTilemap.RefreshAllTiles();
            Debug.Log("물을 주었습니다!");
            StartCoroutine(GrowCrop(tilePosition));
        }
    }

    private IEnumerator GrowCrop(Vector3Int tilePosition)
    {
        yield return new WaitForSeconds(5f);
        farmTilemap.SetTile(tilePosition, grownTile);
        farmTilemap.RefreshAllTiles();
        Debug.Log("작물이 자랐습니다!");

        yield return new WaitForSeconds(5f);
        farmTilemap.SetTile(tilePosition, harvestableTile);
        farmTilemap.RefreshAllTiles();
        Debug.Log("작물이 완전히 자랐습니다!");
    }

    public void HarvestCrop(Vector3Int tilePosition)
    {
        if (farmTilemap.GetTile(tilePosition) == harvestableTile)
        {
            farmTilemap.SetTile(tilePosition, farmableTile);
            farmTilemap.RefreshAllTiles();
            Debug.Log("작물을 수확했습니다!");
        }
    }
}
