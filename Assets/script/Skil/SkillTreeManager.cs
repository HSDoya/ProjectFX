using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Transform gridContainer;

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;

    private SkillNode[,] skillGrid;

    // 전체 트리를 다 열어볼 수 있는 테스트용 포인트 (기본값 = gridWidth * gridHeight).
    // 그리드 크기를 바꾸면 이 값도 노드 총 개수에 맞춰 함께 조정해줘야 전체 트리를 다 열 수 있다.
    public int skillPoints = 64;

    void Start()
    {
        skillGrid = new SkillNode[gridWidth, gridHeight];
        GenerateGrid();
    }

    // gridWidth x gridHeight 그리드 생성
    void GenerateGrid()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                GameObject go = Instantiate(nodePrefab, gridContainer);
                SkillNode node = go.GetComponent<SkillNode>();
                node.Initialize(x, y, this);
                skillGrid[x, y] = node;
            }
        }

        // 중앙에 가장 가까운 시작 노드를 해금 상태로 시작
        UnlockNodeData(gridWidth / 2, gridHeight / 2);
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
        skillGrid[x, y].SetState(NodeState.Unlocked);
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

            // 그리드 범위를 벗어나지 않고, 잠겨있는 노드만 해금 가능 상태로 전환
            if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
            {
                if (skillGrid[nx, ny].State == NodeState.Locked)
                {
                    skillGrid[nx, ny].SetState(NodeState.ReadyToUnlock);
                }
            }
        }
    }

    // 이미지 구역에 따른 색상 반환 함수 (UI 연출용, 실제 스킬 데이터가 들어오기 전까지의 임시 표시)
    public Color GetZoneColor(int x, int y)
    {
        int startX = gridWidth / 2;
        int startY = gridHeight / 2;

        if (x == startX && y == startY) return Color.green; // START
        if (y < gridHeight / 3) return Color.blue;             // 상단 마법
        if (y >= gridHeight - gridHeight / 3) return Color.red; // 하단 물리
        if (x < gridWidth / 3) return Color.magenta;           // 좌측 유틸
        return new Color(1f, 0.6f, 0f);                        // 우측 방어 (오렌지)
    }
}
