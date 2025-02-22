using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Tile_Fishing : MonoBehaviour
{
    private Renderer tileRenderer;

    [SerializeField]
    private GameObject testUI; // 테스트용 UI
    [SerializeField]
    private PlayerMove playermove_manger; // PlayerMove 스크립트에 event_time 변수가 있어야 함

    // 임시 낚시 결과 리스트 (추후 아이템 데이터로 대체 가능)
    private string[] fishResults = { "월척", "보물!!", "쓰레기", "고등어", "갈치" };

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        testUI.SetActive(false);
    }

    // UI를 통해 낚시 이벤트를 시작하기 위한 함수
    public void AdvanceStage()
    {
        testUI.SetActive(true);
    }

    // 낚시 이벤트 실행 함수
    public void fishing()
    {
        if (!playermove_manger.event_time) // 낚시 이벤트 활성 여부 체크
            return;

        Debug.Log("낚시중...");
        StartCoroutine(FishingProcess());
    }

    // 코루틴을 이용한 낚시 프로세스
    private IEnumerator FishingProcess()
    {
        float waitTime = Random.Range(1f, 3f); // 1~3초 랜덤 대기
        yield return new WaitForSeconds(waitTime);

        // 랜덤한 낚시 결과 선택
        string catchResult = fishResults[Random.Range(0, fishResults.Length)];
        Debug.Log($"낚시 결과: {catchResult}!");

        // Item 클래스를 이용해 새로운 아이템 인스턴스 생성
        Item caughtItem = new Item();
        caughtItem.item_name = catchResult;
        // 필요에 따라 다른 속성(isDefaultItem, icon 등)도 설정 가능

        // Inventory 싱글턴 인스턴스를 통해 아이템 추가
        bool added = Inventory.instance.Add(caughtItem);
        if (added)
        {
            Debug.Log($"{catchResult} 아이템이 인벤토리에 추가되었습니다.");
        }
        else
        {
            Debug.Log("인벤토리에 공간이 부족합니다.");
        }

        // 현재 인벤토리 리스트를 콘솔에 출력
        Debug.Log("현재 인벤토리:");
        foreach (Item item in Inventory.instance.items)
        {
            Debug.Log(item.item_name);
        }

        // 낚시 이벤트 종료 처리
        playermove_manger.event_time = false;
    }

    // UI의 예 버튼이 눌렸을 때 실행 (낚시 시작)
    public void yesButton()
    {
        testUI.SetActive(false);
        playermove_manger.event_time = true;
        fishing();
    }

    // UI의 아니오 버튼이 눌렸을 때 실행 (낚시 취소)
    public void noButton()
    {
        testUI.SetActive(false);
    }
}
