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

    // 반지름의 소유자는 PlayerMove(attackRange)다. 이 컴포넌트는 받은 값을 그리기만 한다.
    // (예전엔 여기 radius와 PlayerMove의 판정 거리가 서로 독립된 값이어서, 한쪽만 바꾸면
    //  화면의 원이 실제 사거리와 다른 거짓말을 하게 됐다.)
    public void SetRadius(float newRadius)
    {
        if (Mathf.Approximately(radius, newRadius)) return;
        radius = newRadius;
        if (line != null) DrawCircle();
    }

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