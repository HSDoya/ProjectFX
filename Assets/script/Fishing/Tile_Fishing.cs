using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class Tile_Fishing : MonoBehaviour
{

    private Renderer tileRenderer;

    [SerializeField]
    private GameObject testUI; // 테스트용 UI
    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        testUI.SetActive(false);

    }

    public void AdvanceStage()
    {

        testUI.SetActive(true);
    }
    
    public void ExitButton()
    {
        testUI.SetActive(false);
    }
}
