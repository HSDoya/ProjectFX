using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 화면 좌상단에 얹는 미니멀 스타일 HP/스태미너/EXP HUD.
// HP·스태미너·레벨·경험치 모두 PlayerMove가 들고 있는 실제 값을 매 프레임 읽어서 표시만 한다.
// 이 컴포넌트는 어떤 수치도 자체 보관하지 않는다 - 예전엔 레벨/경험치만 여기 필드로 들고 있어서,
// 경험치 시스템을 붙이면 같은 수치의 소유자가 둘이 되는 구조였다.
public class PlayerHUD : MonoBehaviour
{
    [Header("Player 참조")]
    [SerializeField] private PlayerMove playerMove;

    [Header("HP (PlayerMove의 실제 값을 매 프레임 반영)")]
    public Image hpFill;
    public TextMeshProUGUI hpValueText;

    [Header("Stamina (PlayerMove의 실제 값을 매 프레임 반영)")]
    public Image staminaFill;
    public TextMeshProUGUI staminaValueText;

    [Header("EXP / Level (PlayerMove의 실제 값을 매 프레임 반영)")]
    public Image expFill;
    public TextMeshProUGUI levelText;

    // 레벨업 순간에만 잠깐 떠오르는 알림. 상시 표시값(levelText)과 달리 이건 이벤트로만 움직인다.
    // 비워두면 연출만 생략되고 나머지 HUD는 그대로 동작한다.
    [Header("레벨업 연출")]
    public TextMeshProUGUI levelUpText;
    [Tooltip("완전히 보이는 상태로 머무는 시간(초)")]
    public float levelUpHoldSeconds = 1.0f;
    [Tooltip("사라지는 데 걸리는 시간(초)")]
    public float levelUpFadeSeconds = 0.7f;
    [Tooltip("사라지며 위로 떠오르는 거리(px)")]
    public float levelUpRiseDistance = 36f;

    // 떠오르는 연출의 기준 위치. 애니메이션 중에 위치를 바꾸므로 원래 자리를 기억해 둔다.
    private Vector2 levelUpBasePos;
    private bool levelUpBaseCaptured;
    private Coroutine levelUpRoutine;

    // 구독해 둔 대상을 쥐고 그걸로 해제한다. 중간에 playerMove가 바뀌어도 엉뚱한 곳에서 빼지 않는다.
    private PlayerMove subscribed;

    private void Awake()
    {
        if (playerMove == null) playerMove = FindFirstObjectByType<PlayerMove>();
        HideLevelUpText();
    }

    private void OnEnable()
    {
        // 플레이어가 HUD보다 늦게 깨어났을 수 있으므로 여기서 한 번 더 찾는다.
        if (playerMove == null) playerMove = FindFirstObjectByType<PlayerMove>();

        subscribed = playerMove;
        if (subscribed != null) subscribed.OnLevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        if (subscribed != null) subscribed.OnLevelUp -= OnLevelUp;
        subscribed = null;
    }

    private void OnLevelUp(int newLevel, int gainedLevels)
    {
        if (levelUpText == null) return;

        // 여러 레벨이 한 번에 올랐으면 그 사실도 같이 보여준다(보스를 잡는 경우).
        levelUpText.text = gainedLevels > 1
            ? $"LEVEL UP!  LV.{newLevel}  (+{gainedLevels})"
            : $"LEVEL UP!  LV.{newLevel}";

        if (levelUpRoutine != null) StopCoroutine(levelUpRoutine);
        levelUpRoutine = StartCoroutine(PlayLevelUp());
    }

    private IEnumerator PlayLevelUp()
    {
        var rt = levelUpText.rectTransform;

        if (!levelUpBaseCaptured)
        {
            levelUpBasePos = rt.anchoredPosition;
            levelUpBaseCaptured = true;
        }

        levelUpText.gameObject.SetActive(true);
        levelUpText.alpha = 1f;
        rt.anchoredPosition = levelUpBasePos;

        if (levelUpHoldSeconds > 0f) yield return new WaitForSeconds(levelUpHoldSeconds);

        float t = 0f;
        while (t < levelUpFadeSeconds && levelUpFadeSeconds > 0f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / levelUpFadeSeconds);
            levelUpText.alpha = 1f - k;
            rt.anchoredPosition = levelUpBasePos + new Vector2(0f, levelUpRiseDistance * k);
            yield return null;
        }

        rt.anchoredPosition = levelUpBasePos;
        HideLevelUpText();
        levelUpRoutine = null;
    }

    private void HideLevelUpText()
    {
        if (levelUpText == null) return;
        levelUpText.alpha = 0f;
        levelUpText.gameObject.SetActive(false);
    }

    private void Update()
    {
        RefreshHp();
        RefreshStamina();
        RefreshExp();
    }

    private void RefreshHp()
    {
        if (hpFill == null) return;
        float ratio = (playerMove != null && playerMove.MaxHealth > 0)
            ? playerMove.currentHealth / playerMove.MaxHealth
            : 0f;
        SetFillRatio(hpFill, ratio);
        if (hpValueText != null && playerMove != null)
            hpValueText.text = Mathf.CeilToInt(playerMove.currentHealth).ToString();
    }

    private void RefreshStamina()
    {
        if (staminaFill == null) return;
        float ratio = (playerMove != null && playerMove.MaxStamina > 0)
            ? playerMove.currentStamina / playerMove.MaxStamina
            : 0f;
        SetFillRatio(staminaFill, ratio);
        if (staminaValueText != null && playerMove != null)
            staminaValueText.text = Mathf.CeilToInt(playerMove.currentStamina).ToString();
    }

    private void RefreshExp()
    {
        if (expFill != null)
        {
            float ratio = (playerMove != null && playerMove.ExpToNextLevel > 0)
                ? playerMove.currentExp / playerMove.ExpToNextLevel
                : 0f;
            SetFillRatio(expFill, ratio);
        }
        if (levelText != null && playerMove != null) levelText.text = $"LV. {playerMove.level}";
    }

    private static void SetFillRatio(Image fill, float ratio)
    {
        var rt = fill.rectTransform;
        var anchorMax = rt.anchorMax;
        anchorMax.x = Mathf.Clamp01(ratio);
        rt.anchorMax = anchorMax;
    }

}
