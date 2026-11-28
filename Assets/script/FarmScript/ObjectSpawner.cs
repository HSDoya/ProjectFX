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
        public int spawnCount = 10;
    }

    public List<StaticResourceRule> resourceRules = new List<StaticResourceRule>();
    // 런타임 전용. public인 채로 두면 씬에 직렬화되는데, 실제로 Tree.prefab 에셋
    // 참조가 한 개 섞여 저장돼 있었다. 그 상태로 ClearSpawned가 돌면 Destroy가
    // 에셋을 지우려 들어 "Destroying assets is not permitted" 에러가 난다.
    [System.NonSerialized]
    public List<GameObject> spawnedObjects = new List<GameObject>();

    /// <summary>
    /// 지금까지 뿌린 나무/돌을 전부 치운다. 세이브를 불러오며 지형을 다시 만들 때 쓴다.
    /// 이걸 안 하면 같은 자리에 두 벌이 겹쳐서 쌓인다.
    /// </summary>
    public void ClearSpawned()
    {
        foreach (var obj in spawnedObjects)
        {
            // 씬에 있는 인스턴스만 지운다. 에셋(프리팹)을 Destroy하면 에러가 난다.
            if (obj != null && obj.scene.IsValid()) Destroy(obj);
        }

        spawnedObjects.Clear();

        // 이것도 같이 비워야 한다. spawnedPositions는 "너무 가까운 자리"를 걸러내는 데
        // 쓰이므로, 지워진 옛 위치가 남아 있으면 새 배치를 계속 밀어낸다.
        // (다시 만들 때마다 나무가 조금씩 덜 생긴다.)
        spawnedPositions.Clear();
    }

    // 지형 시드에서 파생되는 난수열. SpawnObjectsInChunk가 청크마다 새로 만든다.
    private System.Random rng = new System.Random();

    private List<Vector3Int> currentChunkGroundTiles = new List<Vector3Int>();
    private List<Vector3> spawnedPositions = new List<Vector3>();

    /// <summary>
    /// 청크 하나에 나무/돌을 뿌린다.
    ///
    /// worldSeed는 MapGenerator의 지형 시드다. 같은 시드면 같은 배치가 나와야
    /// 세이브가 정수 하나로 세계를 복원할 수 있다.
    /// </summary>
    public void SpawnObjectsInChunk(Vector2Int chunkCoord, int chunkSize, int worldSeed)
    {
        // 청크마다 독립된 난수열을 만든다. 하나의 난수열을 이어 쓰면 구역을 어떤
        // 순서로 개방했는지가 배치를 바꿔버린다.
        //
        // 전역 UnityEngine.Random을 쓰지 않는 이유: 다른 시스템이 난수를 하나 뽑을 때마다
        // 뽑히는 값이 밀려서, 같은 시드라도 배치가 달라진다.
        rng = new System.Random(worldSeed ^ (chunkCoord.x * 73856093) ^ (chunkCoord.y * 19349663));

        WarnIfObstacleMaskEmpty();

        CacheGroundTilesInChunk(chunkCoord, chunkSize);
        SpawnInCachedTiles();
    }

    private bool warnedEmptyMask;

    /// <summary>
    /// obstacleLayer가 비어 있으면 Physics2D.OverlapCircle이 아무것도 감지하지 않는다.
    /// spawnCheckRadius를 설정해 둔 채로 장애물 검사가 조용히 무효가 되는 상태라 한 번 알린다.
    /// </summary>
    private void WarnIfObstacleMaskEmpty()
    {
        if (warnedEmptyMask || obstacleLayer.value != 0) return;

        warnedEmptyMask = true;
        GameLog.LogWarning("[Spawner] obstacleLayer가 비어 있어 장애물 검사가 동작하지 않습니다 " +
                           "(spawnCheckRadius 무효). 레이어를 지정하면 배치가 기존 콜라이더를 피합니다.");
    }

    // ───────────────────────── 저장 / 불러오기 ─────────────────────────

    public void CaptureSettings(WorldGenSave w)
    {
        w.spawnCheckRadius = spawnCheckRadius;
        w.minDistanceBetweenObjects = minDistanceBetweenObjects;
        w.maxPositionSearchTries = maxPositionSearchTries;

        var counts = new int[resourceRules.Count];
        for (int i = 0; i < resourceRules.Count; i++)
            counts[i] = resourceRules[i] == null ? 0 : resourceRules[i].spawnCount;

        w.ruleSpawnCounts = counts;
        w.rulesFingerprint = ComputeRulesFingerprint();
    }

    public void ApplySettings(WorldGenSave w, List<string> changes)
    {
        if (w == null || !w.hasSettings) return;

        SaveDiff.Set(ref spawnCheckRadius, w.spawnCheckRadius, "spawnCheckRadius", changes);
        SaveDiff.Set(ref minDistanceBetweenObjects, w.minDistanceBetweenObjects,
                     "minDistanceBetweenObjects", changes);

        // 0이면 아무것도 배치되지 않는다. 구버전/깨진 값은 무시한다.
        if (w.maxPositionSearchTries > 0)
            SaveDiff.Set(ref maxPositionSearchTries, w.maxPositionSearchTries,
                         "maxPositionSearchTries", changes);

        if (w.ruleSpawnCounts != null)
        {
            int count = Mathf.Min(w.ruleSpawnCounts.Length, resourceRules.Count);
            for (int i = 0; i < count; i++)
            {
                var rule = resourceRules[i];
                if (rule == null) continue;

                SaveDiff.Set(ref rule.spawnCount, w.ruleSpawnCounts[i],
                             $"{rule.ruleName}.spawnCount", changes);
            }

            if (w.ruleSpawnCounts.Length != resourceRules.Count)
                GameLog.LogWarning($"[Save] 자원 규칙 개수가 다릅니다 " +
                                   $"(세이브 {w.ruleSpawnCounts.Length} / 현재 {resourceRules.Count}).");
        }

        // 프리팹 구성은 되돌릴 수 없다. 달라졌다는 사실만 알린다.
        if (w.rulesFingerprint != 0 && w.rulesFingerprint != ComputeRulesFingerprint())
            GameLog.LogWarning("[Save] 자원 규칙의 프리팹 구성이 저장 당시와 다릅니다. " +
                               "나무/돌 배치가 달라집니다.");
    }

    /// <summary>
    /// 규칙 이름 + 프리팹 이름 + 순서를 정수 하나로 접는다.
    ///
    /// string.GetHashCode를 쓰지 않는 이유: 실행 간/런타임 간 안정성이 보장되지 않는다.
    /// 세이브에 적어두고 다음 실행에서 비교하는 값이므로 직접 계산한다 (FNV-1a).
    /// </summary>
    public int ComputeRulesFingerprint()
    {
        unchecked
        {
            int hash = 17;
            foreach (var rule in resourceRules)
            {
                if (rule == null) continue;

                hash = hash * 31 + StableHash(rule.ruleName);
                if (rule.prefabs == null) continue;

                foreach (var prefab in rule.prefabs)
                    hash = hash * 31 + StableHash(prefab == null ? null : prefab.name);
            }
            return hash;
        }
    }

    private static int StableHash(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;

        unchecked
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619u;
            }
            return (int)h;
        }
    }

    private void CacheGroundTilesInChunk(Vector2Int chunkCoord, int chunkSize)
    {
        currentChunkGroundTiles.Clear();

        // MapGenerator와 동일하게 검색 시작 좌표를 절반만큼 뒤로 당깁니다. (1사분면 쏠림 해결)
        int startX = (chunkCoord.x * chunkSize) - (chunkSize / 2);
        int startY = (chunkCoord.y * chunkSize) - (chunkSize / 2);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int pos = new Vector3Int(startX + x, startY + y, 0);

                // 물이 없고 땅만 있는 타일 수집
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
            for (int i = 0; i < rule.spawnCount; i++)
            {
                TrySpawn(rule);
            }
        }
    }

    private bool TrySpawn(StaticResourceRule rule)
    {
        // 규칙에 프리팹이 안 꽂혀 있으면 할 일이 없다. 반복 안에서 검사하면 매번 돈다.
        if (rule.prefabs == null || rule.prefabs.Length == 0) return false;

        for (int i = 0; i < maxPositionSearchTries; i++)
        {
            Vector3Int tilePos = currentChunkGroundTiles[rng.Next(currentChunkGroundTiles.Count)];
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(tilePos);
            worldPos.z = 0;

            Collider2D hit = Physics2D.OverlapCircle(worldPos, spawnCheckRadius, obstacleLayer);
            if (hit != null) continue;

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

            GameObject prefab = rule.prefabs[rng.Next(rule.prefabs.Length)];
            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, this.transform);

            spawnedObjects.Add(obj);
            spawnedPositions.Add(worldPos);
            return true;
        }
        return false;
    }
}