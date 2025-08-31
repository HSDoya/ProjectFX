using UnityEngine;

// Player ItemPickup Code
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private string itemID;  // CSV�� itemID�� ����(��: "meat", "feather")
    [SerializeField] private int amount = 1; // �� ���� �ݴ� ����

    private void Reset()
    {
        // �ڵ� �⺻��: Trigger�� ���
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var db = ItemDataCsvLoader.instance;  // CSV �δ� �̱���
        if (db == null)
        {
            Debug.LogError("[ItemPickup] ItemDataCsvLoader.instance�� �����ϴ�. ���� ��ġ�ϼ���.");
            return;
        }

        var data = db.GetItemDataByID(itemID);
        if (data == null)
        {
            Debug.LogError($"[ItemPickup] itemID={itemID} �� CSV���� ã�� �� �����ϴ�.");
            return;
        }

        var inv = Inventory.instance;        // �κ��丮 �̱���
        if (inv == null)
        {
            Debug.LogError("[ItemPickup] Inventory.instance�� �����ϴ�. ���� ��ġ�ϼ���.");
            return;
        }

        bool ok = false;
        // amount��ŭ Add ó�� (���� Add�� 1���� �߰��ϵ��� �ۼ��Ǿ� ������ �ݺ�)
        for (int i = 0; i < Mathf.Max(1, amount); i++)
        {
            if (!inv.Add(data)) { ok = (i > 0); break; } // ���� ���� �� �ߴ�
            ok = true;
        }

        if (ok) Destroy(gameObject);
    }

    // �����Ϳ��� ���ϰ� �¾��Ϸ��� ���� Getter ����(����)
    public void SetItem(string id, int qty = 1)
    {
        itemID = id;
        amount = Mathf.Max(1, qty);
    }
}
