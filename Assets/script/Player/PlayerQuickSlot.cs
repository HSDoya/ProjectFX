using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // UI(Image) 제어를 위해 추가

public class PlayerQuickSlot : MonoBehaviour
{
    [SerializeField] private Inventory inventory;

    [Header("UI Settings")]
    [SerializeField] private Image[] slotHighlightImages; // 인스펙터에서 할당할 테두리 이미지들

    // PlayerMove의 event_time을 제어하기 위한 참조
    private PlayerMove playerMove;

    public int selectedQuickSlotIndex = 0;
    public string currentEquipment = "";
    public ItemData currentEquippedItemData = null;

    private void Awake()
    {
        // 같은 오브젝트에 붙어있는 PlayerMove를 가져옵니다.
        playerMove = GetComponent<PlayerMove>();
    }

    private void Start()
    {
        // 인스펙터에서 할당 안 했으면 싱글톤으로 가져옴
        if (inventory == null && Inventory.instance != null)
            inventory = Inventory.instance;

        if (inventory != null)
        {
            inventory.onItemChangedCallback += UpdateCurrentEquipment;
        }

        // 게임 시작 시 첫 번째 슬롯 테두리 켜기
        UpdateHighlightUI();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.onItemChangedCallback -= UpdateCurrentEquipment;
        }
    }

    private void Update()
    {
        HandleQuickslotInput();
    }

    private void HandleQuickslotInput()
    {
        // 1. 키보드 숫자 키 (1~5번)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectQuickSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectQuickSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectQuickSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectQuickSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuickSlot(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuickSlot(5);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuickSlot(6);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuickSlot(7);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuickSlot(8);

        // 2. 마우스 휠 스크롤로 퀵슬롯 이동
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0) // 휠 위로 굴림 (이전 슬롯)
        {
            int newIndex = selectedQuickSlotIndex - 1;
            if (newIndex < 0 && Inventory.instance != null && Inventory.instance.quickSlots != null)
                newIndex = Inventory.instance.quickSlots.Length - 1;

            SelectQuickSlot(newIndex);
        }
        else if (scroll < 0) // 휠 아래로 굴림 (다음 슬롯)
        {
            int newIndex = selectedQuickSlotIndex + 1;
            if (Inventory.instance != null && Inventory.instance.quickSlots != null && newIndex >= Inventory.instance.quickSlots.Length)
                newIndex = 0;

            SelectQuickSlot(newIndex);
        }
    }

    private void SelectQuickSlot(int index)
    {
        if (Inventory.instance == null || Inventory.instance.quickSlots == null) return;
        if (index < 0 || index >= Inventory.instance.quickSlots.Length) return;

        selectedQuickSlotIndex = index;

        // 아이템 변경 시 PlayerMove의 event_time을 초기화 (기존 코드 유지)
        if (playerMove != null)
        {
            playerMove.event_time = false;
        }

        UpdateCurrentEquipment();

        // 슬롯이 변경될 때마다 테두리 UI도 같이 갱신해줍니다.
        UpdateHighlightUI();
    }

    public void UpdateCurrentEquipment()
    {
        if (Inventory.instance == null || Inventory.instance.quickSlots == null) return;
        if (selectedQuickSlotIndex < 0 || selectedQuickSlotIndex >= Inventory.instance.quickSlots.Length) return;

        Item selectedItem = Inventory.instance.quickSlots[selectedQuickSlotIndex];

        if (selectedItem != null && selectedItem.data != null)
        {
            currentEquipment = selectedItem.data.itemID;
            currentEquippedItemData = selectedItem.data;
            Debug.Log($"[퀵슬롯 {selectedQuickSlotIndex + 1}번] 장착됨: {currentEquipment}");
        }
        else
        {
            currentEquipment = "";
            currentEquippedItemData = null;
        }
    }

    // 새롭게 추가된 테두리 UI 갱신 함수
    private void UpdateHighlightUI()
    {
        // 배열이 비어있으면 에러 방지
        if (slotHighlightImages == null || slotHighlightImages.Length == 0) return;

        for (int i = 0; i < slotHighlightImages.Length; i++)
        {
            if (slotHighlightImages[i] != null)
            {
                // 현재 검사 중인 인덱스(i)가 선택된 슬롯 인덱스와 같으면 true(켜짐), 아니면 false(꺼짐)
                slotHighlightImages[i].enabled = (i == selectedQuickSlotIndex);
            }
        }
    }
}