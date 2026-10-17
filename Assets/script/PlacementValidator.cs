using UnityEngine;
using UnityEngine.Tilemaps;

// "이 자리에 설치할 수 있는가"의 단일 소유자. 가구와 건설 설계도가 같은 판정을 공유한다.
// (각자 판정하면 규칙이 두 벌로 갈라져서, 한쪽만 고치고 다른 쪽을 놓치게 된다.)
//
// bool 하나가 아니라 칸별 결과를 돌려주는 이유: 2x2 이상이 되면 "전체 빨강"만 보여서는
// 어느 칸이 왜 막혔는지 알 수 없다. 미리보기가 칸별로 색을 칠할 수 있어야 한다.
public enum CellPlacement
{
    Ok,
    NoGround,     // 땅 타일이 없음 (맵 경계 밖)
    Water,        // 물
    Farm,         // 밭
    Occupied,     // 다른 오브젝트가 점유
    LockedChunk   // 해금되지 않은 청크
}

public class PlacementValidator : MonoBehaviour
{
    [Header("참조 타일맵")]
    [Tooltip("지형(땅) 타일맵. 칸 좌표 변환과 '땅이 있는지' 검사에 쓴다.")]
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;

    [Header("참조 매니저")]
    public landtiles landTileManager;
    public MapUnlockManager mapUnlockManager;

    [Header("규칙")]
    [Tooltip("밭 위에도 설치를 허용할지. 지금은 밭이 지형 타일맵을 덮어써서 되돌릴 수 없으므로 금지한다.")]
    public bool blockOnFarmTile = true;

    // 판정 결과. cells는 footprint를 칸 단위로 훑은 결과이며 인덱스는 (y * sizeX + x) 순서다.
    public struct Result
    {
        public bool canPlace;
        public CellPlacement[] cells;
        public Vector2Int size;      // 칸 개수
        public Vector2 snappedCenter; // 격자에 맞춘 실제 설치 중심
        public string reason;         // 거부 사유 (UI 메시지용)
    }

    // footprint(월드 유닛)를 칸 개수로 바꾼다. 셀 크기가 1이 아닐 수 있으므로 타일맵 기준으로 계산.
    public Vector2Int ToCellCount(Vector2 footprint)
    {
        Vector3 cell = groundTilemap != null ? groundTilemap.cellSize : Vector3.one;
        return new Vector2Int(
            Mathf.Max(1, Mathf.RoundToInt(footprint.x / Mathf.Max(0.0001f, cell.x))),
            Mathf.Max(1, Mathf.RoundToInt(footprint.y / Mathf.Max(0.0001f, cell.y))));
    }

    /// <summary>
    /// 마우스 월드 좌표를 격자에 맞춘 뒤, 그 자리에 size 칸만큼 설치 가능한지 칸별로 검사한다.
    /// </summary>
    /// <param name="origin">판정 주체(플레이어) 위치 - 사거리 검사 기준</param>
    public Result Validate(Vector2 worldPos, Vector2Int size, Vector2 footprint, Vector2 origin, float range)
    {
        var result = new Result
        {
            size = size,
            cells = new CellPlacement[Mathf.Max(1, size.x * size.y)],
            canPlace = true
        };

        if (groundTilemap == null)
        {
            result.canPlace = false;
            result.reason = "지형 타일맵이 연결되지 않았습니다.";
            return result;
        }

        // 1. 격자 스냅 - 칸 수가 짝수면 칸 경계에, 홀수면 칸 중심에 맞춰야 깔끔하게 떨어진다.
        Vector3Int baseCell = groundTilemap.WorldToCell(worldPos);
        Vector3 cellSize = groundTilemap.cellSize;
        Vector3Int minCell = new Vector3Int(
            baseCell.x - (size.x - 1) / 2,
            baseCell.y - (size.y - 1) / 2,
            0);
        Vector3 minCenter = groundTilemap.GetCellCenterWorld(minCell);
        result.snappedCenter = new Vector2(
            minCenter.x + (size.x - 1) * 0.5f * cellSize.x,
            minCenter.y + (size.y - 1) * 0.5f * cellSize.y);

        // 2. 사거리 - 스냅된 중심 기준으로 재야 미리보기와 실제 설치 판정이 어긋나지 않는다.
        if (Vector2.Distance(origin, result.snappedCenter) > range)
        {
            result.canPlace = false;
            result.reason = "너무 멉니다.";
            for (int i = 0; i < result.cells.Length; i++) result.cells[i] = CellPlacement.Ok;
            return result;
        }

        // 3. 칸별 검사
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                Vector3Int cell = new Vector3Int(minCell.x + x, minCell.y + y, 0);
                CellPlacement status = CheckCell(cell);
                result.cells[y * size.x + x] = status;
                if (status != CellPlacement.Ok)
                {
                    result.canPlace = false;
                    if (string.IsNullOrEmpty(result.reason)) result.reason = ReasonText(status);
                }
            }
        }

        // 4. 겹침은 칸이 아니라 실제 콜라이더로 봐야 하므로 footprint 전체를 한 번에 검사한다.
        if (result.canPlace && IsBlockedByCollider(result.snappedCenter, footprint))
        {
            result.canPlace = false;
            result.reason = "다른 물건이 자리를 차지하고 있습니다.";
            for (int i = 0; i < result.cells.Length; i++)
                if (result.cells[i] == CellPlacement.Ok) result.cells[i] = CellPlacement.Occupied;
        }

        return result;
    }

    private CellPlacement CheckCell(Vector3Int cell)
    {
        // 해금되지 않은 청크 - 아직 열지 않은 땅에는 설치할 수 없다.
        if (mapUnlockManager != null)
        {
            Vector3 cellWorld = groundTilemap.GetCellCenterWorld(cell);
            var mapGen = mapUnlockManager.mapGenerator;
            if (mapGen != null && !mapUnlockManager.IsUnlocked(mapGen.WorldToChunk(cellWorld)))
                return CellPlacement.LockedChunk;
        }

        // 땅이 있는지 - 예전엔 이 검사가 없어서 맵 경계 밖 허공에도 설치할 수 있었다.
        if (!groundTilemap.HasTile(cell)) return CellPlacement.NoGround;

        if (waterTilemap != null && waterTilemap.HasTile(cell)) return CellPlacement.Water;

        if (blockOnFarmTile && landTileManager != null && landTileManager.HasFarmTile(cell))
            return CellPlacement.Farm;

        return CellPlacement.Ok;
    }

    // 플레이어 자신도 겹침 대상에 포함한다(제 자리에 설치해서 갇히는 것을 막기 위함).
    private bool IsBlockedByCollider(Vector2 center, Vector2 footprint)
    {
        foreach (var hit in Physics2D.OverlapBoxAll(center, footprint, 0f))
        {
            if (hit == null || hit.isTrigger) continue;
            // 땅/물 타일맵 콜라이더는 바닥이므로 겹쳐도 설치를 막지 않는다(물은 칸 검사에서 따로 본다).
            if (hit.GetComponentInParent<Tilemap>() != null) continue;
            return true;
        }
        return false;
    }

    private static string ReasonText(CellPlacement status)
    {
        switch (status)
        {
            case CellPlacement.NoGround: return "땅이 없는 곳에는 설치할 수 없습니다.";
            case CellPlacement.Water: return "물 위에는 설치할 수 없습니다.";
            case CellPlacement.Farm: return "밭 위에는 설치할 수 없습니다.";
            case CellPlacement.LockedChunk: return "아직 개방되지 않은 땅입니다.";
            case CellPlacement.Occupied: return "다른 물건이 자리를 차지하고 있습니다.";
            default: return "";
        }
    }
}
