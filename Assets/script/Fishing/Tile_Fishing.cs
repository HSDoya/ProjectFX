using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Tile_Fishing : MonoBehaviour
{
    [SerializeField] private GameObject testUI; // 테스트용 UI
    [SerializeField] private PlayerMove playermove_manger; // PlayerMove 스크립트의 event_time 필요
    [SerializeField] private ItemData fishItem; // 특정 물고기 아이템

    void Start()
    {
        testUI.SetActive(false);
    }

    public void AdvanceStage()
    {
        testUI.SetActive(true);
    }

    public void fishing()
    {
        if (!playermove_manger.event_time) return;

        Debug.Log("낚시중...");
        StartCoroutine(FishingProcess());
    }

    private IEnumerator FishingProcess()
    {
        float waitTime = 2f; // 고정된 대기 시간
        yield return new WaitForSeconds(waitTime);

        Debug.Log($"낚시 결과: {fishItem.displayName}!");

        // 이제 ItemData를 직접 추가할 수 있음
        bool added = Inventory.instance.Add(fishItem);
        if (added)
        {
            Debug.Log($"{fishItem.displayName} 아이템이 인벤토리에 추가되었습니다.");
        }
        else
        {
            Debug.Log("인벤토리에 공간이 부족합니다.");
        }

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
