using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerQuickSlot : MonoBehaviour
{
    [SerializeField] private Inventory inventory;

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
}