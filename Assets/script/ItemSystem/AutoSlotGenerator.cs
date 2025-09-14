using UnityEngine;
using UnityEngine.UI;

public class AutoSlotGenerator : MonoBehaviour
{
    //public Transform slotParent;              // Grid Layout Group이 붙은 빈 오브젝트
    //public GameObject itemSlotPrefab;         // 아이템 슬롯 프리팹 (Icon.prefab)
    //public int slotCount = 20;                // 만들 슬롯 개수

    //void Awake()
    //{
    //    GenerateSlots();
    //}

    //public void GenerateSlots()
    //{
    //    foreach (Transform child in slotParent)
    //    {
    //        Destroy(child.gameObject);
    //    }

    //    for (int i = 0; i < slotCount; i++)
    //    {
    //        GameObject slot = Instantiate(itemSlotPrefab, slotParent);
    //        slot.name = $"Icon ({i})";

    //        ItemUI itemUI = slot.GetComponent<ItemUI>();
    //        if (itemUI != null)
    //            itemUI.ClearSlot();
    //    }

    //    Debug.Log($"슬롯 {slotCount}개 자동 생성 완료!");

    //    // 인벤토리 UI에 슬롯 목록 갱신
    //    FindObjectOfType<InventoryUI>()?.InitSlots();

    //    // 강제 UI 갱신
    //    Inventory.instance?.onItemChangedCallback?.Invoke();
    //}

}
