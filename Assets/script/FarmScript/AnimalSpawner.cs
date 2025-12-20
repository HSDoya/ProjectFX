using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;
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

    [Header("Spawn Settings")]
    public List<AnimalRule> animalRules = new List<AnimalRule>();

    [Tooltip("다음 개체를 스폰하기까지 대기할 시간 (초)")]
    public float minCheckInterval = 2f;
    public float maxCheckInterval = 5f;

    [Tooltip("시작하자마자 바로 스폰을 시작할지 여부")]
    public bool spawnImmediatelyOnStart = false;

    private List<Vector3Int> groundCells = new List<Vector3Int>();
    private List<GameObject> activeAnimals = new List<GameObject>();
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        // 1. 땅 타일 정보 저장
        CacheGroundCells();

        // 2. 순차적 스폰을 위해 코루틴 시작 (Start에서 바로 스폰하지 않음)
        StartCoroutine(SequentialPopulationControlRoutine());
    }

    // 하나씩 순차적으로 개체 수를 체크하고 보충하는 루틴
    IEnumerator SequentialPopulationControlRoutine()
    {
        // 시작 시 약간의 대기 (선택 사항)
        if (!spawnImmediatelyOnStart)
            yield return new WaitForSeconds(minCheckInterval);

        while (true)
        {
            // 리스트 정리 (죽은 개체 제거)
            activeAnimals.RemoveAll(a => a == null);

            bool spawnedAny = false;

            // 모든 규칙을 순회하며 딱 '한 마리'만 스폰 시도
            foreach (var rule in animalRules)
            {
                if (rule.prefab == null) continue;

                int currentCount = 0;
                foreach (var a in activeAnimals)
                {
                    if (a != null && a.name.StartsWith(rule.prefab.name))
                        currentCount++;
                }

                // 목표 수보다 적으면 한 마리 스폰하고 루프 탈출 (다음 대기 시간으로)
                if (currentCount < rule.targetCount)
                {
                    // 게임 시작 직후가 아니라면(또는 설정에 따라) 카메라 밖에서 스폰
                    // 초기에도 순차적으로 나오길 원하므로 기본적으로 카메라 체크를 함
                    if (TrySpawnOneAnimal(rule, false))
                    {
                        spawnedAny = true;
                        // 한 마리 스폰 성공했으므로 지정된 시간만큼 기다림
                        break;
                    }
                }
            }

            // 스폰을 했든, 모든 개체수가 꽉 찼든 다음 체크까지 랜덤하게 대기
            float waitTime = Random.Range(minCheckInterval, maxCheckInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void CacheGroundCells()
    {
        groundCells.Clear();
        if (groundTilemap == null) return;

        BoundsInt bounds = groundTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (groundTilemap.HasTile(pos))
            {
                if (waterTilemap == null || !waterTilemap.HasTile(pos))
                {
                    groundCells.Add(pos);
                }
            }
        }
    }

    // 한 마리만 스폰 시도하고 성공 여부 반환
    private bool TrySpawnOneAnimal(AnimalRule rule, bool ignoreCamera)
    {
        if (groundCells.Count == 0) return false;

        for (int i = 0; i < 50; i++)
        {
            Vector3Int cell = groundCells[Random.Range(0, groundCells.Count)];
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(cell);
            worldPos.z = 0;

            if (!ignoreCamera)
            {
                Vector3 viewportPos = mainCam.WorldToViewportPoint(worldPos);
                if (viewportPos.x > -0.1f && viewportPos.x < 1.1f && viewportPos.y > -0.1f && viewportPos.y < 1.1f)
                    continue;
            }

            if (playerTransform != null && Vector3.Distance(worldPos, playerTransform.position) < minDistanceFromPlayer)
                continue;

            if (Physics2D.OverlapCircle(worldPos, spawnCheckRadius, animalLayer) == null)
            {
                GameObject go = Instantiate(rule.prefab, worldPos, Quaternion.identity);
                activeAnimals.Add(go);
                return true; // 스폰 성공
            }
        }
        return false; // 스폰 실패 (빈 자리 없음)
    }
}