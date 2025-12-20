using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;
    public float spawnCheckRadius = 0.4f;
    public LayerMask obstacleLayer;
    public int maxPositionSearchTries = 50;

    [System.Serializable]
    public class StaticResourceRule
    {
        public string ruleName;
        public GameObject[] prefabs;
        public int spawnCount = 20; // 시작 시 생성할 개수
    }

    public List<StaticResourceRule> resourceRules = new List<StaticResourceRule>();
    public List<GameObject> spawnedObjects = new List<GameObject>(); // PlayerMove에서 참조용
    private List<Vector3Int> groundTiles = new List<Vector3Int>();

    private void Start()
    {
        CacheGroundTiles();
        InitialSpawnAll();
    }

    private void CacheGroundTiles()
    {
        groundTiles.Clear();
        BoundsInt bounds = groundTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (groundTilemap.HasTile(pos) && (waterTilemap == null || !waterTilemap.HasTile(pos)))
            {
                groundTiles.Add(pos);
            }
        }
    }

    private void InitialSpawnAll()
    {
        foreach (var rule in resourceRules)
        {
            int successCount = 0;
            for (int i = 0; i < rule.spawnCount; i++)
            {
                if (TrySpawn(rule)) successCount++;
            }
            Debug.Log($"[ObjectSpawner] {rule.ruleName} {successCount}개 배치 완료.");
        }
    }

    private bool TrySpawn(StaticResourceRule rule)
    {
        for (int i = 0; i < maxPositionSearchTries; i++)
        {
            Vector3Int tilePos = groundTiles[Random.Range(0, groundTiles.Count)];
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(tilePos);
            worldPos.z = 0;

            if (Physics2D.OverlapCircle(worldPos, spawnCheckRadius, obstacleLayer) == null)
            {
                GameObject prefab = rule.prefabs[Random.Range(0, rule.prefabs.Length)];
                GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);
                spawnedObjects.Add(obj);
                return true;
            }
        }
        return false;
    }

    // PlayerMove 등에서 오브젝트를 파괴할 때 호출할 함수
    public void RemoveObject(GameObject obj)
    {
        if (spawnedObjects.Contains(obj)) spawnedObjects.Remove(obj);
        Destroy(obj);
    }
}