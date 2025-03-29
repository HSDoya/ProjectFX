using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public Transform itemGrid;               // slot ������Ʈ
    public GameObject itemSlotPrefab;        // �� ��! (���� ���)

    private List<ItemUI> slots = new List<ItemUI>();

    void Start()
    {
        // ���� ���Ե� �޾Ƽ� ���� (Start���� �� ����)
        foreach (Transform child in itemGrid)
        {
            ItemUI slot = child.GetComponent<ItemUI>();
            if (slot != null)
            {
                slots.Add(slot);
            }
        }

        Inventory.instance.onItemChangedCallback += UpdateUI;
    }

    public void UpdateUI()
    {
        // ��� ���� �ʱ�ȭ
        foreach (ItemUI slot in slots)
        {
            slot.ClearSlot();  // ������ ����
        }

        // ���� �κ��丮 �����۸�ŭ ���Կ� ǥ��
        for (int i = 0; i < Inventory.instance.items.Count; i++)
        {
            if (i < slots.Count)
            {
                slots[i].SetItem(Inventory.instance.items[i].data);
            }
        }
    }

}
