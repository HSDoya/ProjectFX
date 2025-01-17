using UnityEngine;
using System.Collections;
public class Tile_Fishing : MonoBehaviour
{
    private float fishingReady = 0.3f;   // 낚시 준비 단계 색상
    private float fishingInProgress = 0.5f; // 낚시 진행 중 색상
    private float fishCaught = 1f; // 물고기 획득 단계 색상
    private Renderer tileRenderer;
    private int currentStage = 0; // 0: 낚시 준비, 1: 낚시 진행, 2: 물고기 획득

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
    }

    public void AdvanceStage()
    {
        UpdateTileColor();
        currentStage = (currentStage + 1) % 3; // 0 -> 1 -> 2 -> 0 순환
    }

    private void UpdateTileColor()
    {
        switch (currentStage)
        {
            case 0:
                tileRenderer.material.color = new Color(fishingReady, fishingReady, 1f); // 파란색 (낚시 준비)
                break;
            case 1:
                tileRenderer.material.color = new Color(fishingInProgress, 1f, fishingInProgress); // 노란색 (낚시 진행 중)
                break;
            case 2:
                tileRenderer.material.color = new Color(fishCaught, 1f, fishCaught); // 녹색 (물고기 획득)
                break;
        }
    }
}
