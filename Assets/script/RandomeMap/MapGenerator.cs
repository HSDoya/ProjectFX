using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("타일맵 연결")]
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;

    [Header("타일 연결")]
    public TileBase groundTile;
    public TileBase waterTile;

    [Header("맵 생성 설정")]
    public int mapWidth = 80;  // 섬을 크게 만들기 위해 기본 맵 크기를 좀 더 키웠습니다
    public int mapHeight = 80;
    public float noiseScale = 0.08f; // 약간 더 큼직한 지형이 나오도록 수치 조정
    [Range(0f, 1f)]
    public float waterThreshold = 0.4f;

    [Header("중앙 거대 섬 설정 (Falloff)")]
    [Tooltip("중앙으로 갈수록 땅이 될 확률을 높여줍니다 (기본값: 0.5)")]
    public float centerIslandBoost = 0.5f;
    [Tooltip("외곽으로 갈수록 물이 될 확률을 높여줍니다 (기본값: 0.5)")]
    public float edgeWaterPenalty = 0.5f;

    [Header("플레이어 설정")]
    public Transform player;
    public int safeZoneRadius = 2; // 스폰 지점 확정 땅 반경

    [Header("랜덤 시드")]
    public bool useRandomSeed = true;
    public int seed;

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        groundTilemap.ClearAllTiles();
        waterTilemap.ClearAllTiles();

        if (useRandomSeed)
        {
            seed = Random.Range(0, 100000);
        }

        System.Random prng = new System.Random(seed);
        float offsetX = prng.Next(-100000, 100000);
        float offsetY = prng.Next(-100000, 100000);

        int startX = -mapWidth / 2;
        int startY = -mapHeight / 2;

        // 맵의 중심점(최대 거리 계산용)
        Vector2 mapCenter = new Vector2(mapWidth / 2f, mapHeight / 2f);
        float maxDistance = Mathf.Max(mapWidth, mapHeight) / 2f;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float pX = x * noiseScale + offsetX;
                float pY = y * noiseScale + offsetY;

                // 1. 기본 펄린 노이즈 값 가져오기
                float noiseValue = Mathf.PerlinNoise(pX, pY);

                // 2. 중심으로부터의 거리 계산 (0 = 완전 중심, 1 = 맵 끝)
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), mapCenter);
                float normalizedDistance = distanceToCenter / maxDistance;

                // 3. Falloff 적용: 중앙은 가산점(+), 외곽은 감점(-)
                float centerBoost = 1f - normalizedDistance; // 중심일수록 1, 외곽일수록 0
                noiseValue += (centerBoost * centerIslandBoost);    // 중앙에 땅 가산점 추가
                noiseValue -= (normalizedDistance * edgeWaterPenalty); // 외곽에 물 감점 추가

                Vector3Int tilePosition = new Vector3Int(startX + x, startY + y, 0);

                bool isCenterSafeZone = Mathf.Abs(tilePosition.x) <= safeZoneRadius &&
                                        Mathf.Abs(tilePosition.y) <= safeZoneRadius;

                // 4. 최종 계산된 값으로 지형 판별
                if (isCenterSafeZone || noiseValue > waterThreshold)
                {
                    groundTilemap.SetTile(tilePosition, groundTile);
                }
                else
                {
                    waterTilemap.SetTile(tilePosition, waterTile);
                }
            }
        }

        Debug.Log($"맵 생성이 완료되었습니다! (Seed: {seed})");

        if (player != null)
        {
            Vector3 centerWorldPos = groundTilemap.GetCellCenterWorld(Vector3Int.zero);
            centerWorldPos.z = 0;
            player.position = centerWorldPos;
        }
    }
}
