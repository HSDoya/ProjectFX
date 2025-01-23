using UnityEngine;
using System.Collections;

public class landtiles : MonoBehaviour
{
    private Renderer tileRenderer;
    private Color originalColor; // �ʱ� ����

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        originalColor = new Color(0.5f, 0.3f, 0.2f); // ������ �ʱ� �������� ����
        tileRenderer.material.color = originalColor; // �ʱ� ���� ����
    }

    public void ChangeTileColor(Color newColor, float duration)
    {
        StopAllCoroutines(); // ���� �ڷ�ƾ ����
        StartCoroutine(ChangeColorRoutine(newColor, duration));
    }

    private IEnumerator ChangeColorRoutine(Color newColor, float duration)
    {
        tileRenderer.material.color = newColor; // �� ���� ����
        yield return new WaitForSeconds(duration); // ������ �ð� ���
        tileRenderer.material.color = originalColor; // ���� �������� ����
    }
}
