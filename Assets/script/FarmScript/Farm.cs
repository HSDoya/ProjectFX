using UnityEngine;
using UnityEngine.Tilemaps;

public class Farm : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase farmableTile;
    public TileBase wetSoilTile;
    public TileBase seedTile;
    public TileBase sproutTile;
    public TileBase grownPlantTile;
    public TileBase harvestableTile;

    public void PlowSoil(Vector3 worldPosition)
    {
        Vector3Int tilePosition = tilemap.WorldToCell(worldPosition);
        tilemap.SetTile(tilePosition, farmableTile);
    }

    public void WaterSoil(Vector3 worldPosition)
    {
        Vector3Int tilePosition = tilemap.WorldToCell(worldPosition);
        if (tilemap.GetTile(tilePosition) == farmableTile)
        {
            tilemap.SetTile(tilePosition, wetSoilTile);
        }
    }

    public void PlantSeed(Vector3 worldPosition)
    {
        Vector3Int tilePosition = tilemap.WorldToCell(worldPosition);
        if (tilemap.GetTile(tilePosition) == wetSoilTile)
        {
            tilemap.SetTile(tilePosition, seedTile);
        }
    }

    public void HarvestPlant(Vector3 worldPosition)
    {
        Vector3Int tilePosition = tilemap.WorldToCell(worldPosition);
        if (tilemap.GetTile(tilePosition) == harvestableTile)
        {
            tilemap.SetTile(tilePosition, farmableTile);
        }
    }
}
