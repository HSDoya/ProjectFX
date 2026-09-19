using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 화면 좌상단에 얹는 미니멀 스타일 HP/스태미너/EXP HUD.
// HP와 스태미너는 둘 다 PlayerMove가 들고 있는 실제 값을 그대로 반영한다(이중 저장 방지 - 이 컴포넌트는
// 값을 자체 보관하지 않고 매 프레임 읽기만 한다).
// EXP는 아직 게임에 해당 시스템이 없어서, 이 컴포넌트가 값을 직접 들고 표시만 한다
// (SetExp/SetLevel로 나중에 실제 시스템에서 채워 넣으면 됨).
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

    [Header("EXP / Level (테스트용 - 아직 실제 경험치 시스템 없음)")]
    public Image expFill;
    public TextMeshProUGUI levelText;
    public int level = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 100f;

    private void Awake()
    {
        if (playerMove == null) playerMove = FindFirstObjectByType<PlayerMove>();
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
        float ratio = (playerMove != null && playerMove.maxHealth > 0)
            ? playerMove.currentHealth / playerMove.maxHealth
            : 0f;
        SetFillRatio(hpFill, ratio);
        if (hpValueText != null && playerMove != null)
            hpValueText.text = Mathf.CeilToInt(playerMove.currentHealth).ToString();
    }

    private void RefreshStamina()
    {
        if (staminaFill == null) return;
        float ratio = (playerMove != null && playerMove.maxStamina > 0)
            ? playerMove.currentStamina / playerMove.maxStamina
            : 0f;
        SetFillRatio(staminaFill, ratio);
        if (staminaValueText != null && playerMove != null)
            staminaValueText.text = Mathf.CeilToInt(playerMove.currentStamina).ToString();
    }

    private void RefreshExp()
    {
        if (expFill != null)
        {
            float ratio = expToNextLevel > 0 ? currentExp / expToNextLevel : 0f;
            SetFillRatio(expFill, ratio);
        }
        if (levelText != null) levelText.text = $"LV. {level}";
    }

    private static void SetFillRatio(Image fill, float ratio)
    {
        var rt = fill.rectTransform;
        var anchorMax = rt.anchorMax;
        anchorMax.x = Mathf.Clamp01(ratio);
        rt.anchorMax = anchorMax;
    }

    public void SetExp(float current, float toNext)
    {
        currentExp = current;
        expToNextLevel = toNext;
    }

    public void SetLevel(int newLevel)
    {
        level = newLevel;
    }
}
