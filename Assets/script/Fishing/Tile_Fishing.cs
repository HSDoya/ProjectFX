using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Tile_Fishing : MonoBehaviour
{
    [SerializeField] private GameObject testUI;
    [SerializeField] private PlayerMove playermove_manger;
    [SerializeField] private ItemData fishItem;

    void Start()
    {
        testUI.SetActive(false);
    }

    public void AdvanceStage()
    {
        testUI.SetActive(true);
    }

    public void Fishing()
    {
        if (!playermove_manger.event_time) return;

        Debug.Log("낚시중...");
        StartCoroutine(FishingProcess());
    }

    private IEnumerator FishingProcess()
    {
        //  1. 낚시 애니메이션 동안 기다리기
        yield return new WaitForSeconds(2f);

        // 2. 낚시 종료 (event flag + animation 종료)
        playermove_manger.event_time = false;
        playermove_manger.StopFishingAnimation();

        //  3. 아이템 지급
        bool added = Inventory.instance.Add(fishItem);

        if (added)
        {
            Debug.Log($"{fishItem.displayName} 아이템이 인벤토리에 추가되었습니다.");
        }
        else
        {
            Debug.Log("인벤토리에 공간이 부족합니다.");
        }

        // 4. 인벤토리 상태 출력 (UI 갱신 콜백이 자동으로 연결돼 있다고 가정)
        foreach (Item item in Inventory.instance.items)
        {
            Debug.Log("- " + item.data.displayName);
        }
    }
    public void yesButton()
    {
        testUI.SetActive(false);
        playermove_manger.event_time = true;
        Debug.Log(playermove_manger.event_time);
        Fishing();
    }

    public void noButton()
    {
        testUI.SetActive(false);
    }
}
