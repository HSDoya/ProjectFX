using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// ��(����) ��� ���� �̴ϰ��� - �ʰ��� ����
/// - �� GameObject�� �� ��ũ��Ʈ�� ���̸� ���� �� UI�� �ڵ� �����մϴ�.
/// - Space(�Ǵ� ���콺 ����)�� ����, ���� �� ������(ChargeFill) ����, 100%�� ���� ó��.
/// </summary>


public class FishGame : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 barSize = new Vector2(480, 24); // Track ũ��
    [SerializeField] private Vector2 markerSize = new Vector2(6, 36);
    [SerializeField] private float uiScale = 1f;   // ��ü ������

    [Header("Motion")]
    [SerializeField] private float moveSpeed = 1.8f;   // �պ� �ӵ�(�ʴ� bar ����)
    [SerializeField] private bool startFromLeft = true;

    [Header("Judge / Charge")]
    [Tooltip("���� ����(0~1). 0=����, 1=������")]
    [Range(0, 1)] public float successMin = 0.45f;
    [Range(0, 1)] public float successMax = 0.55f;
    [Tooltip("���� �� ä������ ��(0~1). 0.25 => 4�� ����")]
    [Range(0.05f, 1f)] public float chargePerHit = 0.25f;

    // --- runtime refs ---
    RectTransform canvasRT, trackRT, markerRT, fillRT;
    Image chargeFillImg;
    TextMeshProUGUI infoText;

    float t;        // 0~1, ���� �� ��Ŀ ��ġ
    float dir = 1;  // +1 �� ������, -1 �� ����
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

        // �¿� �պ�
        t += dir * moveSpeed * Time.unscaledDeltaTime;
        if (t <= 0f) { t = 0f; dir = 1f; }
        if (t >= 1f) { t = 1f; dir = -1f; }

        UpdateMarker();

        // Space or Click �� ����
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
                Debug.Log("[BarFishing] ����!");
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
        // �θ� ĵ����(������ �ڵ� ����)
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

        // �����̳�
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

        // HitZone �ð�ȭ(�⺻ �簢�� �� ��)
        GameObject hz = NewUI("HitZone", trackRT, new Color(0, 0, 0, 0.7f));
        var hzRT = hz.GetComponent<RectTransform>();
        float width = barSize.x;
        float xMin = Mathf.Lerp(-width * 0.5f, width * 0.5f, successMin);
        float xMax = Mathf.Lerp(-width * 0.5f, width * 0.5f, successMax);
        hzRT.sizeDelta = new Vector2(Mathf.Max(6f, xMax - xMin), barSize.y + 8f);
        hzRT.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, 0f);

        // �ȳ�/��� �ؽ�Ʈ
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
