using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 가까운 건설 현장의 필요 재료와 투입 현황을 보여주는 패널.
//
// 역할을 좁게 둔다: "어느 현장을 보여줄지"는 BuildingSite.FindNearest가 정하고(= R키와 같은 대상),
// 이 컴포넌트는 받은 현장을 그리기만 한다. 투입량을 따로 들고 있지 않고 매번 현장에서 읽으므로
// 실제 재료와 화면 숫자가 어긋날 수 없다.
//
// 재료 한 줄은 제작창의 IngredientRowUI를 그대로 쓴다(아이콘 + 이름 + "보유/필요" 색깔 숫자).
// 모양이 똑같은 줄을 새로 만들면 같은 UI가 두 벌이 된다.
public class BuildingSiteUI : MonoBehaviour
{
    [Header("구성")]
    [Tooltip("현장이 가까이 있을 때만 켜지는 패널 본체")]
    public GameObject contentRoot;
    public TextMeshProUGUI titleText;
    [Tooltip("재료 줄이 쌓이는 부모")]
    public RectTransform rowContainer;
    public IngredientRowUI rowPrefab;
    public TextMeshProUGUI footerText;

    [Header("문구")]
    // TMP 기본 폰트에 한글 글리프가 없어서 영문으로 둔다. 한글 폰트를 넣으면 인스펙터에서 바꾸면 된다.
    public string depositHint = "[R] Deposit materials";
    public string readyText = "Ready to build";

    private BuildingSite target;
    private readonly List<IngredientRowUI> rows = new List<IngredientRowUI>();
    private readonly List<ItemData> rowItems = new List<ItemData>();
    private readonly List<int> shownDeposited = new List<int>();

    private void Awake()
    {
        // 패널 본체만 꺼둔다. 이 컴포넌트가 붙은 오브젝트는 켜진 상태로 남아야
        // PlayerMove가 FindFirstObjectByType으로 찾을 수 있다(비활성 오브젝트는 검색되지 않는다).
        if (contentRoot != null) contentRoot.SetActive(false);
    }

    /// <summary>
    /// 보여줄 건설 현장을 지정한다. null이면 패널을 숨긴다. 매 프레임 호출해도 된다.
    /// </summary>
    public void Show(BuildingSite site)
    {
        if (site == null)
        {
            Hide();
            return;
        }

        if (site != target)
        {
            target = site;
            Rebuild();
        }

        RefreshCounts();
    }

    // 파괴된 오브젝트는 유니티에서 null과 같다고 비교되므로(F로 현장을 회수한 직후가 그렇다)
    // target만 보고 판단하면 패널이 옛 숫자를 띄운 채 남는다. 그려둔 줄이 있는지도 함께 본다.
    private void Hide()
    {
        if (target == null && rows.Count == 0) return;

        target = null;
        ClearRows();
        if (contentRoot != null) contentRoot.SetActive(false);
    }

    private void ClearRows()
    {
        foreach (var row in rows)
            if (row != null) Destroy(row.gameObject);
        rows.Clear();
        rowItems.Clear();
        shownDeposited.Clear();
    }

    // 대상이 바뀔 때만 줄을 다시 만든다. 매 프레임 Instantiate/Destroy하면 쓸데없이 무겁다.
    private void Rebuild()
    {
        ClearRows();

        if (contentRoot != null) contentRoot.SetActive(true);
        if (titleText != null) titleText.text = target.UiLabel;

        if (rowPrefab == null || rowContainer == null)
        {
            // 제너레이터를 아직 돌리지 않은 상태. 제목/문구만이라도 뜨게 하고 조용히 넘어간다.
            Debug.LogWarning("[BuildingSiteUI] 재료 줄 프리팹이나 부모가 비어 있습니다. Tools/건설 UI 테스트용 생성을 실행하세요.");
            return;
        }

        foreach (var req in target.requirements)
        {
            if (req == null || string.IsNullOrEmpty(req.itemID)) continue;

            // itemID -> ItemData 변환은 ItemDataManager가 소유한다(BuildingSite.RefundDeposited와 같은 경로).
            ItemData data = ItemDataManager.instance != null
                ? ItemDataManager.instance.GetItemDataByID(req.itemID.Trim())
                : null;

            var row = Instantiate(rowPrefab, rowContainer);
            rows.Add(row);
            rowItems.Add(data);
            shownDeposited.Add(-1); // 첫 갱신이 반드시 일어나게
        }
    }

    // 투입량이 바뀐 줄만 다시 그린다.
    private void RefreshCounts()
    {
        int i = 0;
        foreach (var req in target.requirements)
        {
            if (req == null || string.IsNullOrEmpty(req.itemID)) continue;
            if (i >= rows.Count) break;

            if (shownDeposited[i] != req.deposited)
            {
                rows[i].Setup(rowItems[i], req.deposited, req.amount);
                shownDeposited[i] = req.deposited;
            }
            i++;
        }

        if (footerText != null)
            footerText.text = target.IsSatisfied() ? readyText : depositHint;
    }
}
