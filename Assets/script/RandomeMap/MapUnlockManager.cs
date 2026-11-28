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

    // 구역별 표지판. 표지판을 만든 쪽이 지우는 일도 맡기 위해 들고 있는다.
    private Dictionary<Vector2Int, GameObject> unlockSigns = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        if (mapGenerator != null)
        {
            // 세이브가 있으면 그 시드와 생성 설정으로 처음부터 만든다.
            //
            // 이게 없으면 매 실행마다 무작위 시드로 지형을 만들고(청크 하나가 4만 타일),
            // 불러오기가 그걸 전부 부수고 저장된 시드로 다시 만든다. 즉 "시드가 다른 경우"가
            // 예외가 아니라 재시작 후 불러오기의 기본 경로가 된다.
            // 0번이 아니라 마지막에 쓰던 슬롯을 본다. 그래야 그 슬롯을 불러올 때
            // 지형이 이미 같아서 다시 만들지 않는다.
            if (SaveManager.PeekWorld(SaveManager.LastUsedSlot, out WorldGenSave savedWorld))
                SaveManager.ApplyWorld(savedWorld);

            mapGenerator.InitMap();
        }

        // 중앙 (0, 0) 구역 무료 개방
        UnlockChunk(new Vector2Int(0, 0), true);
    }

    // ───────────────────────── 저장 / 불러오기 ─────────────────────────

    public void CaptureSettings(WorldGenSave w)
    {
        w.maxGridRadius = maxGridRadius;
    }

    public void ApplySettings(WorldGenSave w, List<string> changes)
    {
        if (w == null || !w.hasSettings) return;

        SaveDiff.Set(ref maxGridRadius, w.maxGridRadius, "maxGridRadius", changes);
    }

    /// <summary>구역 개방 상태를 배열로 펴서 내보낸다. (JsonUtility가 Dictionary를 다루지 못한다)</summary>
    public ChunkSave[] CaptureChunks()
    {
        var list = new List<ChunkSave>(chunkStates.Count);
        foreach (var pair in chunkStates)
            list.Add(new ChunkSave { x = pair.Key.x, y = pair.Key.y, state = (int)pair.Value });

        return list.ToArray();
    }

    /// <summary>
    /// 세이브의 구역 상태를 반영한다.
    ///
    /// rebuildWorld가 true면 지형부터 다시 만든다. Start에서 세이브의 시드를 미리 읽어
    /// 세계를 만들기 때문에 보통은 false로 들어와 아무것도 부수지 않는다.
    ///
    /// Purchasable(표지판) 상태는 따로 복원하지 않는다. UnlockChunk가 이웃을 자동으로
    /// Purchasable로 만들므로, 개방된 구역만 복원하면 표지판은 알아서 제자리에 선다.
    /// </summary>
    public void LoadChunks(ChunkSave[] saved, bool rebuildWorld)
    {
        if (rebuildWorld)
        {
            ClearUnlockSigns();

            if (mapGenerator != null)
            {
                if (mapGenerator.objectSpawner != null) mapGenerator.objectSpawner.ClearSpawned();
                mapGenerator.InitMap(); // 타일맵을 비우고 시드로 오프셋을 다시 잡는다
            }

            chunkStates.Clear();
        }

        bool any = false;
        if (saved != null)
        {
            foreach (var chunk in saved)
            {
                if ((ChunkState)chunk.state != ChunkState.Unlocked) continue;
                UnlockChunk(new Vector2Int(chunk.x, chunk.y), true);
                any = true;
            }
        }

        // 구버전 세이브거나 내용이 비었다면 최소한 시작 구역은 열어둔다.
        if (!any) UnlockChunk(new Vector2Int(0, 0), true);
    }

    private void ClearUnlockSigns()
    {
        foreach (var pair in unlockSigns)
            if (pair.Value != null) Destroy(pair.Value);

        unlockSigns.Clear();
    }

    /// <summary>
    /// 해당 구역의 표지판을 치운다.
    ///
    /// 예전에는 UnlockSign이 클릭을 받으면 스스로를 파괴했다. 그러면 세이브를 불러와
    /// 코드로 구역을 개방할 때는 클릭이 없으니 표지판이 지워지지 않고, 이미 열린 땅
    /// 위에 표지판이 그대로 서 있는다. 만든 쪽이 지우도록 옮겼다.
    /// </summary>
    private void RemoveSign(Vector2Int coord)
    {
        if (!unlockSigns.TryGetValue(coord, out var sign)) return;

        if (sign != null) Destroy(sign);
        unlockSigns.Remove(coord);
    }

    // 이 청크가 개방된 땅인지. chunkStates가 private이라 밖에서 물어볼 방법이 없었다.
    // 건물/가구 설치 판정에서 "해금 안 된 땅에는 설치 불가"를 검사하는 데 쓴다.
    public bool IsUnlocked(Vector2Int coord)
    {
        return chunkStates.TryGetValue(coord, out var state) && state == ChunkState.Unlocked;
    }

    public void UnlockChunk(Vector2Int coord, bool isFree = false)
    {
        if (chunkStates.ContainsKey(coord) && chunkStates[coord] == ChunkState.Unlocked) return;

        if (!isFree)
        {
            GameLog.Log($" {coord} 구역을 {baseUnlockCost} 골드에 개방했습니다!");
        }

        chunkStates[coord] = ChunkState.Unlocked;
        RemoveSign(coord);
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
        unlockSigns[coord] = sign;

        UnlockSign signScript = sign.GetComponent<UnlockSign>();
        if (signScript != null)
        {
            signScript.Setup(this, coord, baseUnlockCost);
        }
    }
}
