using UnityEngine;

public class QuickSlotController : MonoBehaviour
{
    private void Update()
    {
        // 1~9번 키 입력 감지 (0번은 인덱스 9로 처리)
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                UseQuickSlotItem(i);
            }
        }
    }

    private void UseQuickSlotItem(int slotIndex)
    {
        // Inventory 싱글톤이 없거나 퀵슬롯 배열 범위를 벗어나면 리턴
        if (Inventory.instance == null) return;
        if (slotIndex < 0 || slotIndex >= Inventory.instance.quickSlots.Length) return;

        Item itemToUse = Inventory.instance.quickSlots[slotIndex];

        // 슬롯이 비어있으면 무시
        if (itemToUse == null || itemToUse.data == null) return;

        // 아이템 타입에 따른 처리 로직
        if (itemToUse.data.itemType == ItemType.Consumable)
        {
            Debug.Log($"{itemToUse.data.displayName}을(를) 사용했습니다!");
            // TODO: 플레이어 체력 회복 등의 실제 효과 적용 로직 추가

            // 수량 감소 및 0개일 때 슬롯 비우기
            itemToUse.quantity--;
            if (itemToUse.quantity <= 0)
            {
                Inventory.instance.quickSlots[slotIndex] = null;
            }

            // UI 갱신
            Inventory.instance.RefreshUI();
        }
        else if (itemToUse.data.itemType == ItemType.Equipment)
        {
            Debug.Log($"{itemToUse.data.displayName} 장착 시도!");
            // TODO: 장비 장착 로직 호출 (EquipmentManager.instance.Equip... 등)
        }
    }
}