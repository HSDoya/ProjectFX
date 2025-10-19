using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VerticalFishingMiniGameView : MonoBehaviour
{
    [Header("UI Refs (Inspector 연결)")]
    [SerializeField] private RectTransform trackRT;   // 세로 막대(Track)
    [SerializeField] private RectTransform markerRT;  // 검정 가로막대(마커)
    [SerializeField] private Image chargeFill;        // 채움 바 (Filled Vertical)
    [SerializeField] private RectTransform hitZoneRT; // 프리팹에 있는 HitZone Image 연결
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Motion")]
    [SerializeField] private float moveSpeed = 1.8f;  // 초당 bar 비율
    [SerializeField] private bool startFromBottom = true;

    [Header("Judge / Charge")]
    [Tooltip("성공 구간(0=아래, 1=위)")]
    [Range(0, 1)][SerializeField] private float successMin = 0.45f;
    [Range(0, 1)][SerializeField] private float successMax = 0.55f;
    [Tooltip("성공 시 누적 채움량(0~1). 0.25면 4번 성공 시 완료")]
    [Range(0.05f, 1f)][SerializeField] private float chargePerHit = 0.25f;

    [Header("HitZone Randomize")]
    [SerializeField] private bool randomizeOnSuccess = true;
    [SerializeField, Range(0.05f, 0.4f)] private float zoneMinHeight = 0.08f; // 히트존 최소 높이(비율)
    [SerializeField, Range(0.05f, 0.4f)] private float zoneMaxHeight = 0.20f; // 히트존 최대 높이(비율)
    [SerializeField, Range(0f, 0.15f)] private float edgeMargin = 0.05f;      // 위아래 여백

    private float t;     // 0~1, 현재 마커 위치
    private float dir;   // +1=위로, -1=아래로
    private bool running;
    private Action<bool> onFinished;
    private Action onClosed;

    private void Start()
    {
        UpdateHitZoneVisual(); // 시작 시 프리팹 히트존 초기 위치 반영
    }

    public void Open(Action<bool> onFinished, Action onClosed)
    {
        this.onFinished = onFinished;
        this.onClosed = onClosed;

        t = startFromBottom ? 0f : 1f;
        dir = startFromBottom ? 1f : -1f;

        if (chargeFill) chargeFill.fillAmount = 0f;
        if (infoText) infoText.text = "Space!";

        // 첫 시작 시 랜덤 배치도 가능
        if (randomizeOnSuccess) RandomizeHitZone();

        UpdateMarker();
        gameObject.SetActive(true);
        running = true;
    }

    public void Close()
    {
        running = false;
        onClosed?.Invoke();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!running) return;

        // 위↕아래 왕복
        t += dir * moveSpeed * Time.unscaledDeltaTime;
        if (t <= 0f) { t = 0f; dir = 1f; }
        if (t >= 1f) { t = 1f; dir = -1f; }

        UpdateMarker();

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            Judge();
    }

    private void UpdateMarker()
    {
        if (!trackRT || !markerRT) return;

        float height = trackRT.rect.height;
        float y = Mathf.Lerp(-height * 0.5f, height * 0.5f, t);
        markerRT.anchoredPosition = new Vector2(0f, y);
    }

    private void Judge()
    {
        bool ok = (successMin <= t && t <= successMax);
        if (!chargeFill || !infoText) return;

        if (ok)
        {
            chargeFill.fillAmount = Mathf.Clamp01(chargeFill.fillAmount + chargePerHit);
            infoText.text = $"Good!  {Mathf.RoundToInt(chargeFill.fillAmount * 100)}%";

            // 성공하면 히트존 새 위치로 이동
            if (randomizeOnSuccess)
                RandomizeHitZone();

            if (Mathf.Approximately(chargeFill.fillAmount, 1f))
            {
                infoText.text = "SUCCESS!";
                running = false;
                onFinished?.Invoke(true);
                Close();
            }
        }
        else
        {
            infoText.text = "Miss";
        }
    }

    //  히트존 프리팹 위치/크기 업데이트
    private void UpdateHitZoneVisual()
    {
        if (hitZoneRT == null || trackRT == null) return;

        float height = trackRT.rect.height;
        float yMin = Mathf.Lerp(-height * 0.5f, height * 0.5f, Mathf.Clamp01(successMin));
        float yMax = Mathf.Lerp(-height * 0.5f, height * 0.5f, Mathf.Clamp01(successMax));
        float zoneHeight = Mathf.Max(6f, yMax - yMin);

        hitZoneRT.sizeDelta = new Vector2(trackRT.rect.width + 6f, zoneHeight);
        hitZoneRT.anchoredPosition = new Vector2(0f, (yMin + yMax) * 0.5f);
    }

    // 히트존 위치를 랜덤하게 바꾸는 함수
    private void RandomizeHitZone()
    {
        if (!trackRT || hitZoneRT == null) return;

        // 랜덤 높이(비율)
        float zoneHeight = UnityEngine.Random.Range(zoneMinHeight, zoneMaxHeight);
        float center = UnityEngine.Random.Range(edgeMargin + zoneHeight * 0.5f,
                                                1f - edgeMargin - zoneHeight * 0.5f);

        successMin = Mathf.Clamp01(center - zoneHeight * 0.5f);
        successMax = Mathf.Clamp01(center + zoneHeight * 0.5f);
        UpdateHitZoneVisual();
    }
}
