using UnityEngine;
using UnityEngine.UI;

public class AutoSlotGenerator : MonoBehaviour
{
    public Transform slotParent;              // Grid Layout Group이 붙은 빈 오브젝트
    public GameObject itemSlotPrefab;         // 아이템 슬롯 프리팹 (Icon.prefab)
    public int slotCount = 20;                // 만들 슬롯 개수

    void Start()
    {
        GenerateSlots();
    }

    public void GenerateSlots()
    {
        // 기존에 있던 자식 슬롯들 제거 (중복 방지)
        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = Instantiate(itemSlotPrefab, slotParent); // ← 선언 필수!
            slot.name = $"Icon ({i})";

            ItemUI itemUI = slot.GetComponent<ItemUI>();
            if (itemUI != null)
                itemUI.ClearSlot();  // 아이콘 비우기
        }

        Debug.Log($" 슬롯 {slotCount}개 자동 생성 완료!");
    }

}
