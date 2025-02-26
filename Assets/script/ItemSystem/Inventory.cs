using UnityEngine;
using System.Collections.Generic;
public class Inventory : MonoBehaviour
{
    #region Singleton
    public static Inventory instance;
    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogWarning("More than one instance of Inventory found!");
            return;
        }
        instance = this;
    }
    #endregion

    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    public int space = 20;  // �κ��丮 ������ ������ ��
    public List<Item> items = new List<Item>();  // �κ��丮�� ���� �ִ� ������ ����Ʈ

    public bool Add(ItemData newItemData)
    {
        if (items.Count >= space)
        {
            Debug.Log("Not enough room.");
            return false;
        }

        // Item ��ü ��� ItemData ����
        Item newItem = new Item(newItemData);
        items.Add(newItem);

        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();

        return true;
    }
}
