using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    //Tilemaps
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;

    //공통 설정
    //이 반경 안에 다른 콜라이더/오브젝트가 있으면 스폰하지 않음
    public float spawnCheckRadius = 0.3f;

    //한 번에 스폰 위치를 찾기 위해 시도하는 최대 횟수
    public int maxPositionSearchTries = 20;

    [System.Serializable]
    public class SpawnRule
    {
        //에디터에서 보기 위한 이름 (나무, 돌, 닭 등)
        public string ruleName;

        //이 룰로 스폰될 프리팹들 (랜덤 선택)
        public GameObject[] prefabs;

        //게임 시작 시 최초로 뿌릴 개수
        public int initialCount = 10;

        //맵 전체에서 이 종류가 존재할 수 있는 최대 개수
        public int maxCount = 30;

        //스폰 시도 간격 최소/최대 (초) - 림월드처럼 랜덤 간격
        public Vector2 spawnIntervalRange = new Vector2(20f, 40f);

        [HideInInspector] public float nextSpawnTime;
        [HideInInspector] public List<GameObject> livingInstances = new List<GameObject>();
    }

    //생태계 스폰 규칙들
    public List<SpawnRule> spawnRules = new List<SpawnRule>();

    //디버그/호환용
    //외부 스크립트(PlayerMove 등)에서 접근할 수 있도록 전체 스폰된 오브젝트 리스트
    public List<GameObject> spawnedObjects = new List<GameObject>();

    // 내부 캐시
    private List<Vector3Int> groundTiles = new List<Vector3Int>();

    // 생명주기
    private void Start()
    {
        CacheGroundTiles();
        InitSpawnRules();
        InitialSpawnAll();
    }

    private void Update()
    {
        UpdateSpawnOverTime();
        CleanupDestroyedInstances();
    }

    // 타일 캐싱
    private void CacheGroundTiles()
    {
        groundTiles.Clear();

        if (groundTilemap == null)
        {
            Debug.LogError("[ObjectSpawner] groundTilemap이 설정되지 않았습니다.");
            return;
        }

        BoundsInt bounds = groundTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (groundTilemap.HasTile(pos))
                {
                    // 물 타일이 없을 때만
                    if (waterTilemap == null || !waterTilemap.HasTile(pos))
                    {
                        groundTiles.Add(pos);
                    }
                }
            }
        }

        if (groundTiles.Count == 0)
        {
            Debug.LogWarning("[ObjectSpawner] 유효한 groundTiles가 없습니다. 타일맵 설정을 확인하세요.");
        }
    }

    // 룰 초기화
    private void InitSpawnRules()
    {
        float now = Time.time;

        foreach (var rule in spawnRules)
        {
            // 최소/최대 보정
            if (rule.spawnIntervalRange.y < rule.spawnIntervalRange.x)
                rule.spawnIntervalRange.y = rule.spawnIntervalRange.x;

            // 처음 스폰 시간 예약(조금 랜덤하게)
            float firstDelay = Random.Range(
                rule.spawnIntervalRange.x * 0.5f,
                rule.spawnIntervalRange.y * 0.5f
            );
            rule.nextSpawnTime = now + firstDelay;

            if (rule.livingInstances == null)
                rule.livingInstances = new List<GameObject>();
        }
    }

    // 최초 스폰
    private void InitialSpawnAll()
    {
        foreach (var rule in spawnRules)
        {
            for (int i = 0; i < rule.initialCount; i++)
            {
                TrySpawnForRule(rule);
            }
        }
    }

    // 시간에 따라 자연스럽게 증가 (림월드 느낌)
    private void UpdateSpawnOverTime()
    {
        float now = Time.time;

        foreach (var rule in spawnRules)
        {
            // 프리팹이 없으면 스킵
            if (rule.prefabs == null || rule.prefabs.Length == 0)
                continue;

            // 현재 살아있는 개수 카운트(Null 제거는 Cleanup에서)
            int livingCount = 0;
            foreach (var obj in rule.livingInstances)
            {
                if (obj != null) livingCount++;
            }

            // 최대 개수에 도달했다면 스폰 중단
            if (livingCount >= rule.maxCount)
                continue;

            // 아직 스폰 타이밍이 아님
            if (now < rule.nextSpawnTime)
                continue;

            // 스폰 시도
            bool spawned = TrySpawnForRule(rule);

            // 다음 스폰 예약
            float interval = Random.Range(rule.spawnIntervalRange.x, rule.spawnIntervalRange.y);
            rule.nextSpawnTime = now + interval;

            if (spawned)
            {
                // 디버그용
                // Debug.Log($"[ObjectSpawner] Rule {rule.ruleName} 새 개체 스폰 완료.");
            }
        }
    }

    // ─────────────────────────────────────────
    // 룰 단위 스폰 시도
    // ─────────────────────────────────────────
    private bool TrySpawnForRule(SpawnRule rule)
    {
        if (groundTiles.Count == 0) return false;
        if (rule.prefabs == null || rule.prefabs.Length == 0) return false;

        // 사용 가능한 위치를 여러 번 시도해서 찾음
        for (int i = 0; i < maxPositionSearchTries; i++)
        {
            // 랜덤 타일 선택
            Vector3Int tilePos = groundTiles[Random.Range(0, groundTiles.Count)];
            Vector3 worldPos = groundTilemap.CellToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0f);

            // 주변에 다른 오브젝트가 있으면 스킵(겹치지 않게)
            if (Physics2D.OverlapCircle(worldPos, spawnCheckRadius) != null)
                continue;

            // 랜덤 프리팹 선택 후 스폰
            GameObject prefab = rule.prefabs[Random.Range(0, rule.prefabs.Length)];
            if (prefab == null) continue;

            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);

            // 각 룰별, 전체 리스트에 모두 기록
            rule.livingInstances.Add(obj);
            spawnedObjects.Add(obj);

            return true;
        }

        // 유효 위치 못 찾음
        return false;
    }

    // 죽은/파괴된 개체 정리
    private void CleanupDestroyedInstances()
    {
        // 룰별 정리
        foreach (var rule in spawnRules)
        {
            if (rule.livingInstances == null) continue;

            for (int i = rule.livingInstances.Count - 1; i >= 0; i--)
            {
                if (rule.livingInstances[i] == null)
                    rule.livingInstances.RemoveAt(i);
            }
        }

        // 전체 리스트 정리
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] == null)
                spawnedObjects.RemoveAt(i);
        }
    }
}
