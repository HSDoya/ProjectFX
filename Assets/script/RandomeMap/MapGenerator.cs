using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("타일맵 연결")]
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;
    public TileBase groundTile;
    public TileBase waterTile;

    [Header("청크(구역) 맵 생성 설정")]
    public int chunkSize = 200;
    public float noiseScale = 0.08f;
    [Range(0f, 1f)] public float waterThreshold = 0.4f;

    [Header("섬 크기 및 고립 설정")]
    [Tooltip("시작 섬의 기본 크기 반경 (권장: 10 ~ 14)")]
    public float startIslandRadius = 12f;
    [Tooltip("해금되는 섬들의 기본 크기 반경 (권장: 8 ~ 12)")]
    public float unlockedIslandRadius = 10f;
    [Tooltip("해금된 섬의 테두리가 자연스럽게 구불구불해지는 정도 (권장: 4 ~ 8)")]
    public float shapeWobbleAmount = 6f;
    [Tooltip("구역 끝부분을 강제로 바다로 만드는 힘 (대륙 끊김 방지) 권장: 2.0")]
    public float islandEdgePenalty = 2.0f;

    [Header("기타 설정")]
    public Transform player;
    public ObjectSpawner objectSpawner;

    [Header("시드")]
    public bool useRandomSeed = true;
    public int seed;

    [Header("개발용")]
    [Tooltip("켜면 세이브의 생성 설정을 무시하고 현재 인스펙터 값으로 지형을 만든다. " +
             "생성 파라미터를 조정하는 동안에만 켠다. 시드는 그대로 적용된다.")]
    public bool ignoreSavedWorldSettings;

    /// <summary>
    /// 생성 규칙 버전. 지형이나 오브젝트 배치 방식을 바꿀 때마다 올린다.
    ///
    /// 세이브 포맷 버전과는 다른 것이다. 포맷이 그대로여도 생성 규칙이 바뀌면
    /// 같은 시드가 다른 세계를 만든다. 그 사실을 불러올 때 알려주기 위한 번호다.
    ///
    ///   1: 나무/돌을 전역 UnityEngine.Random으로 뿌리던 시절 (배치 재현 불가)
    ///   2: 청크별 System.Random으로 뿌린다 (시드로 배치 재현)
    /// </summary>
    public const int GenVersion = 2;

    private float offsetX;
    private float offsetY;
    private HashSet<Vector2Int> generatedChunks = new HashSet<Vector2Int>();

    // ───────────────────────── 저장 / 불러오기 ─────────────────────────

    public void CaptureSettings(WorldGenSave w)
    {
        w.genVersion = GenVersion;
        w.hasSeed = true;
        w.seed = seed;

        w.hasSettings = true;
        w.chunkSize = chunkSize;
        w.noiseScale = noiseScale;
        w.waterThreshold = waterThreshold;
        w.startIslandRadius = startIslandRadius;
        w.unlockedIslandRadius = unlockedIslandRadius;
        w.shapeWobbleAmount = shapeWobbleAmount;
        w.islandEdgePenalty = islandEdgePenalty;
    }

    /// <summary>
    /// 시드만 되돌린다. 설정과 분리된 이유: ignoreSavedWorldSettings를 켜고 파라미터를
    /// 조정하는 중에도 시드는 적용되어야 한다. 시드까지 무시하면 아예 다른 세계가 된다.
    /// </summary>
    public void ApplySeed(WorldGenSave w, List<string> changes)
    {
        if (w == null || !w.hasSeed) return;

        if (seed != w.seed) changes?.Add($"seed {seed} → {w.seed}");

        // 다음 InitMap이 시드를 새로 뽑지 않도록 끈다.
        useRandomSeed = false;
        seed = w.seed;
    }

    public void ApplySettings(WorldGenSave w, List<string> changes)
    {
        if (w == null || !w.hasSettings) return;

        // chunkSize는 좌표 체계의 기준이다. 0이 들어오면 세계가 만들어지지 않는다.
        if (w.chunkSize > 0) SaveDiff.Set(ref chunkSize, w.chunkSize, "chunkSize", changes);

        SaveDiff.Set(ref noiseScale, w.noiseScale, "noiseScale", changes);
        SaveDiff.Set(ref waterThreshold, w.waterThreshold, "waterThreshold", changes);
        SaveDiff.Set(ref startIslandRadius, w.startIslandRadius, "startIslandRadius", changes);
        SaveDiff.Set(ref unlockedIslandRadius, w.unlockedIslandRadius, "unlockedIslandRadius", changes);
        SaveDiff.Set(ref shapeWobbleAmount, w.shapeWobbleAmount, "shapeWobbleAmount", changes);
        SaveDiff.Set(ref islandEdgePenalty, w.islandEdgePenalty, "islandEdgePenalty", changes);
    }

    public void InitMap()
    {
        if (useRandomSeed) seed = Random.Range(0, 100000);
        System.Random prng = new System.Random(seed);
        offsetX = prng.Next(-100000, 100000);
        offsetY = prng.Next(-100000, 100000);

        groundTilemap.ClearAllTiles();
        waterTilemap.ClearAllTiles();
        generatedChunks.Clear();
    }

    // 월드 좌표가 어느 청크에 속하는지. GenerateChunk가 쓰는 오프셋 규칙
    // (startX = coord.x * chunkSize - chunkSize/2)의 역변환이다.
    // chunkSize와 그 규칙을 이 클래스가 소유하므로 역변환도 여기 둔다
    // (다른 곳에서 직접 계산하면 같은 규칙이 두 벌로 갈라진다).
    public Vector2Int WorldToChunk(Vector2 worldPos)
    {
        float half = chunkSize / 2f;
        return new Vector2Int(
            Mathf.FloorToInt((worldPos.x + half) / chunkSize),
            Mathf.FloorToInt((worldPos.y + half) / chunkSize));
    }

    public void GenerateChunk(Vector2Int chunkCoord)
    {
        if (generatedChunks.Contains(chunkCoord)) return;

        int startX = (chunkCoord.x * chunkSize) - (chunkSize / 2);
        int startY = (chunkCoord.y * chunkSize) - (chunkSize / 2);

        float centerOffset = chunkSize / 2f;
        float maxLocalDistance = chunkSize / 2f;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                int worldX = startX + x;
                int worldY = startY + y;

                float pX = worldX * noiseScale + offsetX;
                float pY = worldY * noiseScale + offsetY;
                float noiseValue = Mathf.PerlinNoise(pX, pY);

                float dx = x - centerOffset;
                float dy = y - centerOffset;
                float distanceToCenter;
                float baseRadius;

                if (chunkCoord == Vector2Int.zero)
                {
                    distanceToCenter = Mathf.Sqrt(dx * dx + (dy * 1.5f) * (dy * 1.5f));
                    baseRadius = startIslandRadius;
                }
                else
                {
                    float wobble = (Mathf.PerlinNoise(worldX * 0.15f + offsetX, worldY * 0.15f + offsetY) - 0.5f) * shapeWobbleAmount;
                    distanceToCenter = Mathf.Sqrt(dx * dx + dy * dy) + wobble;
                    baseRadius = unlockedIslandRadius;
                }

                // 지정된 baseRadius 안쪽은 0, 구역 끝(maxLocalDistance)으로 갈수록 1이 되는 비율
                float falloffStart = baseRadius;
                float falloffEnd = maxLocalDistance - 1f;
                float falloff = Mathf.InverseLerp(falloffStart, falloffEnd, distanceToCenter);

                // 반경 안쪽은 가산점(+1.0)을 주어 무조건 땅으로 만들고, 반경 밖은 페널티를 주어 바다로 만듦
                float finalNoiseValue = noiseValue + 1.0f - (falloff * islandEdgePenalty);

                Vector3Int tilePosition = new Vector3Int(worldX, worldY, 0);

                if (finalNoiseValue > waterThreshold)
                {
                    groundTilemap.SetTile(tilePosition, groundTile);
                }
                else
                {
                    waterTilemap.SetTile(tilePosition, waterTile);
                }
            }
        }

        generatedChunks.Add(chunkCoord);

        if (chunkCoord == Vector2Int.zero && player != null)
        {
            player.position = Vector3.zero;
        }

        if (objectSpawner != null)
        {
            objectSpawner.SpawnObjectsInChunk(chunkCoord, chunkSize, seed);
        }
    }
}