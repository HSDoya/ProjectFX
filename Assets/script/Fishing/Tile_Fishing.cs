using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Tile_Fishing : MonoBehaviour
{
    [SerializeField] private GameObject testUI;
    [SerializeField] private PlayerMove playermove_manger;
    

    void Start()
    {
        testUI.SetActive(false);
        Debug.Log("디버그 체크");
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
        yield return new WaitForSeconds(2f);
        playermove_manger.event_time = false;
        playermove_manger.StopFishingAnimation();

        // CSV에서 아이템 로드
        var fishItem = ItemDataCsvLoader.instance?.GetItemDataByID("fish");

        if (fishItem == null)
        {
            Debug.LogError("[Fishing] fish 아이템을 찾을 수 없습니다. CSV를 확인하세요.");
            yield break;
        }

        bool added = Inventory.instance.Add(fishItem);

        if (added)
        {
            Debug.Log($"{fishItem.displayName} 아이템이 인벤토리에 추가되었습니다.");
        }
        else
        {
            Debug.Log("인벤토리에 공간이 부족합니다.");
        }

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
