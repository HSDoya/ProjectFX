using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("타일맵 연결")]
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;
    public TileBase groundTile;
    public TileBase waterTile;


    [Header("맵 생성 설정")]
    public int mapWidth = 80;
    public int mapHeight = 80;
    public float noiseScale = 0.08f;
    [Range(0f, 1f)] public float waterThreshold = 0.4f;

    [Header("중앙 섬 설정 (Falloff)")]
    public float centerIslandBoost = 0.5f;
    public float edgeWaterPenalty = 0.5f;

    [Header("기타 설정")]
    public Transform player;
    public int safeZoneRadius = 2;
    public ObjectSpawner objectSpawner; // 여기에 ObjectSpawner 연결

    [Header("시드")]
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

        if (useRandomSeed) seed = Random.Range(0, 100000);
        System.Random prng = new System.Random(seed);
        float offsetX = prng.Next(-100000, 100000);
        float offsetY = prng.Next(-100000, 100000);

        int startX = -mapWidth / 2;
        int startY = -mapHeight / 2;
        Vector2 mapCenter = new Vector2(mapWidth / 2f, mapHeight / 2f);
        float maxDistance = Mathf.Max(mapWidth, mapHeight) / 2f;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float pX = x * noiseScale + offsetX;
                float pY = y * noiseScale + offsetY;
                float noiseValue = Mathf.PerlinNoise(pX, pY);

                float distanceToCenter = Vector2.Distance(new Vector2(x, y), mapCenter);
                float normalizedDistance = distanceToCenter / maxDistance;

                float centerBoost = 1f - normalizedDistance;
                noiseValue += (centerBoost * centerIslandBoost);
                noiseValue -= (normalizedDistance * edgeWaterPenalty);

                Vector3Int tilePosition = new Vector3Int(startX + x, startY + y, 0);
                bool isSafeZone = Mathf.Abs(tilePosition.x) <= safeZoneRadius && Mathf.Abs(tilePosition.y) <= safeZoneRadius;

                if (isSafeZone || noiseValue > waterThreshold)
                    groundTilemap.SetTile(tilePosition, groundTile);
                else
                    waterTilemap.SetTile(tilePosition, waterTile);
            }
        }

        // 플레이어 배치
        if (player != null)
        {
            player.position = groundTilemap.GetCellCenterWorld(Vector3Int.zero);
        }

        // 오브젝트 배치 호출
        if (objectSpawner != null)
        {
            objectSpawner.SpawnInitialObjects();
        }
    }
}
