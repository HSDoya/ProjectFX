using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Tile_Fishing : MonoBehaviour
{
    [SerializeField] private GameObject testUI; // �׽�Ʈ�� UI
    [SerializeField] private PlayerMove playermove_manger; // PlayerMove ��ũ��Ʈ�� event_time �ʿ�
    [SerializeField] private ItemData fishItem; // Ư�� ����� ������

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

        Debug.Log("������...");
        StartCoroutine(FishingProcess());
    }

    private IEnumerator FishingProcess()
    {
        float waitTime = 2f; // ������ ��� �ð�
        yield return new WaitForSeconds(waitTime);

        Debug.Log($"���� ���: {fishItem.displayName}!");

        // ���� ItemData�� ���� �߰��� �� ����
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
        fishing();
    }

    public void noButton()
    {
        testUI.SetActive(false);
    }
}
