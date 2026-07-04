using UnityEngine;
using UnityEngine.UI;

public enum NodeState { Locked, ReadyToUnlock, Unlocked }

public class SkillNode : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public NodeState State { get; private set; }

    [SerializeField] private Image nodeImage;
    [SerializeField] private Button nodeButton;

    private SkillTreeManager manager;

    // 노드 초기화
    public void Initialize(int x, int y, SkillTreeManager treeManager)
    {
        X = x;
        Y = y;
        manager = treeManager;
        nodeButton.onClick.AddListener(OnNodeClicked);
        SetState(NodeState.Locked);
    }

    // 상태 변경 및 UI 업데이트
    public void SetState(NodeState newState)
    {
        State = newState;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        switch (State)
        {
            case NodeState.Locked:
                nodeImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 어두운 반투명
                nodeButton.interactable = false;
                break;
            case NodeState.ReadyToUnlock:
                nodeImage.color = new Color(1f, 1f, 1f, 0.8f); // 밝음 (클릭 가능)
                nodeButton.interactable = true;
                break;
            case NodeState.Unlocked:
                nodeImage.color = manager.GetZoneColor(X, Y); // 구역 고유 색상
                nodeButton.interactable = false;
                break;
        }
    }

    private void OnNodeClicked()
    {
        if (State == NodeState.ReadyToUnlock)
        {
            manager.TryUnlockNode(this);
        }
    }
}