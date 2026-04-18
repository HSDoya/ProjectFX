using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;

    [Header("배치 설정")]
    public float spawnCheckRadius = 0.5f; // 겹침 감지 반경 (조금 키우는 것 추천)
    public float minDistanceBetweenObjects = 0.8f; // 오브젝트 간 최소 거리 유지
    public LayerMask obstacleLayer;
    public int maxPositionSearchTries = 50;

    [System.Serializable]
    public class StaticResourceRule
    {
        public string ruleName;
        public GameObject[] prefabs;
        public int spawnCount = 20;
    }

    public List<StaticResourceRule> resourceRules = new List<StaticResourceRule>();
    public List<GameObject> spawnedObjects = new List<GameObject>();
    private List<Vector3Int> groundTiles = new List<Vector3Int>();
    private List<Vector3> spawnedPositions = new List<Vector3>(); // 위치 기록용

    public void SpawnInitialObjects()
    {
        // 기존 데이터 초기화
        foreach (var obj in spawnedObjects) { if (obj != null) Destroy(obj); }
        spawnedObjects.Clear();
        spawnedPositions.Clear();

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
        if (groundTiles.Count == 0) return;

        foreach (var rule in resourceRules)
        {
            int successCount = 0;
            for (int i = 0; i < rule.spawnCount; i++)
            {
                if (TrySpawn(rule)) successCount++;
            }
            Debug.Log($"[ObjectSpawner] {rule.ruleName}: {successCount}/{rule.spawnCount} 배치 성공");
        }
    }

    private bool TrySpawn(StaticResourceRule rule)
    {
        for (int i = 0; i < maxPositionSearchTries; i++)
        {
            // 1. 랜덤 타일 선택 및 좌표 변환
            Vector3Int tilePos = groundTiles[Random.Range(0, groundTiles.Count)];
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(tilePos);
            worldPos.z = 0;

            // 2. 물리적 겹침 체크 (Physics2D)
            Collider2D hit = Physics2D.OverlapCircle(worldPos, spawnCheckRadius, obstacleLayer);
            if (hit != null) continue;

            // 3. 코드 레벨 거리 체크 (기록된 위치들과 비교)
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

            // 4. 모든 조건 통과 시 생성
            GameObject prefab = rule.prefabs[Random.Range(0, rule.prefabs.Length)];
            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, this.transform);

            spawnedObjects.Add(obj);
            spawnedPositions.Add(worldPos); // 위치 기록
            return true;
        }
        return false;
    }

    public void RemoveObject(GameObject obj)
    {
        if (spawnedObjects.Contains(obj))
        {
            spawnedPositions.Remove(obj.transform.position);
            spawnedObjects.Remove(obj);
        }
        Destroy(obj);
    }
}