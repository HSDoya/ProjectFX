using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 안 저장/불러오기 탭. 탭 창(MenuWindow)의 콘텐츠 중 하나다.
///
/// 에디터 메뉴(Tools/세이브)는 플레이 중에만 쓸 수 있고 빌드에는 들어가지 않는다.
/// 플레이어가 쓰는 입구가 필요해서 탭으로 올렸다.
///
/// 되돌릴 수 없는 동작(덮어쓰기, 불러오기, 삭제)은 전부 확인을 한 번 받는다.
/// 버튼이 한 줄에 나란히 있어서 잘못 누르기 쉽다.
///
/// UI 글자는 영문이다. TMP 기본 폰트(LiberationSans)에 한글 글리프가 없어서
/// 한글을 넣으면 네모로 깨진다. 한글 폰트를 넣으면 이 파일의 문자열만 바꾸면 된다.
/// </summary>
public class SaveLoadPanelUI : MonoBehaviour
{
    [Serializable]
    public class SlotRow
    {
        public int slot;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI infoText;
        public Button saveButton;
        public Button loadButton;
        public Button deleteButton;
    }

    [Header("슬롯")]
    public List<SlotRow> rows = new List<SlotRow>();

    [Header("확인 창")]
    [Tooltip("확인을 받을 때만 켜지는 덮개. 패널 전체를 덮어 뒤쪽 버튼 클릭을 막는다.")]
    public GameObject confirmRoot;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("알림")]
    public TextMeshProUGUI statusText;
    [Tooltip("알림 문구가 남아 있는 시간(초). 0이면 지우지 않는다.")]
    public float statusSeconds = 4f;

    // 확인 창에서 "Yes"를 눌렀을 때 실행할 동작.
    private Action pendingAction;
    private Coroutine statusRoutine;

    private void Awake()
    {
        foreach (var row in rows)
        {
            if (row == null) continue;

            // 람다가 루프 변수를 잡으면 전부 마지막 값이 되므로 복사해서 넘긴다.
            int slot = row.slot;

            if (row.saveButton != null) row.saveButton.onClick.AddListener(() => OnSaveClicked(slot));
            if (row.loadButton != null) row.loadButton.onClick.AddListener(() => OnLoadClicked(slot));
            if (row.deleteButton != null) row.deleteButton.onClick.AddListener(() => OnDeleteClicked(slot));
        }

        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CloseConfirm);

        CloseConfirm();
    }

    // 탭을 열 때마다 파일 상태를 다시 읽는다. 다른 슬롯이 그 사이 지워졌을 수도 있다.
    private void OnEnable()
    {
        CloseConfirm();
        SetStatus(string.Empty);
        Refresh();
    }

    private void OnDisable()
    {
        if (statusRoutine != null)
        {
            StopCoroutine(statusRoutine);
            statusRoutine = null;
        }
    }

    // ───────────────────────── 목록 갱신 ─────────────────────────

    public void Refresh()
    {
        foreach (var row in rows)
        {
            if (row == null) continue;

            SaveManager.SaveSlotInfo info = SaveManager.PeekInfo(row.slot);

            if (row.titleText != null) row.titleText.text = $"Slot {row.slot + 1}";
            if (row.infoText != null) row.infoText.text = Describe(info);

            // 빈 슬롯은 불러올 것도 지울 것도 없다.
            if (row.loadButton != null) row.loadButton.interactable = info.exists && !info.broken;
            if (row.deleteButton != null) row.deleteButton.interactable = info.exists;
        }
    }

    private static string Describe(SaveManager.SaveSlotInfo info)
    {
        if (!info.exists) return "Empty";
        if (info.broken) return "Damaged file";

        string when = info.hasTime ? info.savedAtLocal.ToString("yyyy-MM-dd HH:mm") : "unknown time";
        return $"Lv.{info.level}      {when}      v{info.version}";
    }

    // ───────────────────────── 버튼 ─────────────────────────

    private void OnSaveClicked(int slot)
    {
        // 빈 슬롯에 저장하는 것은 잃을 것이 없으므로 바로 진행한다.
        if (SaveManager.Exists(slot))
            Ask($"Overwrite slot {slot + 1}?", () => DoSave(slot));
        else
            DoSave(slot);
    }

    private void DoSave(int slot)
    {
        bool ok = SaveManager.Save(slot);

        SetStatus(ok ? $"Saved to slot {slot + 1}." : $"Could not save slot {slot + 1}.");
        Refresh();
    }

    private void OnLoadClicked(int slot)
    {
        Ask($"Load slot {slot + 1}?\nUnsaved progress will be lost.", () => DoLoad(slot));
    }

    private void DoLoad(int slot)
    {
        if (!SaveManager.Load(slot))
        {
            SetStatus($"Could not load slot {slot + 1}.");
            Refresh();
            return;
        }

        // 불러온 세계를 봐야 하므로 창을 닫는다. 이 패널도 같이 꺼지므로 Refresh는 의미가 없다.
        if (MenuWindow.Instance != null) MenuWindow.Instance.Close();
    }

    private void OnDeleteClicked(int slot)
    {
        Ask($"Delete slot {slot + 1}?\nThis cannot be undone.", () => DoDelete(slot));
    }

    private void DoDelete(int slot)
    {
        bool ok = SaveManager.Delete(slot);

        SetStatus(ok ? $"Deleted slot {slot + 1}." : $"Could not delete slot {slot + 1}.");
        Refresh();
    }

    // ───────────────────────── 확인 창 ─────────────────────────

    private void Ask(string message, Action onYes)
    {
        pendingAction = onYes;

        if (confirmText != null) confirmText.text = message;
        if (confirmRoot != null) confirmRoot.SetActive(true);
    }

    private void OnConfirmYes()
    {
        Action action = pendingAction;

        // 먼저 닫는다. action이 창을 닫으면(불러오기) 이 뒤의 코드가 돌지 않을 수 있다.
        CloseConfirm();

        action?.Invoke();
    }

    private void CloseConfirm()
    {
        pendingAction = null;
        if (confirmRoot != null) confirmRoot.SetActive(false);
    }

    // ───────────────────────── 알림 문구 ─────────────────────────

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;

        if (statusRoutine != null)
        {
            StopCoroutine(statusRoutine);
            statusRoutine = null;
        }

        if (!string.IsNullOrEmpty(message) && statusSeconds > 0f && gameObject.activeInHierarchy)
            statusRoutine = StartCoroutine(ClearStatusAfterDelay());
    }

    private IEnumerator ClearStatusAfterDelay()
    {
        // 창이 열려 있는 동안 시간이 멈춰 있어도 흘러가야 한다.
        yield return new WaitForSecondsRealtime(statusSeconds);

        if (statusText != null) statusText.text = string.Empty;
        statusRoutine = null;
    }
}
