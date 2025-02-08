using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class Tile_Fishing : MonoBehaviour
{

    private Renderer tileRenderer;
    
    [SerializeField]
    private GameObject testUI; // 테스트용 UI
    [SerializeField]
    private PlayerMove playermove_manger;
    private string[] fishResults = { "월척", "보물!!", "쓰레기", "고등어", "갈치" }; // 임시 낚시 결과 리스트 -> 아이템, 인벤토리 코드 추가시 삭제  

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
        if (!playermove_manger.event_time) // 낚시 이벤트 비활성화시 return
            return;
        Debug.Log("낚시중...");
        StartCoroutine(FishingProcess());

    }
    private IEnumerator FishingProcess()
    {
        float waitTime = Random.Range(1f, 3f); // 1~3초 랜덤 시간 설정
        yield return new WaitForSeconds(waitTime); // 대기

        string catchResult = fishResults[Random.Range(0, fishResults.Length)]; // 랜덤한 낚시 결과 선택
        Debug.Log($"낚시 결과: {catchResult}!");
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
