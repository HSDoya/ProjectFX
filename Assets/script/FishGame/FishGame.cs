using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// 바(막대) 기반 낚시 미니게임 - 초간단 버전
/// - 빈 GameObject에 이 스크립트만 붙이면 실행 시 UI를 자동 생성합니다.
/// - Space(또는 마우스 왼쪽)로 판정, 성공 시 게이지(ChargeFill) 증가, 100%면 성공 처리.
/// </summary>


public class FishGame : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 barSize = new Vector2(480, 24); // Track 크기
    [SerializeField] private Vector2 markerSize = new Vector2(6, 36);
    [SerializeField] private float uiScale = 1f;   // 전체 스케일

    [Header("Motion")]
    [SerializeField] private float moveSpeed = 1.8f;   // 왕복 속도(초당 bar 비율)
    [SerializeField] private bool startFromLeft = true;

    [Header("Judge / Charge")]
    [Tooltip("성공 구간(0~1). 0=왼쪽, 1=오른쪽")]
    [Range(0, 1)] public float successMin = 0.45f;
    [Range(0, 1)] public float successMax = 0.55f;
    [Tooltip("성공 시 채워지는 양(0~1). 0.25 => 4번 성공")]
    [Range(0.05f, 1f)] public float chargePerHit = 0.25f;

    // --- runtime refs ---
    RectTransform canvasRT, trackRT, markerRT, fillRT;
    Image chargeFillImg;
    TextMeshProUGUI infoText;

    float t;        // 0~1, 막대 내 마커 위치
    float dir = 1;  // +1 → 오른쪽, -1 → 왼쪽
    bool running = true;

    void Awake()
    {
        BuildMinimalUI();
        //ResetGame();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!running) return;

        // 좌우 왕복
        t += dir * moveSpeed * Time.unscaledDeltaTime;
        if (t <= 0f) { t = 0f; dir = 1f; }
        if (t >= 1f) { t = 1f; dir = -1f; }

        UpdateMarker();

        // Space or Click → 판정
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            Judge();
    }

    void Judge()
    {
        bool ok = (successMin <= t && t <= successMax);
        if (ok)
        {
            chargeFillImg.fillAmount = Mathf.Clamp01(chargeFillImg.fillAmount + chargePerHit);
            infoText.text = $"Good!  {Mathf.RoundToInt(chargeFillImg.fillAmount * 100)}%";
            if (Mathf.Approximately(chargeFillImg.fillAmount, 1f))
            {
                infoText.text = "SUCCESS!";
                running = false;
                Debug.Log("[BarFishing] 성공!");
            }
        }
        else
        {
            infoText.text = "Miss";
        }
    }

    void ResetGame()
    {
        t = startFromLeft ? 0f : 1f;
        dir = startFromLeft ? 1f : -1f;
        chargeFillImg.fillAmount = 0f;
        infoText.text = "Space!";
        running = true;
        UpdateMarker();
    }

    void UpdateMarker()
    {
        float width = trackRT.rect.width;
        float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
        markerRT.anchoredPosition = new Vector2(x, 0f);
    }

    // ----------------- UI AUTO BUILD -----------------
    void BuildMinimalUI()
    {
        // 부모 캔버스(없으면 자동 생성)
        Canvas c = FindFirstObjectByType<Canvas>();
        if (c == null)
        {
            GameObject cg = new GameObject("MiniGameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            c = cg.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cg.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
        }
        canvasRT = c.GetComponent<RectTransform>();

        // 컨테이너
        GameObject panel = NewUI("FishingPanel", canvasRT, null);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(barSize.x + 200, 180);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.localScale = Vector3.one * uiScale;

        // Track
        GameObject track = NewUI("Track", panelRT, Color.gray);
        trackRT = track.GetComponent<RectTransform>();
        trackRT.sizeDelta = barSize;
        trackRT.anchoredPosition = new Vector2(0, 20);

        // ChargeFill
        GameObject fill = NewUI("ChargeFill", trackRT, new Color(0.1f, 0.65f, 0.7f));
        fillRT = fill.GetComponent<RectTransform>();
        fillRT.sizeDelta = barSize;
        var img = fill.GetComponent<Image>();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 0f;
        chargeFillImg = img;

        // Marker
        GameObject marker = NewUI("Marker", trackRT, Color.black);
        markerRT = marker.GetComponent<RectTransform>();
        markerRT.sizeDelta = markerSize;
        markerRT.anchoredPosition = Vector2.zero;

        // HitZone 시각화(기본 사각형 한 장)
        GameObject hz = NewUI("HitZone", trackRT, new Color(0, 0, 0, 0.7f));
        var hzRT = hz.GetComponent<RectTransform>();
        float width = barSize.x;
        float xMin = Mathf.Lerp(-width * 0.5f, width * 0.5f, successMin);
        float xMax = Mathf.Lerp(-width * 0.5f, width * 0.5f, successMax);
        hzRT.sizeDelta = new Vector2(Mathf.Max(6f, xMax - xMin), barSize.y + 8f);
        hzRT.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, 0f);

        // 안내/결과 텍스트
        GameObject txt = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txt.transform.SetParent(panelRT, false);
        infoText = txt.GetComponent<TextMeshProUGUI>();
        var txtRT = txt.GetComponent<RectTransform>();
        txtRT.sizeDelta = new Vector2(400, 40);
        txtRT.anchoredPosition = new Vector2(0, -40);
        infoText.alignment = TextAlignmentOptions.Center;
        infoText.fontSize = 28;
        infoText.text = "Space!";
    }

    GameObject NewUI(string name, RectTransform parent, Color? color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color ?? new Color(1, 1, 1, 0.12f);
        return go;
    }
}
