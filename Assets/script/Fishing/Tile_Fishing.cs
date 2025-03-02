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

        Debug.Log("������...");
        StartCoroutine(FishingProcess());
    }

    private IEnumerator FishingProcess()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log($"���� ���: {fishItem.displayName}!");

        bool added = Inventory.instance.Add(fishItem);
        if (added)
        {
            Debug.Log($"{fishItem.displayName} �������� �κ��丮�� �߰��Ǿ����ϴ�.");
        }
        else
        {
            Debug.Log("�κ��丮�� ������ �����մϴ�.");
        }

        playermove_manger.event_time = false;
    }
    public void yesButton()
    {
        testUI.SetActive(false);
        playermove_manger.event_time = true;
        Fishing();
    }

    public void noButton()
    {
        testUI.SetActive(false);
    }
}
