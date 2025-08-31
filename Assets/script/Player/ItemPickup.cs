using UnityEngine;

// Player ItemPickup Code
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private string itemID;  // CSV의 itemID와 동일(예: "meat", "feather")
    [SerializeField] private int amount = 1; // 한 번에 줍는 수량

    private void Reset()
    {
        // 자동 기본값: Trigger로 사용
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var db = ItemDataCsvLoader.instance;  // CSV 로더 싱글턴
        if (db == null)
        {
            Debug.LogError("[ItemPickup] ItemDataCsvLoader.instance가 없습니다. 씬에 배치하세요.");
            return;
        }

        var data = db.GetItemDataByID(itemID);
        if (data == null)
        {
            Debug.LogError($"[ItemPickup] itemID={itemID} 를 CSV에서 찾을 수 없습니다.");
            return;
        }

        var inv = Inventory.instance;        // 인벤토리 싱글턴
        if (inv == null)
        {
            Debug.LogError("[ItemPickup] Inventory.instance가 없습니다. 씬에 배치하세요.");
            return;
        }

        bool ok = false;
        // amount만큼 Add 처리 (현재 Add는 1개씩 추가하도록 작성되어 있으니 반복)
        for (int i = 0; i < Mathf.Max(1, amount); i++)
        {
            if (!inv.Add(data)) { ok = (i > 0); break; } // 공간 부족 시 중단
            ok = true;
        }

        if (ok) Destroy(gameObject);
    }

    // 에디터에서 편하게 셋업하려고 공개 Getter 제공(선택)
    public void SetItem(string id, int qty = 1)
    {
        itemID = id;
        amount = Mathf.Max(1, qty);
    }
}
