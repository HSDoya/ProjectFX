using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;   // 기본 스폰 가능 땅
    public Tilemap waterTilemap;    // 스폰 불가 지역
    public LayerMask animalLayer;
    public float spawnCheckRadius = 0.5f;

    [Header("카메라 밖 스폰 설정")]
    public float minDistanceFromPlayer = 12f;
    public Transform playerTransform;

    [System.Serializable]
    public class AnimalRule
    {
        public string animalName;
        public GameObject prefab;
        public int targetCount = 5;
    }

    [Header("Spawn Rules")]
    public List<AnimalRule> animalRules = new List<AnimalRule>();

    public float minCheckInterval = 2f;
    public float maxCheckInterval = 5f;
    public bool spawnImmediatelyOnStart = false;

    private List<Vector3Int> groundCells = new List<Vector3Int>();
    private List<GameObject> activeAnimals = new List<GameObject>();
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        CacheGroundCells();
        StartCoroutine(SequentialPopulationControlRoutine());
    }

    IEnumerator SequentialPopulationControlRoutine()
    {
        if (!spawnImmediatelyOnStart)
            yield return new WaitForSeconds(minCheckInterval);

        while (true)
        {
            activeAnimals.RemoveAll(a => a == null);

            foreach (var rule in animalRules)
            {
                int count = 0;
                foreach (var a in activeAnimals)
                {
                    if (a != null && a.name.StartsWith(rule.prefab.name))
                        count++;
                }

                if (count < rule.targetCount)
                {
                    if (TrySpawnOneAnimal(rule))
                        break;
                }
            }

            yield return new WaitForSeconds(Random.Range(minCheckInterval, maxCheckInterval));
        }
    }

    void CacheGroundCells()
    {
        groundCells.Clear();
        BoundsInt bounds = groundTilemap.cellBounds;
            
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!groundTilemap.HasTile(pos)) continue;
            if (waterTilemap != null && waterTilemap.HasTile(pos)) continue;

            groundCells.Add(pos);
        }
    }

    // ===============================
    // ⭐ 한 마리 스폰
    // ===============================
    bool TrySpawnOneAnimal(AnimalRule rule)
    {
        for (int i = 0; i < 50; i++)
        {
            Vector3Int cell = groundCells[Random.Range(0, groundCells.Count)];
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(cell);
            worldPos.z = 0;

            // 카메라 안쪽 제외
            Vector3 vp = mainCam.WorldToViewportPoint(worldPos);
            if (vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1)
                continue;

            if (playerTransform != null &&
                Vector3.Distance(worldPos, playerTransform.position) < minDistanceFromPlayer)
                continue;

            if (Physics2D.OverlapCircle(worldPos, spawnCheckRadius, animalLayer))
                continue;

            // ⭐ AnimalAI 이동 가능 여부 1회 검사
            GameObject go = Instantiate(rule.prefab, worldPos, Quaternion.identity);
            AnimalAI ai = go.GetComponent<AnimalAI>();

            if (ai != null && !ai.CanMoveTo(worldPos))
            {
                Destroy(go);
                continue;
            }

            activeAnimals.Add(go);
            return true;
        }

        return false;
    }
}
