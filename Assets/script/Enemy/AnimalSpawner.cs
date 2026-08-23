using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [Tooltip("스폰이 허용되는 타일맵 목록 (예: Ground, Grass, Dirt)")]
    public List<Tilemap> walkableTilemaps = new List<Tilemap>();
    [Tooltip("스폰이 금지되는 타일맵 목록 (예: Water, Obstacles, Wall)")]
    public List<Tilemap> blockedTilemaps = new List<Tilemap>();

    [Header("스폰 대상 레이어 및 중복 방지")]
    public LayerMask livingEntityLayer; // 적/동물 충돌체 레이어
    public float spawnCheckRadius = 0.5f;

    [Header("카메라 밖 및 플레이어 거리 설정")]
    public Transform playerTransform;
    public float minDistanceFromPlayer = 12f;
    [Tooltip("카메라 시야 외곽 여유 마진 (0.1이면 화면 경계 밖 10% 영역까지 제외)")]
    public float viewportMargin = 0.15f;

    [System.Serializable]
    public class SpawnRule
    {
        public string entityName;      // 기획용 명칭 (예: 슬라임, 닭, 소)
        public GameObject prefab;       // EnemyBaseAI가 부착된 프리팹
        public int targetCount = 5;     // 필드에 유지할 최대 마리 수
    }

    [Header("스폰 규칙 목록")]
    public List<SpawnRule> spawnRules = new List<SpawnRule>();

    [Header("스폰 주기 및 배치 설정")]
    public float minCheckInterval = 2f;
    public float maxCheckInterval = 5f;
    [Tooltip("한 번의 주기마다 최대 몇 마리까지 생성할 것인가")]
    public int maxSpawnPerBatch = 1;
    public bool spawnImmediatelyOnStart = false;

    private List<Vector3Int> cachedWalkableCells = new List<Vector3Int>();
    private List<GameObject> activeEntities = new List<GameObject>();
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        CacheWalkableCells();
        StartCoroutine(PopulationControlRoutine());
    }

    IEnumerator PopulationControlRoutine()
    {
        if (!spawnImmediatelyOnStart)
            yield return new WaitForSeconds(minCheckInterval);

        while (true)
        {
            // 파괴되었거나 죽은 오브젝트(null) 정리
            activeEntities.RemoveAll(e => e == null);

            int spawnedInThisBatch = 0;

            foreach (var rule in spawnRules)
            {
                if (rule.prefab == null) continue;

                // 현재 필드에 살아있는 해당 프리팹 종류 카운팅
                int currentCount = 0;
                foreach (var e in activeEntities)
                {
                    if (e != null && e.name.StartsWith(rule.prefab.name))
                        currentCount++;
                }

                // 목표 수량보다 적을 경우 스폰 시도
                while (currentCount < rule.targetCount && spawnedInThisBatch < maxSpawnPerBatch)
                {
                    if (TrySpawnOneEntity(rule))
                    {
                        currentCount++;
                        spawnedInThisBatch++;
                    }
                    else
                    {
                        // 50회 시도 내에 적합한 좌표를 못 찾으면 다음 주기로 패스
                        break;
                    }
                }

                if (spawnedInThisBatch >= maxSpawnPerBatch)
                    break;
            }

            yield return new WaitForSeconds(Random.Range(minCheckInterval, maxCheckInterval));
        }
    }

    // 스폰 가능한 유효 타일 셀 캐싱
    void CacheWalkableCells()
    {
        cachedWalkableCells.Clear();

        foreach (var wTilemap in walkableTilemaps)
        {
            if (wTilemap == null) continue;

            BoundsInt bounds = wTilemap.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (!wTilemap.HasTile(pos)) continue;

                // 스폰 불가 타일맵과 겹치는 위치는 제외
                Vector3 worldPos = wTilemap.GetCellCenterWorld(pos);
                if (IsBlockedPosition(worldPos)) continue;

                cachedWalkableCells.Add(pos);
            }
        }
    }

    // 한 마리 스폰 검증 및 생성
    bool TrySpawnOneEntity(SpawnRule rule)
    {
        if (cachedWalkableCells.Count == 0) return false;

        Tilemap refTilemap = walkableTilemaps.Find(t => t != null);
        if (refTilemap == null) return false;

        for (int i = 0; i < 50; i++)
        {
            Vector3Int cell = cachedWalkableCells[Random.Range(0, cachedWalkableCells.Count)];
            Vector3 worldPos = refTilemap.GetCellCenterWorld(cell);
            worldPos.z = 0;

            // 1. 카메라 시야각 밖 검증 (화면 + 마진 영역 밖인지 확인)
            if (mainCam != null)
            {
                Vector3 vp = mainCam.WorldToViewportPoint(worldPos);
                if (vp.x >= -viewportMargin && vp.x <= 1 + viewportMargin &&
                    vp.y >= -viewportMargin && vp.y <= 1 + viewportMargin)
                {
                    continue;
                }
            }

            // 2. 플레이어와의 최소 거리 검증
            if (playerTransform != null &&
                Vector3.Distance(worldPos, playerTransform.position) < minDistanceFromPlayer)
            {
                continue;
            }

            // 3. 충돌체 중복(타 개체 겹침) 검증
            if (Physics2D.OverlapCircle(worldPos, spawnCheckRadius, livingEntityLayer))
            {
                continue;
            }

            // 4. 금지 타일맵(물/벽 등) 위치 여부 검증
            if (IsBlockedPosition(worldPos))
            {
                continue;
            }

            // 5. 조건 통과 시 인스턴스화
            GameObject go = Instantiate(rule.prefab, worldPos, Quaternion.identity);

            // EnemyBaseAI 타일맵 참조 동기화 보장
            EnemyBaseAI ai = go.GetComponent<EnemyBaseAI>();
            if (ai != null)
            {
                if (ai.walkableTilemaps == null || ai.walkableTilemaps.Count == 0)
                    ai.walkableTilemaps = new List<Tilemap>(walkableTilemaps);

                if (ai.blockedTilemaps == null || ai.blockedTilemaps.Count == 0)
                    ai.blockedTilemaps = new List<Tilemap>(blockedTilemaps);
            }

            activeEntities.Add(go);
            return true;
        }

        return false;
    }

    bool IsBlockedPosition(Vector2 worldPos)
    {
        foreach (var bTilemap in blockedTilemaps)
        {
            if (bTilemap != null && bTilemap.HasTile(bTilemap.WorldToCell(worldPos)))
                return true;
        }
        return false;
    }
}