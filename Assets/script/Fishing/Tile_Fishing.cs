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
        yield return new WaitForSeconds(2f);

        Debug.Log($"낚시 결과: {fishItem.displayName}");

        bool added = Inventory.instance.Add(fishItem);

        if (added)
        {
            Debug.Log($"{fishItem.displayName} 아이템이 인벤토리에 추가되었습니다.");
            Debug.Log("<현재 인벤토리 상태>");
            foreach (Item item in Inventory.instance.items)
            {
                Debug.Log("- " + item.data.displayName);
            }
        }
        else
        {
            Debug.Log("인벤토리에 공간이 부족합니다.");
        }

        //  낚시 종료 처리만 깔끔하게
        playermove_manger.event_time = false;
    }
    public void yesButton()
    {
        testUI.SetActive(false);
        // playermove_manger.event_time = true;
        Debug.Log(playermove_manger.event_time);
        Fishing();
    }

    public void noButton()
    {
        testUI.SetActive(false);
    }
}
