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
        public GameObject prefab;   // ��/�� ������ (AnimalAI ����)
        public int weight = 1;      // ���� ����
        public int count = 3;       // ���� ����(�� ������ ��ǥ ��)
    }
    [Header("Prefabs & Counts")]
    public List<WeightedPrefab> animals = new List<WeightedPrefab>();

    [Header("Placement")]
    public float minDistanceBetweenAnimals = 0.8f;  // ���� ��ħ ���� �ݰ�
    public LayerMask overlapMask;                    // Animal ���̾� ��(������ 0)
    public int maxAttemptsPerAnimal = 100;          // �ڸ��� �� ã�� �� �ݺ� Ƚ��

    [Header("Spawn Timing")]
    public bool spawnOnStart = true;
    public bool allowRespawn = false;               // �ֱ� ������ ����
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

        // ���� �� ����
        Vector3Int cell = groundCells[Random.Range(0, groundCells.Count)];
        worldPos = groundTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

        // ��ħ �˻�
        if (overlapMask.value != 0)
        {
            var hit = Physics2D.OverlapCircle(worldPos, minDistanceBetweenAnimals, overlapMask);
            if (hit != null) return false;
        }

        return true;
    }
    private void RespawnTick()
    {
        // ����ִ� ���� �������� ������ ����
        Dictionary<GameObject, int> liveCount = new Dictionary<GameObject, int>();

        // ���� ����ִ� ������ Ÿ�� ����(������ �� ����ȭ�� ���� name ���� ���)
        foreach (var wp in animals)
            liveCount[wp.prefab] = 0;

        // ���� ����ִ� ��ü �ľ�
        spawned.RemoveAll(go => go == null);
        foreach (var go in spawned)
        {
            if (go == null) continue;
            foreach (var wp in animals)
            {
                // ������ ���� �񱳰� ��ƴٸ� name ��(��(Clone)�� ����)
                if (go.name.StartsWith(wp.prefab.name))
                {
                    liveCount[wp.prefab]++;
                    break;
                }
            }
        }
        // ������ ����
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
