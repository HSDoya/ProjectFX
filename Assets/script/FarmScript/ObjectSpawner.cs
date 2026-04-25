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
        public int spawnCount = 5; // 💡 주의: 이제 맵 전체가 아니라 '1개 구역(청크) 당' 생성 개수입니다!
    }

    public List<StaticResourceRule> resourceRules = new List<StaticResourceRule>();
    public List<GameObject> spawnedObjects = new List<GameObject>();

    private List<Vector3Int> currentChunkGroundTiles = new List<Vector3Int>(); // 현재 청크의 땅 타일 목록
    private List<Vector3> spawnedPositions = new List<Vector3>();

    // 🌟 맵 전체가 아니라 '특정 구역(청크)'에만 오브젝트를 스폰하는 함수로 변경되었습니다.
    public void SpawnObjectsInChunk(Vector2Int chunkCoord, int chunkSize)
    {
        // 🚨 주의: 기존 오브젝트를 파괴하는 Clear() 로직을 삭제했습니다! (기존 땅의 자원 유지)

        CacheGroundTilesInChunk(chunkCoord, chunkSize);
        SpawnInCachedTiles();
    }

    // 딱 해당 구역(chunkCoord) 안의 땅 타일만 찾아내는 함수
    private void CacheGroundTilesInChunk(Vector2Int chunkCoord, int chunkSize)
    {
        currentChunkGroundTiles.Clear();

        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int pos = new Vector3Int(startX + x, startY + y, 0);

                // 물 타일이 없고 땅 타일만 있는 곳 추출
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
            int successCount = 0;
            for (int i = 0; i < rule.spawnCount; i++)
            {
                if (TrySpawn(rule)) successCount++;
            }
            Debug.Log($"[ObjectSpawner] {rule.ruleName}: 새 구역에 {successCount}/{rule.spawnCount} 배치 성공");
        }
    }

    private bool TrySpawn(StaticResourceRule rule)
    {
        for (int i = 0; i < maxPositionSearchTries; i++)
        {
            // 1. 현재 청크의 땅 중에서만 랜덤 타일 선택
            Vector3Int tilePos = currentChunkGroundTiles[Random.Range(0, currentChunkGroundTiles.Count)];
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(tilePos);
            worldPos.z = 0;

            // 2. 물리적 겹침 체크
            Collider2D hit = Physics2D.OverlapCircle(worldPos, spawnCheckRadius, obstacleLayer);
            if (hit != null) continue;

            // 3. 거리 체크 (다른 청크에서 스폰된 오브젝트와의 거리도 고려됨)
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

            // 4. 생성
            GameObject prefab = rule.prefabs[Random.Range(0, rule.prefabs.Length)];
            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, this.transform);

            spawnedObjects.Add(obj);
            spawnedPositions.Add(worldPos);
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