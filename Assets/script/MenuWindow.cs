using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 아이템 / 제작 / 스탯 ... 을 탭으로 묶은 하나의 창.
//
// "창이 열려 있는가"와 "어느 탭인가"를 이 컴포넌트만 소유한다. 예전엔 Inventory와 CraftingUI가
// 각자 열림 상태를 들고 있어서 두 가지 문제가 있었다.
//   - I와 E를 둘 다 누르면 인벤토리와 제작창이 동시에 열려 겹쳤다 (서로를 몰랐으므로)
//   - 퀵슬롯 숨김이 Inventory.ToggleUI에만 있어서, 제작창을 열면 퀵슬롯이 그대로 남았다
// 이제 패널들은 "보여지는 콘텐츠"일 뿐이고 스스로 열림 여부를 판단하지 않는다.
//
// 이 컴포넌트는 항상 켜져 있는 오브젝트에 붙고, 실제로 보이고 숨는 것은 windowRoot다
// (꺼진 오브젝트에 붙어 있으면 다시 열 수 없고 FindFirstObjectByType으로도 찾히지 않는다).
public class MenuWindow : MonoBehaviour
{
    [Serializable]
    public class Tab
    {
        public string label = "Tab";
        [Tooltip("이 탭을 고르면 켜지는 패널. 씬 전환 탭이면 비워둔다.")]
        public GameObject content;
        public Button button;
        [Tooltip("비어 있지 않으면 콘텐츠를 켜는 대신 이 씬으로 전환한다 (스킬트리처럼 별도 씬인 경우).")]
        public string sceneToLoad;
    }

    [Header("구성")]
    [Tooltip("탭 바 + 콘텐츠를 담은 창 본체. 이것만 켜고 끈다.")]
    public GameObject windowRoot;
    public List<Tab> tabs = new List<Tab>();

    [Header("창이 열려 있을 때 숨길 것")]
    [Tooltip("하단 퀵슬롯. 인벤토리 전용이었던 숨김 처리를 창 전체로 올렸다.")]
    public GameObject hudQuickSlotUI;

    [Header("탭 버튼 색")]
    public Color activeTabColor = new Color(0.95f, 0.93f, 0.86f);
    public Color inactiveTabColor = new Color(0.45f, 0.43f, 0.40f);

    // 씬에 하나만 있는 창. "지금 입력을 가로채는 UI가 열려 있나"를 퀵슬롯 같은 다른 쪽에서도
    // 물어보므로, 그 답을 여기 한 곳에서만 준다 (참조를 들고 다닐 필요가 없어진다).
    public static MenuWindow Instance { get; private set; }

    /// <summary>입력을 가로채는 창이 열려 있는지. 창이 없는 씬에서는 false.</summary>
    public static bool IsAnyOpen => Instance != null && Instance.IsOpen;

    public bool IsOpen { get; private set; }
    public int CurrentTab { get; private set; }

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i; // 람다가 루프 변수를 잡으면 전부 마지막 값이 되므로 복사해서 넘긴다
            var button = tabs[i].button;
            if (button != null) button.onClick.AddListener(() => SelectTab(index));
        }

        ApplyState(); // 닫힌 상태로 시작
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ───────────────────────── 열기 / 닫기 ─────────────────────────

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        IsOpen = true;
        ApplyState();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        ApplyState();
    }

    // ───────────────────────── 탭 ─────────────────────────

    /// <summary>
    /// 다음 탭으로 순환. 씬 전환 탭은 건너뛴다 (Tab 키로 지나가다 씬이 바뀌면 사고가 된다).
    /// </summary>
    public void NextTab()
    {
        if (!IsOpen || tabs.Count == 0) return;

        for (int step = 1; step <= tabs.Count; step++)
        {
            int next = (CurrentTab + step) % tabs.Count;
            if (string.IsNullOrEmpty(tabs[next].sceneToLoad))
            {
                SelectTab(next);
                return;
            }
        }
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;

        var tab = tabs[index];

        // 씬 전환 탭은 현재 탭을 바꾸지 않는다. 씬에서 돌아왔을 때 원래 보던 탭이 남아 있어야 한다.
        if (!string.IsNullOrEmpty(tab.sceneToLoad))
        {
            LoadScene(tab.sceneToLoad);
            return;
        }

        CurrentTab = index;
        IsOpen = true; // 버튼을 눌렀다는 건 창이 열려 있다는 뜻
        ApplyState();
    }

    // 창/탭 상태를 화면에 반영하는 유일한 지점. 상태를 바꾸는 모든 경로가 여기로 모인다.
    private void ApplyState()
    {
        if (windowRoot != null) windowRoot.SetActive(IsOpen);

        for (int i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            bool selected = IsOpen && i == CurrentTab;

            if (tab.content != null)
            {
                tab.content.SetActive(selected);
                if (selected) RebuildLayout(tab.content);
            }

            // 씬 전환 탭은 선택 상태가 없으므로 항상 비활성 색으로 둔다.
            if (tab.button != null && tab.button.targetGraphic != null)
            {
                bool highlight = selected && string.IsNullOrEmpty(tab.sceneToLoad);
                tab.button.targetGraphic.color = highlight ? activeTabColor : inactiveTabColor;
            }
        }

        if (hudQuickSlotUI != null) hudQuickSlotUI.SetActive(!IsOpen);
    }

    /// <summary>
    /// 방금 켠 탭 내용의 레이아웃을 즉시 다시 계산한다.
    ///
    /// 레이아웃 그룹은 오브젝트가 꺼져 있는 동안 크기를 갱신하지 못한다. 탭 내용은 창이 닫힌
    /// 상태로 시작하므로 "한 번도 제대로 계산된 적 없는" 크기를 들고 켜지고, 중첩이 깊으면
    /// (제작창: ScrollRect > ContentSizeFitter > Vertical > Horizontal) 켜지는 프레임에
    /// 한 번에 풀리지 않는다. 그 결과 제목이 좁은 폭 기준으로 줄바꿈된 채 굳거나 닫기 버튼이
    /// 패널 밖으로 밀려난다.
    ///
    /// 절대 좌표로 짠 패널(인벤토리)은 레이아웃 그룹이 없어서 이 문제가 없었다 - 그래서
    /// 같은 창인데 제작 탭만 깨져 보였다.
    /// </summary>
    private static void RebuildLayout(GameObject content)
    {
        var rect = content.transform as RectTransform;
        if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void LoadScene(string sceneName)
    {
        // Build Settings에 등록되지 않은 씬을 로드하면 런타임 에러가 난다. 미등록이면 안내만 하고 넘어간다.
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[MenuWindow] '{sceneName}' 씬이 Build Settings에 등록되지 않아 전환할 수 없습니다. " +
                             "File > Build Profiles(또는 Build Settings)에서 추가하세요.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
