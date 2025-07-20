using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;

    public GameObject[] spawnPrefabs; // 돌, 나무 등
    public int spawnCount = 10;

    private List<Vector3Int> groundTiles = new List<Vector3Int>();

    private void Start()
    {
        CacheGroundTiles();
        SpawnObjectsOnGround();
    }

    private void CacheGroundTiles()
    {
        // Tilemap 범위를 기준으로 육지 타일만 저장
        BoundsInt bounds = groundTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (groundTilemap.HasTile(pos) && !waterTilemap.HasTile(pos))
                {
                    groundTiles.Add(pos);
                }
            }
        }
    }

    private void SpawnObjectsOnGround()
    {
        int count = 0;

        while (count < spawnCount && groundTiles.Count > 0)
        {
            int index = Random.Range(0, groundTiles.Count);
            Vector3Int tilePos = groundTiles[index];
            groundTiles.RemoveAt(index); // 중복 방지

            Vector3 worldPos = groundTilemap.CellToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0);
            GameObject prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
            Instantiate(prefab, worldPos, Quaternion.identity);
            count++;
        }
    }
}
