using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class landtiles : MonoBehaviour
{
    private float farming = 1f;
    private float sowingseeds = 0.3f;
    private float Sprinklewater = 0.5f;
    private Renderer tileRenderer;
    private int currentStage = 0; // 0: farming, 1: sowingseeds, 2: Sprinklewater

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
    }
    public void AdvanceStage()
    {
        UpdateTileColor();
        currentStage = (currentStage + 1) % 3; // 0 -> 1 -> 2 -> 0 ¼øÈ¯
    }
    private void UpdateTileColor()
    {
        switch (currentStage)
        {
            case 0:
                tileRenderer.material.color = new Color(1f, farming, farming); // »¡°£»ö
                break;
            case 1:
                tileRenderer.material.color = new Color(sowingseeds, sowingseeds, 1f); // ÆÄ¶õ»ö
                break;
            case 2:
                tileRenderer.material.color = new Color(Sprinklewater, 1f, Sprinklewater); // ³ì»ö
                break;
        }
    }
}
