using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 스킬 트리. 탭 창(MenuWindow)의 Skills 탭 안에서 동작한다.
//
// 예전에는 별도 씬(SkillTree.unity)이었다. 그런데 그 씬으로 가려면 SceneManager.LoadScene을
// 단일 모드로 불러야 해서 Main이 통째로 언로드됐다 - 세이브가 없으니 인벤토리/레벨/건설이
// 전부 날아가고, 돌아올 수단도 없었다. 지금은 씬 전환 없이 패널로 떠서 그 문제가 사라졌고,
// 같은 씬에 있으니 PlayerStats의 스킬 포인트를 바로 쓸 수 있다.
public class SkillTreeManager : MonoBehaviour
{
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private SkillDatabaseSO skillDatabase;
    [Tooltip("남은 스킬 포인트 표시. 없으면 표시만 생략된다.")]
    [SerializeField] private TextMeshProUGUI pointsText;

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private float cellPitch = 90f; // 칸 크기(80) + 여백(10). 좌표 -> 픽셀 위치 변환에 사용
    [SerializeField] private float nodeSize = 80f;

    private SkillNode[,] skillGrid;
    private bool generated;

    // 해금한 스킬들이 주는 스탯 보너스의 합계. 해금 상태의 소유자가 여기이므로 합계도 여기서 센다.
    // PlayerStats는 이 값을 통보받아 계산에만 쓴다(SetSkillBonuses).
    private int[] grantedBonus;

    // 세이브에서 읽었지만 아직 트리가 만들어지지 않아 적용하지 못한 해금 목록.
    // 트리는 패널을 처음 열 때 생성되므로, 한 번도 열지 않은 상태에서 불러오면 여기 담아뒀다가
    // 생성 직후에 적용한다.
    private string[] pendingUnlocks;

    private static readonly int StatCount = System.Enum.GetValues(typeof(StatType)).Length;

    // 포인트는 PlayerStats가 소유한다. 여기서 따로 들고 있지 않는다.
    private PlayerStats Stats => PlayerStats.Instance;

    // 구독해 둔 대상을 쥐고 그걸로 해제한다. Instance로 해제하면 그 사이에 바뀌었을 때
    // 엉뚱한 대상에서 빼게 된다 (InventoryUI/QuickSlotBarUI와 같은 방침).
    private PlayerStats subscribed;

    // 패널이 꺼진 채로 시작하므로 Start가 아니라 처음 열릴 때 만든다.
    private void OnEnable()
    {
        if (grantedBonus == null) grantedBonus = new int[StatCount];

        if (!generated)
        {
            generated = true;
            skillGrid = new SkillNode[gridWidth, gridHeight];
            GenerateGrid();
            FitContainer();

            // 트리가 생기기 전에 불러온 세이브가 있으면 지금 반영한다.
            if (pendingUnlocks != null) ApplyUnlocked(pendingUnlocks);
        }

        subscribed = Stats;
        if (subscribed != null)
        {
            subscribed.OnStatsChanged -= RefreshPoints;
            subscribed.OnStatsChanged += RefreshPoints;
        }

        // 해금 효과를 다시 통보한다. PlayerStats가 트리보다 늦게 깨어났어도 여기서 따라잡는다.
        // 더하기가 아니라 덮어쓰기라서 패널을 몇 번 여닫아도 결과가 같다.
        PushSkillBonuses();

        RefreshPoints();
    }

    private void OnDisable()
    {
        if (subscribed != null) subscribed.OnStatsChanged -= RefreshPoints;
        subscribed = null;
    }

    private void RefreshPoints()
    {
        if (pointsText == null) return;
        pointsText.text = Stats != null ? $"Skill points : {Stats.availableSkillPoints}" : "Skill points : -";
    }

    // 노드를 다 만든 뒤 실제로 쓰인 좌표 범위에 맞춰 컨테이너 크기를 맞춘다.
    // gridWidth/Height(8x8) 기준으로 두면 4x6만 쓰는 지금 트리가 패널 왼쪽 위로 쏠린다.
    private void FitContainer()
    {
        var rt = gridContainer as RectTransform;
        if (rt == null || skillDatabase == null) return;

        int maxX = 0, maxY = 0;
        foreach (var skill in skillDatabase.allSkills)
        {
            if (skill == null) continue;
            if (skill.gridX > maxX) maxX = skill.gridX;
            if (skill.gridY > maxY) maxY = skill.gridY;
        }

        rt.sizeDelta = new Vector2(maxX * cellPitch + nodeSize, maxY * cellPitch + nodeSize);
    }

    void GenerateGrid()
    {
        if (skillDatabase == null)
        {
            GameLog.LogWarning("[SkillTreeManager] skillDatabase가 연결되지 않았습니다.");
            return;
        }

        // 스킬 데이터가 있는 좌표에만 노드 생성 (64칸을 다 채우지 않음)
        foreach (var skill in skillDatabase.allSkills)
        {
            if (skill == null) continue;
            CreateNode(skill.gridX, skill.gridY, skill);
        }

        // 사분면(카테고리)별 시작 스킬을 무료로 해금
        foreach (var skill in skillDatabase.allSkills)
        {
            if (skill != null && skill.unlockedByDefault)
            {
                UnlockNodeData(skill.gridX, skill.gridY);
            }
        }
    }

    private void CreateNode(int x, int y, SkillData data)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
        {
            GameLog.LogWarning($"[SkillTreeManager] 그리드 범위를 벗어난 좌표 ({x},{y}) - '{data?.displayName}'를 건너뜁니다.");
            return;
        }
        if (skillGrid[x, y] != null)
        {
            GameLog.LogWarning($"[SkillTreeManager] 좌표 ({x},{y})에 이미 노드가 있습니다 - '{data?.displayName}'를 건너뜁니다.");
            return;
        }

        GameObject go = Instantiate(nodePrefab, gridContainer);
        SkillNode node = go.GetComponent<SkillNode>();
        node.Initialize(x, y, this, data);
        skillGrid[x, y] = node;

        // GridLayoutGroup이 아니라 좌표를 기준으로 직접 위치를 지정 (왼쪽 위 기준, x는 오른쪽, y는 아래로 증가)
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(nodeSize, nodeSize);
            rt.anchoredPosition = new Vector2(x * cellPitch, -y * cellPitch);
        }
    }

    public void TryUnlockNode(SkillNode node)
    {
        var stats = Stats;
        if (stats == null)
        {
            GameLog.LogWarning("[SkillTree] PlayerStats를 찾을 수 없어 스킬을 해금할 수 없습니다.");
            return;
        }

        // 조용히 아무것도 안 하면 "눌렀는데 왜 안 되지"가 된다. 이유를 남긴다.
        if (!stats.SpendSkillPoint())
        {
            GameLog.Log("[SkillTree] 스킬 포인트가 없습니다. 레벨을 올리면 레벨당 1점씩 받습니다.");
            return;
        }

        UnlockNodeData(node.X, node.Y);
    }

    private void UnlockNodeData(int x, int y)
    {
        var node = skillGrid[x, y];
        if (node == null) return;

        node.SetState(NodeState.Unlocked);

        if (node.Data != null)
        {
            ApplyBonus(node.Data);

            string effect = node.Data.HasBonus
                ? $"  ({node.Data.bonusStat} +{node.Data.bonusAmount})"
                : string.Empty;
            GameLog.Log($"[SkillTree] '{node.Data.displayName}' 스킬 언락됨{effect}");
        }

        ActivateNeighbors(x, y);
    }

    // ───────────────────────── 저장 / 불러오기 ─────────────────────────

    /// <summary>해금된 스킬의 skillID 목록. 좌표가 아니라 ID로 내보내므로 트리 배치를 바꿔도 안전하다.</summary>
    public string[] CaptureUnlocked()
    {
        // 트리를 한 번도 열지 않았다면 불러온 목록을 그대로 다시 내보낸다.
        // 안 그러면 "불러오기 -> 트리 안 열고 저장"에서 해금이 전부 날아간다.
        if (skillGrid == null) return pendingUnlocks ?? new string[0];

        var list = new List<string>();
        foreach (var node in skillGrid)
        {
            if (node == null || node.State != NodeState.Unlocked) continue;
            if (node.Data == null || string.IsNullOrEmpty(node.Data.skillID)) continue;
            list.Add(node.Data.skillID);
        }
        return list.ToArray();
    }

    /// <summary>세이브에서 읽은 해금 목록을 반영한다. 트리가 아직 없으면 생성될 때까지 들고 있는다.</summary>
    public void LoadUnlocked(string[] skillIDs)
    {
        pendingUnlocks = skillIDs ?? new string[0];
        if (generated) ApplyUnlocked(pendingUnlocks);
    }

    /// <summary>
    /// 전부 잠근 뒤 기본 해금 + 저장된 해금을 다시 쌓는다.
    ///
    /// 더하기만 하면 불러올 때마다 보너스가 누적되므로, 합계도 0부터 다시 센다.
    /// </summary>
    private void ApplyUnlocked(string[] skillIDs)
    {
        if (skillGrid == null) return;

        foreach (var node in skillGrid)
            if (node != null) node.SetState(NodeState.Locked);

        grantedBonus = new int[StatCount];

        // 기본 해금은 세이브 내용과 무관하게 항상 열려 있어야 한다.
        if (skillDatabase != null)
        {
            foreach (var skill in skillDatabase.allSkills)
                if (skill != null && skill.unlockedByDefault)
                    UnlockNodeData(skill.gridX, skill.gridY);
        }

        if (skillIDs != null)
        {
            // 같은 ID가 두 번 들어 있으면 보너스가 두 번 더해진다. 한 번만 처리한다.
            var seen = new HashSet<string>();
            foreach (var id in skillIDs)
            {
                if (string.IsNullOrEmpty(id) || !seen.Add(id)) continue;

                SkillNode node = FindNodeById(id);
                if (node == null)
                {
                    GameLog.LogWarning($"[SkillTree] 알 수 없는 스킬 ID '{id}' - 건너뜁니다.");
                    continue;
                }
                if (node.State == NodeState.Unlocked) continue; // 기본 해금과 겹치는 경우

                UnlockNodeData(node.X, node.Y);
            }
        }

        PushSkillBonuses();
        RefreshPoints();
    }

    private SkillNode FindNodeById(string skillID)
    {
        if (skillGrid == null || string.IsNullOrEmpty(skillID)) return null;

        foreach (var node in skillGrid)
            if (node != null && node.Data != null && node.Data.skillID == skillID)
                return node;

        return null;
    }

    // 해금된 스킬 하나의 보너스를 합계에 더하고 PlayerStats에 통보한다.
    private void ApplyBonus(SkillData data)
    {
        if (data == null || !data.HasBonus) return;

        if (grantedBonus == null) grantedBonus = new int[StatCount];

        int i = (int)data.bonusStat;
        if (i < 0 || i >= grantedBonus.Length) return;

        grantedBonus[i] += data.bonusAmount;
        PushSkillBonuses();
    }

    // 합계를 통째로 넘긴다. PlayerStats 쪽이 덮어쓰기라 중복 호출이 안전하다.
    private void PushSkillBonuses()
    {
        var stats = Stats;
        if (stats != null) stats.SetSkillBonuses(grantedBonus);
    }

    // 상하좌우 인접 노드 활성화 로직
    private void ActivateNeighbors(int x, int y)
    {
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];

            // 그리드 범위를 벗어나지 않고, 실제 노드가 존재하며, 잠겨있는 노드만 해금 가능 상태로 전환
            if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
            {
                var neighbor = skillGrid[nx, ny];
                if (neighbor != null && neighbor.State == NodeState.Locked)
                {
                    neighbor.SetState(NodeState.ReadyToUnlock);
                }
            }
        }
    }

    // 스킬 카테고리에 따른 색상 반환 (UI 연출용)
    public Color GetNodeColor(SkillData data)
    {
        if (data == null) return Color.white;

        switch (data.category)
        {
            case SkillCategory.Experience: return Color.blue;
            case SkillCategory.Combat: return Color.red;
            case SkillCategory.Farming: return new Color(0.4f, 0.8f, 0.2f);
            case SkillCategory.Development: return new Color(1f, 0.6f, 0f);
            default: return Color.white;
        }
    }
}
