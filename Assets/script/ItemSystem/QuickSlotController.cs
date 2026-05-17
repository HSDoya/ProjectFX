using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트를 제어하기 위해 필요

public class QuickSlotController : MonoBehaviour
{
    [Header("UI Reference")]
    // 인스펙터에서 각 퀵슬롯의 테두리(Highlight) 이미지 9개를 순서대로 드래그해서 넣으세요.
    public Image[] slotHighlightImages;

    // 현재 선택된 슬롯 인덱스 (0~8)
    private int selectedSlotIndex = 0;

    private void Start()
    {
        // 게임 시작 시 0번(첫 번째) 슬롯을 선택된 상태로 만듭니다.
        UpdateHighlightUI();
    }

    private void Update()
    {
        // 1. 1~9번 숫자 키로 슬롯 선택하기
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }

        // 2. 마우스 휠로 슬롯 선택 변경하기 (스타듀밸리처럼)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            SelectSlot(selectedSlotIndex - 1); // 위로 굴리면 왼쪽 슬롯
        }
        else if (scroll < 0f)
        {
            SelectSlot(selectedSlotIndex + 1); // 아래로 굴리면 오른쪽 슬롯
        }

        // 3. 선택된 아이템 사용 (예: 마우스 왼쪽 클릭)
        if (Input.GetMouseButtonDown(0))
        {
            UseQuickSlotItem(selectedSlotIndex);
        }
    }

    // 슬롯을 선택하고 UI를 갱신하는 함수
    private void SelectSlot(int index)
    {
        // 마우스 휠로 인덱스가 범위를 벗어났을 때 순환되도록 처리
        if (index < 0) index = 8;
        if (index > 8) index = 0;

        selectedSlotIndex = index;
        UpdateHighlightUI();
    }

    // 선택된 슬롯의 테두리만 켜고, 나머지는 끄는 함수
    private void UpdateHighlightUI()
    {
        // 배열이 비어있으면 오류 방지
        if (slotHighlightImages == null || slotHighlightImages.Length == 0) return;

        for (int i = 0; i < slotHighlightImages.Length; i++)
        {
            if (slotHighlightImages[i] != null)
            {
                // 현재 인덱스(i)가 선택된 인덱스(selectedSlotIndex)와 같으면 true(켜짐), 아니면 false(꺼짐)
                slotHighlightImages[i].enabled = (i == selectedSlotIndex);
            }
        }
    }

    // 아이템 사용 로직 (기존 로직 유지)
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
            // TODO: 무기/도구 장착 시, 캐릭터 손에 해당 아이템 스프라이트 들려주기
        }
    }
}