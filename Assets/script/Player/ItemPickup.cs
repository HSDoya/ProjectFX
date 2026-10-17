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

        var db = ItemDataManager.instance;  // CSV 로더 싱글턴
        if (db == null)
        {
            Debug.LogError("[ItemPickup] ItemDataManager.instance가 없습니다. 씬에 배치하세요.");
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

        // FieldItem과 같은 방식으로 한 번에 넣는다 (예전엔 1개씩 Add를 반복해서 획득 로직이 두 벌이었다).
        // AddItem은 들어간 만큼 item.quantity를 깎으므로, 실패해도 남은 수량을 그대로 알 수 있다.
        var item = new Item(data, Mathf.Max(1, amount));
        int originalQuantity = item.quantity;

        if (inv.AddItem(item))
        {
            Debug.Log($"{data.displayName} {originalQuantity}개 획득!");
            Destroy(gameObject);
            return;
        }

        // 전부 넣었을 때만 없앤다. 예전엔 일부만 들어가도 Destroy해서 남은 수량이 그대로 증발했다.
        amount = item.quantity; // 못 넣은 만큼만 남겨두고, 자리를 비운 뒤 다시 주울 수 있게 한다
        Debug.Log($"인벤토리가 가득 차서 {data.displayName}을(를) {item.quantity}개 남겼습니다.");
    }

    // 에디터에서 편하게 셋업하려고 공개 Getter 제공(선택)
    public void SetItem(string id, int qty = 1)
    {
        itemID = id;
        amount = Mathf.Max(1, qty);
    }
}
