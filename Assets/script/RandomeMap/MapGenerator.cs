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
    public int chunkSize = 40;
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
            objectSpawner.SpawnObjectsInChunk(chunkCoord, chunkSize);
        }
    }
}