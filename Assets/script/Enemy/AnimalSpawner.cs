using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;   // 기본 스폰 가능 땅
    public Tilemap waterTilemap;    // 스폰 불가 지역
    public LayerMask animalLayer;   // 적/동물 레이어 (중복 생성 방지용)
    public float spawnCheckRadius = 0.5f;

    [Header("카메라 밖 스폰 설정")]
    public float minDistanceFromPlayer = 12f;
    public Transform playerTransform;

    [System.Serializable]
    public class AnimalRule
    {
        public string animalName;   // 기획용 적/동물 이름
        public GameObject prefab;   // EnemyBaseAI가 부착된 닭/소/슬라임 프리팹
        public int targetCount = 5; // 최대 스폰 유치 마리 수
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
            // 리스트에서 이미 죽어서 파괴된(null) 객체들을 실시간으로 청소
            activeAnimals.RemoveAll(a => a == null);

            foreach (var rule in animalRules)
            {
                int count = 0;
                foreach (var a in activeAnimals)
                {
                    // 복사본 생성 시 이름 뒤에 (Clone)이 붙으므로 StartsWith로 수량을 카운팅
                    if (a != null && a.name.StartsWith(rule.prefab.name))
                        count++;
                }

                // 기획한 타겟 마리 수보다 적으면 한 마리 생성을 시도
                if (count < rule.targetCount)
                {
                    if (TrySpawnOneAnimal(rule))
                        break; // 한 번에 한 마리씩만 안정적으로 생성하기 위해 탈출
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

    // ⭐ 한 마리 스폰 (EnemyBaseAI 규격에 맞춤)
    bool TrySpawnOneAnimal(AnimalRule rule)
    {
        for (int i = 0; i < 50; i++)
        {
            Vector3Int cell = groundCells[Random.Range(0, groundCells.Count)];
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(cell);
            worldPos.z = 0;

            // 1. 카메라 안쪽 스폰 필터링
            Vector3 vp = mainCam.WorldToViewportPoint(worldPos);
            if (vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1)
                continue;

            // 2. 플레이어와 너무 가깝게 스폰되는 것 필터링
            if (playerTransform != null &&
                Vector3.Distance(worldPos, playerTransform.position) < minDistanceFromPlayer)
                continue;

            // 3. 해당 위치에 이미 다른 생명체가 겹쳐있는지 필터링
            if (Physics2D.OverlapCircle(worldPos, spawnCheckRadius, animalLayer))
                continue;

            // 4. 일단 오브젝트 생성
            GameObject go = Instantiate(rule.prefab, worldPos, Quaternion.identity);

            //수정 완료, 옛날 AnimalAI 대신 통합본인 EnemyBaseAI 컴포넌트를 추적합니다.
            EnemyBaseAI ai = go.GetComponent<EnemyBaseAI>();

            // ⭐ 타일맵상 진짜 밟을 수 있는 공간이 맞는지 1회 사전 검증
            if (ai != null && !ai.CanMoveTo(worldPos))
            {
                // 바다 한가운데나 벽 속처럼 잘못 배치되었다면 생성 취소하고 삭제
                Destroy(go);
                continue;
            }

            // 검증이 완료된 정상적인 개체만 관리 리스트에 추가
            activeAnimals.Add(go);
            return true;
        }

        return false;
    }
}