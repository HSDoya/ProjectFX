using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Tile_Fishing : MonoBehaviour
{
    private Renderer tileRenderer;

    [SerializeField]
    private GameObject testUI; // �׽�Ʈ�� UI
    [SerializeField]
    private PlayerMove playermove_manger; // PlayerMove ��ũ��Ʈ�� event_time ������ �־�� ��

    // �ӽ� ���� ��� ����Ʈ (���� ������ �����ͷ� ��ü ����)
    private string[] fishResults = { "��ô", "����!!", "������", "����", "��ġ" };

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        testUI.SetActive(false);
    }

    // UI�� ���� ���� �̺�Ʈ�� �����ϱ� ���� �Լ�
    public void AdvanceStage()
    {
        testUI.SetActive(true);
    }

    // ���� �̺�Ʈ ���� �Լ�
    public void fishing()
    {
        if (!playermove_manger.event_time) // ���� �̺�Ʈ Ȱ�� ���� üũ
            return;

        Debug.Log("������...");
        StartCoroutine(FishingProcess());
    }

    // �ڷ�ƾ�� �̿��� ���� ���μ���
    private IEnumerator FishingProcess()
    {
        float waitTime = Random.Range(1f, 3f); // 1~3�� ���� ���
        yield return new WaitForSeconds(waitTime);

        // ������ ���� ��� ����
        string catchResult = fishResults[Random.Range(0, fishResults.Length)];
        Debug.Log($"���� ���: {catchResult}!");

        // Item Ŭ������ �̿��� ���ο� ������ �ν��Ͻ� ����
        Item caughtItem = new Item();
        caughtItem.item_name = catchResult;
        // �ʿ信 ���� �ٸ� �Ӽ�(isDefaultItem, icon ��)�� ���� ����

        // Inventory �̱��� �ν��Ͻ��� ���� ������ �߰�
        bool added = Inventory.instance.Add(caughtItem);
        if (added)
        {
            Debug.Log($"{catchResult} �������� �κ��丮�� �߰��Ǿ����ϴ�.");
        }
        else
        {
            Debug.Log("�κ��丮�� ������ �����մϴ�.");
        }

        // ���� �κ��丮 ����Ʈ�� �ֿܼ� ���
        Debug.Log("���� �κ��丮:");
        foreach (Item item in Inventory.instance.items)
        {
            Debug.Log(item.item_name);
        }

        // ���� �̺�Ʈ ���� ó��
        playermove_manger.event_time = false;
    }

    // UI�� �� ��ư�� ������ �� ���� (���� ����)
    public void yesButton()
    {
        testUI.SetActive(false);
        playermove_manger.event_time = true;
        fishing();
    }

    // UI�� �ƴϿ� ��ư�� ������ �� ���� (���� ���)
    public void noButton()
    {
        testUI.SetActive(false);
    }
}
