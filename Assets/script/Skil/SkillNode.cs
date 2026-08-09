using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum NodeState { Locked, ReadyToUnlock, Unlocked }

public class SkillNode : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public NodeState State { get; private set; }
    public SkillData Data { get; private set; }

    [SerializeField] private Image nodeImage;
    [SerializeField] private Button nodeButton;
    // 프리팹에 아직 이름 표시용 텍스트 자식이 없다면 비워둬도 정상 동작함(이름만 안 보임)
    [SerializeField] private TextMeshProUGUI nameLabel;

    private SkillTreeManager manager;

    // 노드 초기화 (data가 null이면 스킬 없는 허브/시작 노드로 취급)
    public void Initialize(int x, int y, SkillTreeManager treeManager, SkillData data)
    {
        X = x;
        Y = y;
        manager = treeManager;
        Data = data;
        nodeButton.onClick.AddListener(OnNodeClicked);

        if (nameLabel != null)
            nameLabel.text = data != null ? data.displayName : "";

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
                nodeImage.color = manager.GetNodeColor(Data); // 카테고리 색상
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
