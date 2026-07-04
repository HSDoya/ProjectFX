using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Transform gridContainer;

    private SkillNode[,] skillGrid = new SkillNode[7, 7];
    public int skillPoints = 5; // 테스트용 스킬 포인트

    void Start()
    {
        GenerateGrid();
    }

    // 7x7 그리드 생성
    void GenerateGrid()
    {
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                GameObject go = Instantiate(nodePrefab, gridContainer);
                SkillNode node = go.GetComponent<SkillNode>();
                node.Initialize(x, y, this);
                skillGrid[x, y] = node;
            }
        }

        // 중앙 (3,3) 시작 노드 해금 상태로 시작
        UnlockNodeData(3, 3);
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
            if (nx >= 0 && nx < 7 && ny >= 0 && ny < 7)
            {
                if (skillGrid[nx, ny].State == NodeState.Locked)
                {
                    skillGrid[nx, ny].SetState(NodeState.ReadyToUnlock);
                }
            }
        }
    }

    // 이미지 구역에 따른 색상 반환 함수 (UI 연출용)
    public Color GetZoneColor(int x, int y)
    {
        if (x == 3 && y == 3) return Color.green; // START
        if (y < 2) return Color.blue;             // 상단 마법
        if (y > 4) return Color.red;              // 하단 물리
        if (x < 2) return Color.magenta;          // 좌측 유틸
        return new Color(1f, 0.6f, 0f);           // 우측 방어 (오렌지)
    }
}