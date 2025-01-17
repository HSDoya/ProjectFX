using UnityEngine;
using System.Collections;
public class Tile_Fishing : MonoBehaviour
{
    private float fishingReady = 0.3f;   // ���� �غ� �ܰ� ����
    private float fishingInProgress = 0.5f; // ���� ���� �� ����
    private float fishCaught = 1f; // ����� ȹ�� �ܰ� ����
    private Renderer tileRenderer;
    private int currentStage = 0; // 0: ���� �غ�, 1: ���� ����, 2: ����� ȹ��

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
    }

    public void AdvanceStage()
    {
        UpdateTileColor();
        currentStage = (currentStage + 1) % 3; // 0 -> 1 -> 2 -> 0 ��ȯ
    }

    private void UpdateTileColor()
    {
        switch (currentStage)
        {
            case 0:
                tileRenderer.material.color = new Color(fishingReady, fishingReady, 1f); // �Ķ��� (���� �غ�)
                break;
            case 1:
                tileRenderer.material.color = new Color(fishingInProgress, 1f, fishingInProgress); // ����� (���� ���� ��)
                break;
            case 2:
                tileRenderer.material.color = new Color(fishCaught, 1f, fishCaught); // ��� (����� ȹ��)
                break;
        }
    }
}
