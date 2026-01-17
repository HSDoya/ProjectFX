using UnityEngine;

public class TreeOcclusion : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("플레이어가 숨었을 때 나무의 투명도 (0~1)")]
    public float transparencyAlpha = 0.5f;

    [Tooltip("숨었을 때 플레이어 색상 (어두운 회색)")]
    public Color hiddenPlayerColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Tooltip("아웃라인 효과가 있는 플레이어 머티리얼")]
    public Material outlineMaterial; // 위에서 만든 Mat_PlayerOutline 연결

    private SpriteRenderer treeRenderer;
    private Material defaultPlayerMaterial; // 원래 플레이어 머티리얼 저장용

    private void Awake()
    {
        treeRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Player 태그를 가진 물체가 들어왔는지 확인
        if (collision.CompareTag("Player"))
        {
            // 1. 나무 반투명하게 변경
            if (treeRenderer != null)
            {
                Color color = treeRenderer.color;
                color.a = transparencyAlpha;
                treeRenderer.color = color;
            }

            // 2. 플레이어 시각 효과 변경
            SpriteRenderer playerSR = collision.GetComponent<SpriteRenderer>();
            if (playerSR != null)
            {
                // 원래 머티리얼 백업
                defaultPlayerMaterial = playerSR.material;

                // 색상 어둡게 & 아웃라인 머티리얼 적용
                playerSR.color = hiddenPlayerColor;
                playerSR.material = outlineMaterial;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. 나무 투명도 원상복구
            if (treeRenderer != null)
            {
                Color color = treeRenderer.color;
                color.a = 1f;
                treeRenderer.color = color;
            }

            // 2. 플레이어 시각 효과 원상복구
            SpriteRenderer playerSR = collision.GetComponent<SpriteRenderer>();
            if (playerSR != null)
            {
                playerSR.color = Color.white; // 원래 색(흰색)

                // 머티리얼이 있다면 원래대로 복구
                if (defaultPlayerMaterial != null)
                {
                    playerSR.material = defaultPlayerMaterial;
                }
            }
        }
    }
}
