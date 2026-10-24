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

    // 포인트는 PlayerStats가 소유한다. 여기서 따로 들고 있지 않는다.
    private PlayerStats Stats => PlayerStats.Instance;

    // 구독해 둔 대상을 쥐고 그걸로 해제한다. Instance로 해제하면 그 사이에 바뀌었을 때
    // 엉뚱한 대상에서 빼게 된다 (InventoryUI/QuickSlotBarUI와 같은 방침).
    private PlayerStats subscribed;

    // 패널이 꺼진 채로 시작하므로 Start가 아니라 처음 열릴 때 만든다.
    private void OnEnable()
    {
        if (!generated)
        {
            generated = true;
            skillGrid = new SkillNode[gridWidth, gridHeight];
            GenerateGrid();
            FitContainer();
        }

        subscribed = Stats;
        if (subscribed != null)
        {
            subscribed.OnStatsChanged -= RefreshPoints;
            subscribed.OnStatsChanged += RefreshPoints;
        }

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
            Debug.LogWarning("[SkillTreeManager] skillDatabase가 연결되지 않았습니다.");
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
            Debug.LogWarning($"[SkillTreeManager] 그리드 범위를 벗어난 좌표 ({x},{y}) - '{data?.displayName}'를 건너뜁니다.");
            return;
        }
        if (skillGrid[x, y] != null)
        {
            Debug.LogWarning($"[SkillTreeManager] 좌표 ({x},{y})에 이미 노드가 있습니다 - '{data?.displayName}'를 건너뜁니다.");
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
            Debug.LogWarning("[SkillTree] PlayerStats를 찾을 수 없어 스킬을 해금할 수 없습니다.");
            return;
        }

        // 조용히 아무것도 안 하면 "눌렀는데 왜 안 되지"가 된다. 이유를 남긴다.
        if (!stats.SpendSkillPoint())
        {
            Debug.Log("[SkillTree] 스킬 포인트가 없습니다. 레벨을 올리면 레벨당 1점씩 받습니다.");
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
            Debug.Log($"[SkillTree] '{node.Data.displayName}' 스킬 언락됨");

        ActivateNeighbors(x, y);
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
