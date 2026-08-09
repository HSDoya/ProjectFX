using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private SkillDatabaseSO skillDatabase;

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private float cellPitch = 90f; // 칸 크기(80) + 여백(10). 좌표 -> 픽셀 위치 변환에 사용

    private SkillNode[,] skillGrid;

    // 사분면 시작 스킬 4개는 무료로 자동 해금되므로, 나머지(20 - 4 = 16)를 여는 데 필요한 포인트.
    // skillDatabase의 스킬 수/시작 스킬 수가 바뀌면 이 값도 함께 조정해야 전체 트리를 다 열 수 있다.
    public int skillPoints = 16;

    void Start()
    {
        skillGrid = new SkillNode[gridWidth, gridHeight];
        GenerateGrid();
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
            rt.sizeDelta = new Vector2(80, 80);
            rt.anchoredPosition = new Vector2(x * cellPitch, -y * cellPitch);
        }
    }

    public void TryUnlockNode(SkillNode node)
    {
        if (skillPoints > 0)
        {
            skillPoints--;
            UnlockNodeData(node.X, node.Y);
        }
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
