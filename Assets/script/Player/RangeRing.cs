using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RangeRing : MonoBehaviour
{
    [Header("Ring Settings")]
    public float radius = 1.5f;
    public float lineWidth = 0.05f;
    public int segments = 50;
    public Color ringColor = new Color(1f, 0f, 0f, 0.5f);

    // ==========================================
    // ★ 추가: 인스펙터에서 레이어 순서를 설정할 수 있도록 변수화
    // ==========================================
    [Header("Sorting Settings")]
    public string sortingLayerName = "Default"; // 스프라이트 정렬 레이어 이름
    public int sortingOrder = -1;               // 레이어 순서 (Order in Layer)

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();

        line.positionCount = segments + 1;
        line.useWorldSpace = false;
        line.loop = true;

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        // ==========================================
        // ★ 수정: 하드코딩된 -1 대신 인스펙터의 변수 값을 적용
        // ==========================================
        line.sortingLayerName = sortingLayerName;
        line.sortingOrder = sortingOrder;

        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = ringColor;
        line.endColor = ringColor;

        DrawCircle();
    }

    private void DrawCircle()
    {
        float angle = 0f;
        for (int i = 0; i < (segments + 1); i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            line.SetPosition(i, new Vector3(x, y, 0));
            angle += (360f / segments);
        }
    }
}