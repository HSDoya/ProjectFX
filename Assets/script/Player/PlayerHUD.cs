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
