using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class MapGenerator : MonoBehaviour
{
    [Header("타일맵 연결")]
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;
    public TileBase groundTile;
    public TileBase waterTile;

    [Header("청크(구역) 맵 생성 설정")]
    public int chunkSize = 40;
    public float noiseScale = 0.08f;
    [Range(0f, 1f)] public float waterThreshold = 0.4f;

    [Header("섬 형태 만들기 (청크별 독립된 섬)")]
    [Tooltip("수치가 클수록 구역의 가장자리가 물이 되어 섬이 둥글고 작아집니다. (권장: 0.8 ~ 1.2)")]
    public float islandEdgePenalty = 0.8f;
    [Tooltip("수치가 클수록 구역 중앙에 땅이 생길 확률이 높아집니다. (권장: 0.3 ~ 0.5)")]
    public float islandCenterBoost = 0.3f;

    [Header("기타 설정")]
    public Transform player;
    public int safeZoneRadius = 8;
    public ObjectSpawner objectSpawner;

    [Header("시드")]
    public bool useRandomSeed = true;
    public int seed;

    private float offsetX;
    private float offsetY;
    private HashSet<Vector2Int> generatedChunks = new HashSet<Vector2Int>();

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

    public void GenerateChunk(Vector2Int chunkCoord)
    {
        if (generatedChunks.Contains(chunkCoord)) return;

        int startX = (chunkCoord.x * chunkSize) - (chunkSize / 2);
        int startY = (chunkCoord.y * chunkSize) - (chunkSize / 2);

        // 청크 내부의 중심점(localCenter)을 구합니다.
        Vector2 localCenter = new Vector2(chunkSize / 2f, chunkSize / 2f);
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

                //  핵심: 청크(구역)별로 둥근 섬의 모양을 깎아냅니다!
                float distanceToLocalCenter = Vector2.Distance(new Vector2(x, y), localCenter);
                float normalizedDistance = distanceToLocalCenter / maxLocalDistance;

                // 중앙은 가산점, 외곽은 감점을 주어 둥근 섬 형태를 유도합니다.
                float centerBoost = 1f - normalizedDistance;
                noiseValue += (centerBoost * islandCenterBoost); // 구역 중앙은 땅이 되기 쉬움
                noiseValue -= (normalizedDistance * islandEdgePenalty); // 구역 외곽은 무조건 물이 됨

                Vector3Int tilePosition = new Vector3Int(worldX, worldY, 0);

                // 스폰 지점 주변을 부드러운 원형/타원형 땅으로 보장합니다!
                // Y축 값에 1.5f를 곱해주면, 세로보다 가로가 더 넓은 자연스러운 '타원형'이 됩니다.
                float distFromCenter = Vector2.Distance(Vector2.zero, new Vector2(worldX, worldY * 1.5f));
                bool isSafeZone = distFromCenter <= safeZoneRadius;

                if (isSafeZone || noiseValue > waterThreshold)
                    groundTilemap.SetTile(tilePosition, groundTile);
                else
                    waterTilemap.SetTile(tilePosition, waterTile);
            }
        }

        generatedChunks.Add(chunkCoord);

        if (chunkCoord == Vector2Int.zero && player != null)
        {
            player.position = Vector3.zero;
        }

        if (objectSpawner != null)
        {
            objectSpawner.SpawnObjectsInChunk(chunkCoord, chunkSize);
        }
    }
}
