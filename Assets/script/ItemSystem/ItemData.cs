using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("�⺻ ����")]
    public string displayName; // ������ �̸�
    public string description; // ������ ����
    public Sprite icon; // ������ ������
    public string itemID; //������ �ĺ� ID
    
    
    
    
    [Header("����")]
    public bool canStack = true; // ��ø ���� ����
    public int maxStackAmount = 1; // �ִ� ��ø ����
}

