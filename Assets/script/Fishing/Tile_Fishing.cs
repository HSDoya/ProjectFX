using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class Tile_Fishing : MonoBehaviour
{

    private Renderer tileRenderer;
    
    [SerializeField]
    private GameObject testUI; // �׽�Ʈ�� UI
    [SerializeField]
    private PlayerMove playermove_manger;
    private string[] fishResults = { "��ô", "����!!", "������", "����", "��ġ" }; // �ӽ� ���� ��� ����Ʈ -> ������, �κ��丮 �ڵ� �߰��� ����  

    void Start()
    {
      
        tileRenderer = GetComponent<Renderer>();
        testUI.SetActive(false);
       
    }

   
    public void AdvanceStage()
    {

        testUI.SetActive(true);
    }

    public void fishing()
    {
        if (!playermove_manger.event_time) // ���� �̺�Ʈ ��Ȱ��ȭ�� return
            return;
        Debug.Log("������...");
        StartCoroutine(FishingProcess());

    }
    private IEnumerator FishingProcess()
    {
        float waitTime = Random.Range(1f, 3f); // 1~3�� ���� �ð� ����
        yield return new WaitForSeconds(waitTime); // ���

        string catchResult = fishResults[Random.Range(0, fishResults.Length)]; // ������ ���� ��� ����
        Debug.Log($"���� ���: {catchResult}!");
        playermove_manger.event_time = false;
    }


    public void yesButton()
    {
        testUI.SetActive(false);
        playermove_manger.event_time = true;
        fishing();
    }

    public void noButton()
    {
        testUI.SetActive(false);
    }
}
