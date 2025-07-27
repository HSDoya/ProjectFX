using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;

    public GameObject[] spawnPrefabs;
    public int spawnCount = 10;

    public List<GameObject> spawnedObjects = new List<GameObject>(); // 🔥 추가

    private List<Vector3Int> groundTiles = new List<Vector3Int>();

    private void Start()
    {
        CacheGroundTiles();
        SpawnObjectsOnGround();
    }

    private void CacheGroundTiles()
    {
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
            groundTiles.RemoveAt(index);

            Vector3 worldPos = groundTilemap.CellToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0);
            GameObject prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);

            spawnedObjects.Add(obj); // 🔥 생성된 오브젝트 저장
            count++;
        }
    }
}
