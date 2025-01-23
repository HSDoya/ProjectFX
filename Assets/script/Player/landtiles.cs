using UnityEngine;
using System.Collections;

public class landtiles : MonoBehaviour
{
    private Renderer tileRenderer;
    private Color originalColor; // 초기 색상

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        originalColor = new Color(0.5f, 0.3f, 0.2f); // 갈색을 초기 색상으로 설정
        tileRenderer.material.color = originalColor; // 초기 색상 적용
    }

    public void ChangeTileColor(Color newColor, float duration)
    {
        StopAllCoroutines(); // 기존 코루틴 중지
        StartCoroutine(ChangeColorRoutine(newColor, duration));
    }

    private IEnumerator ChangeColorRoutine(Color newColor, float duration)
    {
        tileRenderer.material.color = newColor; // 새 색상 적용
        yield return new WaitForSeconds(duration); // 지정된 시간 대기
        tileRenderer.material.color = originalColor; // 원래 색상으로 복구
    }
}
