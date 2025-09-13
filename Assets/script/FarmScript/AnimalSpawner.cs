using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;
    [System.Serializable]
    public class WeightedPrefab
    {
        public GameObject prefab;   // 닭/소 프리팹 (AnimalAI 포함)
        public int weight = 1;      // 스폰 비중
        public int count = 3;       // 스폰 개수(이 프리팹 목표 수)
    }
    [Header("Prefabs & Counts")]
    public List<WeightedPrefab> animals = new List<WeightedPrefab>();

    [Header("Placement")]
    public float minDistanceBetweenAnimals = 0.8f;  // 서로 겹침 방지 반경
    public LayerMask overlapMask;                    // Animal 레이어 등(없으면 0)
    public int maxAttemptsPerAnimal = 100;          // 자리를 못 찾을 때 반복 횟수

    [Header("Spawn Timing")]
    public bool spawnOnStart = true;
    public bool allowRespawn = false;               // 주기 리스폰 여부
    public float respawnInterval = 20f;

    private List<Vector3Int> groundCells = new List<Vector3Int>();
    private readonly List<GameObject> spawned = new List<GameObject>();

    void Start()
    {
        CacheGroundCells();

        if (spawnOnStart) SpawnAll();

        if (allowRespawn) InvokeRepeating(nameof(RespawnTick), respawnInterval, respawnInterval);
    }
    private void CacheGroundCells()
    {
        groundCells.Clear();
        BoundsInt bounds = groundTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!groundTilemap.HasTile(cell)) continue;
                if (waterTilemap != null && waterTilemap.HasTile(cell)) continue;
                groundCells.Add(cell);
            }
        }
    }
    private void SpawnAll()
    {
        foreach (var wp in animals)
        {
            if (wp.prefab == null || wp.count <= 0) continue;

            int spawnedCount = 0;
            int attempts = 0;

            while (spawnedCount < wp.count && attempts < maxAttemptsPerAnimal * wp.count)
            {
                attempts++;
                if (TryGetRandomFreeSpot(out Vector3 worldPos))
                {
                    var go = Instantiate(wp.prefab, worldPos, Quaternion.identity);
                    spawned.Add(go);
                    spawnedCount++;
                }
            }
        }
    }
    private bool TryGetRandomFreeSpot(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        if (groundCells.Count == 0) return false;

        // 랜덤 셀 선택
        Vector3Int cell = groundCells[Random.Range(0, groundCells.Count)];
        worldPos = groundTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

        // 겹침 검사
        if (overlapMask.value != 0)
        {
            var hit = Physics2D.OverlapCircle(worldPos, minDistanceBetweenAnimals, overlapMask);
            if (hit != null) return false;
        }

        return true;
    }
    private void RespawnTick()
    {
        // 살아있는 수를 기준으로 부족분 보충
        Dictionary<GameObject, int> liveCount = new Dictionary<GameObject, int>();

        // 현재 살아있는 프리팹 타입 집계(프리팹 비교 간단화를 위해 name 기준 사용)
        foreach (var wp in animals)
            liveCount[wp.prefab] = 0;

        // 아직 살아있는 개체 파악
        spawned.RemoveAll(go => go == null);
        foreach (var go in spawned)
        {
            if (go == null) continue;
            foreach (var wp in animals)
            {
                // 프리팹 원본 비교가 어렵다면 name 비교(“(Clone)” 제거)
                if (go.name.StartsWith(wp.prefab.name))
                {
                    liveCount[wp.prefab]++;
                    break;
                }
            }
        }
        // 부족분 스폰
        foreach (var wp in animals)
        {
            int need = Mathf.Max(0, wp.count - liveCount[wp.prefab]);
            int spawnedCount = 0;
            int attempts = 0;

            while (spawnedCount < need && attempts < maxAttemptsPerAnimal * Mathf.Max(1, need))
            {
                attempts++;
                if (TryGetRandomFreeSpot(out Vector3 worldPos))
                {
                    var go = Instantiate(wp.prefab, worldPos, Quaternion.identity);
                    spawned.Add(go);
                    spawnedCount++;
                }
            }
        }
    }
}
