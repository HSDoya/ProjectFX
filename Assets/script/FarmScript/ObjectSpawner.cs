using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;

    [Header("배치 설정")]
    public float spawnCheckRadius = 0.5f;
    public float minDistanceBetweenObjects = 0.8f;
    public LayerMask obstacleLayer;
    public int maxPositionSearchTries = 50;

    [System.Serializable]
    public class StaticResourceRule
    {
        public string ruleName;
        public GameObject[] prefabs;
        public int spawnCount = 10;
    }

    public List<StaticResourceRule> resourceRules = new List<StaticResourceRule>();
    public List<GameObject> spawnedObjects = new List<GameObject>();

    private List<Vector3Int> currentChunkGroundTiles = new List<Vector3Int>();
    private List<Vector3> spawnedPositions = new List<Vector3>();

    public void SpawnObjectsInChunk(Vector2Int chunkCoord, int chunkSize)
    {
        CacheGroundTilesInChunk(chunkCoord, chunkSize);
        SpawnInCachedTiles();
    }

    private void CacheGroundTilesInChunk(Vector2Int chunkCoord, int chunkSize)
    {
        currentChunkGroundTiles.Clear();

        // MapGenerator와 동일하게 검색 시작 좌표를 절반만큼 뒤로 당깁니다. (1사분면 쏠림 해결)
        int startX = (chunkCoord.x * chunkSize) - (chunkSize / 2);
        int startY = (chunkCoord.y * chunkSize) - (chunkSize / 2);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int pos = new Vector3Int(startX + x, startY + y, 0);

                // 물이 없고 땅만 있는 타일 수집
                if (groundTilemap.HasTile(pos) && (waterTilemap == null || !waterTilemap.HasTile(pos)))
                {
                    currentChunkGroundTiles.Add(pos);
                }
            }
        }
    }

    private void SpawnInCachedTiles()
    {
        if (currentChunkGroundTiles.Count == 0) return;

        foreach (var rule in resourceRules)
        {
            for (int i = 0; i < rule.spawnCount; i++)
            {
                TrySpawn(rule);
            }
        }
    }

    private bool TrySpawn(StaticResourceRule rule)
    {
        for (int i = 0; i < maxPositionSearchTries; i++)
        {
            Vector3Int tilePos = currentChunkGroundTiles[Random.Range(0, currentChunkGroundTiles.Count)];
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(tilePos);
            worldPos.z = 0;

            Collider2D hit = Physics2D.OverlapCircle(worldPos, spawnCheckRadius, obstacleLayer);
            if (hit != null) continue;

            bool isTooClose = false;
            foreach (Vector3 pos in spawnedPositions)
            {
                if (Vector3.Distance(worldPos, pos) < minDistanceBetweenObjects)
                {
                    isTooClose = true;
                    break;
                }
            }
            if (isTooClose) continue;

            GameObject prefab = rule.prefabs[Random.Range(0, rule.prefabs.Length)];
            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, this.transform);

            spawnedObjects.Add(obj);
            spawnedPositions.Add(worldPos);
            return true;
        }
        return false;
    }
}