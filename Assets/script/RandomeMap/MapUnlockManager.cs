using UnityEngine;
using System.Collections.Generic;
// 청크의 현재 상태를 정의합니다.
public enum ChunkState
{
    Unexplored,  // 아직 발견되지 않음 (접근 불가)
    Purchasable, // 주변 땅이 열려서 구매(해금) 가능해짐
    Unlocked     // 이미 개방된 땅
}

public class MapUnlockManager : MonoBehaviour
{
    public MapGenerator mapGenerator;
    public GameObject unlockSignPrefab;

    [Header("해금 설정")]
    public int baseUnlockCost = 50;

    [Header("맵 크기 제한")]
    [Tooltip("중앙을 기준으로 상하좌우 몇 칸까지 확장할지 정합니다. 2로 설정하면 5x5 맵이 됩니다.")]
    public int maxGridRadius = 2; // -2, -1, 0, 1, 2 => 총 5칸 (5x5)

    private Dictionary<Vector2Int, ChunkState> chunkStates = new Dictionary<Vector2Int, ChunkState>();

    void Start()
    {
        if (mapGenerator != null)
        {
            mapGenerator.InitMap();
        }

        // 중앙 (0, 0) 구역 무료 개방
        UnlockChunk(new Vector2Int(0, 0), true);
    }

    public void UnlockChunk(Vector2Int coord, bool isFree = false)
    {
        if (chunkStates.ContainsKey(coord) && chunkStates[coord] == ChunkState.Unlocked) return;

        if (!isFree)
        {
            Debug.Log($" {coord} 구역을 {baseUnlockCost} 골드에 개방했습니다!");
        }

        chunkStates[coord] = ChunkState.Unlocked;
        mapGenerator.GenerateChunk(coord);
        UpdateNeighbors(coord);
    }

    private void UpdateNeighbors(Vector2Int coord)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int neighbor = coord + dir;

            //  핵심: 5x5 그리드 제한 (x와 y 좌표가 -2 ~ 2 사이일 때만 표지판 생성)
            if (Mathf.Abs(neighbor.x) <= maxGridRadius && Mathf.Abs(neighbor.y) <= maxGridRadius)
            {
                if (!chunkStates.ContainsKey(neighbor) || chunkStates[neighbor] == ChunkState.Unexplored)
                {
                    chunkStates[neighbor] = ChunkState.Purchasable;
                    SpawnUnlockSign(neighbor);
                }
            }
        }
    }

    private void SpawnUnlockSign(Vector2Int coord)
    {
        //  수정됨: 청크가 중앙 정렬되었으므로, 표지판의 위치도 딱 맞게 교정합니다!
        float worldX = coord.x * mapGenerator.chunkSize;
        float worldY = coord.y * mapGenerator.chunkSize;
        Vector3 spawnPos = new Vector3(worldX, worldY, 0);

        GameObject sign = Instantiate(unlockSignPrefab, spawnPos, Quaternion.identity, this.transform);

        UnlockSign signScript = sign.GetComponent<UnlockSign>();
        if (signScript != null)
        {
            signScript.Setup(this, coord, baseUnlockCost);
        }
    }
}
