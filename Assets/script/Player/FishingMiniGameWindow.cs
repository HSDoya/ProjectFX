using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class FishingMiniGameWindow : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(720, 200);
    [SerializeField] private Vector2 barSize = new Vector2(480, 24);
    [SerializeField] private Vector2 markerSize = new Vector2(6, 36);
    [SerializeField] private float uiScale = 1f;

    [Header("Motion")]
    [SerializeField] private float moveSpeed = 1.8f;   // �պ� �ӵ�(�ʴ� ����)
    [SerializeField] private bool startFromLeft = true;

    [Header("Judge / Charge")]
    [Tooltip("���� ����(0~1). 0=����, 1=������")]
    [Range(0, 1)] public float successMin = 0.45f;
    [Range(0, 1)] public float successMax = 0.55f;
    [Tooltip("���� �� ä������ ��(0~1). 0.25 => 4�� ����")]
    [Range(0.05f, 1f)] public float chargePerHit = 0.25f;

    // ��Ÿ�� UI ����
    private RectTransform rootRT, trackRT, markerRT, fillRT;
    private Image chargeFillImg;
    private TextMeshProUGUI infoText;

    // ����
    private float t;     // 0~1, ���� �� ��Ŀ ��ġ
    private float dir;   // +1 �� ������, -1 �� ����
    private bool running;

    // �ݹ�
    private Action<bool> onFinished;
    private Action onClosed;

    private void Awake()
    {
        rootRT = GetComponent<RectTransform>();
        BuildUIUnderThis();
    }

    // PlayerMove���� ȣ��
    public void Open(Action<bool> onFinished, Action onClosed)
    {
        this.onFinished = onFinished;
        this.onClosed = onClosed;

        // ��ġ/���� �ʱ�ȭ
        t = startFromLeft ? 0f : 1f;
        dir = startFromLeft ? 1f : -1f;
        running = true;

        if (chargeFillImg) chargeFillImg.fillAmount = 0f;
        if (infoText) infoText.text = "Space!";

        // ��� ��ġ & ������
        rootRT.anchorMin = rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta = panelSize;
        rootRT.anchoredPosition = Vector2.zero;
        rootRT.localScale = Vector3.one * uiScale;

        UpdateMarker();
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!running) return;

        // �¿� �պ� �̵�
        t += dir * moveSpeed * Time.unscaledDeltaTime;
        if (t <= 0f) { t = 0f; dir = 1f; }
        if (t >= 1f) { t = 1f; dir = -1f; }

        UpdateMarker();

        // �Է�: Space = ����, ESC = ���� ����(�׽�Ʈ��), ���콺 ��Ŭ���� ����
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) Judge();
        if (Input.GetKeyDown(KeyCode.Escape)) ForceFailAndClose();
    }

    private void Judge()
    {
        bool ok = (successMin <= t && t <= successMax);
        if (ok)
        {
            chargeFillImg.fillAmount = Mathf.Clamp01(chargeFillImg.fillAmount + chargePerHit);
            infoText.text = $"Good!  {Mathf.RoundToInt(chargeFillImg.fillAmount * 100)}%";
            if (Mathf.Approximately(chargeFillImg.fillAmount, 1f))
            {
                running = false;
                infoText.text = "SUCCESS!";
                onFinished?.Invoke(true);
                // â �ݱ�
                Close();
            }
        }
        else
        {
            infoText.text = "Miss";
        }
    }

    public void ForceFailAndClose()
    {
        if (!running) { Close(); return; }
        running = false;
        infoText.text = "FAIL";
        onFinished?.Invoke(false);
        Close();
    }

    public void Close()
    {
        onClosed?.Invoke();
        Destroy(gameObject);
    }

    // ----------------- ���� ���� -----------------

    private void UpdateMarker()
    {
        float width = trackRT.rect.width;
        float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
        markerRT.anchoredPosition = new Vector2(x, 0f);
    }

    private void BuildUIUnderThis()
    {
        // Panel ���(���� ��)
        var panel = NewUI("Panel", transform as RectTransform, new Color(1, 1, 1, 0.08f));
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.sizeDelta = panelSize;
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;

        // Track
        var track = NewUI("Track", panelRT, Color.gray);
        trackRT = track.GetComponent<RectTransform>();
        trackRT.sizeDelta = barSize;
        trackRT.anchoredPosition = new Vector2(0, 24);

        // ChargeFill
        var fill = NewUI("ChargeFill", trackRT, new Color(0.1f, 0.65f, 0.7f));
        fillRT = fill.GetComponent<RectTransform>();
        fillRT.sizeDelta = barSize;
        var img = fill.GetComponent<Image>();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 0f;
        img.raycastTarget = false;
        chargeFillImg = img;

        // Marker
        var marker = NewUI("Marker", trackRT, Color.black);
        markerRT = marker.GetComponent<RectTransform>();
        markerRT.sizeDelta = markerSize;
        markerRT.anchoredPosition = Vector2.zero;

        // HitZone �ð�ȭ
        var hz = NewUI("HitZone", trackRT, new Color(0, 0, 0, 0.7f));
        var hzRT = hz.GetComponent<RectTransform>();
        float width = barSize.x;
        float xMin = Mathf.Lerp(-width * 0.5f, width * 0.5f, successMin);
        float xMax = Mathf.Lerp(-width * 0.5f, width * 0.5f, successMax);
        hzRT.sizeDelta = new Vector2(Mathf.Max(6f, xMax - xMin), barSize.y + 8f);
        hzRT.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, 0f);

        // �ؽ�Ʈ
        var txtGO = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(panelRT, false);
        infoText = txtGO.GetComponent<TextMeshProUGUI>();
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.sizeDelta = new Vector2(400, 40);
        txtRT.anchoredPosition = new Vector2(0, -40);
        infoText.alignment = TextAlignmentOptions.Center;
        infoText.fontSize = 28;
        infoText.text = "Space!";
        infoText.raycastTarget = false;
    }

    private GameObject NewUI(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return go;
    }
}
