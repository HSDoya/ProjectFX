using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class PlayerQuickSlot : MonoBehaviour
{
    [SerializeField] private Inventory inventory;

    [Header("UI Settings")]
    [SerializeField] private Image[] slotHighlightImages;

    [Header("Equip Settings")]
    [SerializeField] private SpriteRenderer equippedItemRenderer;

    // ★ 추가: 인스펙터에서 마우스로 조절할 '오른쪽을 바라볼 때의 손 위치 Offset'
    [SerializeField] private Vector3 rightHandOffset = new Vector3(0.3f, -0.1f, 0f);

    private bool isSwinging = false;
    private bool isFacingRight = true;

    private PlayerMove playerMove;

    public int selectedQuickSlotIndex = 0;
    public string currentEquipment = "";
    public ItemData currentEquippedItemData = null;

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
    }

    private void Start()
    {
        if (inventory == null && Inventory.instance != null)
            inventory = Inventory.instance;

        if (inventory != null)
        {
            inventory.onItemChangedCallback += UpdateCurrentEquipment;
        }

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
        HandleActionInput();

        // 매 프레임마다 무기 위치/방향 업데이트
        if (!isSwinging)
        {
            UpdateItemDirection();
        }
    }

    private void HandleQuickslotInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectQuickSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectQuickSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectQuickSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectQuickSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuickSlot(4);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) SelectQuickSlot(5);
        else if (Input.GetKeyDown(KeyCode.Alpha7)) SelectQuickSlot(6);
        else if (Input.GetKeyDown(KeyCode.Alpha8)) SelectQuickSlot(7);
        else if (Input.GetKeyDown(KeyCode.Alpha9)) SelectQuickSlot(8);
        else if (Input.GetKeyDown(KeyCode.Alpha0)) SelectQuickSlot(9);

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0)
        {
            int newIndex = selectedQuickSlotIndex - 1;
            if (newIndex < 0 && Inventory.instance != null && Inventory.instance.quickSlots != null)
                newIndex = Inventory.instance.quickSlots.Length - 1;

            SelectQuickSlot(newIndex);
        }
        else if (scroll < 0)
        {
            int newIndex = selectedQuickSlotIndex + 1;
            if (Inventory.instance != null && Inventory.instance.quickSlots != null && newIndex >= Inventory.instance.quickSlots.Length)
                newIndex = 0;

            SelectQuickSlot(newIndex);
        }
    }

    // 무기 위치 및 방향 업데이트 함수
    private void UpdateItemDirection()
    {
        float moveX = Input.GetAxisRaw("Horizontal");

        if (moveX > 0) isFacingRight = true;
        else if (moveX < 0) isFacingRight = false;

        if (equippedItemRenderer != null)
        {
            // ★ 수정: 오른쪽을 볼 때는 인스펙터 설정값 그대로, 왼쪽을 볼 때는 X값만 반전(-x)시킵니다.
            Vector3 targetPosition = rightHandOffset;
            if (!isFacingRight)
            {
                targetPosition.x = -targetPosition.x;
            }

            // 계산된 좌표를 손 오브젝트의 localPosition에 대입합니다.
            equippedItemRenderer.transform.localPosition = targetPosition;

            // 무기 스프라이트 좌우 반전
            equippedItemRenderer.flipX = !isFacingRight;
        }
    }

    private void HandleActionInput()
    {
        if (Input.GetMouseButtonDown(0) && currentEquippedItemData != null && !isSwinging)
        {
            StartCoroutine(SwingRoutine());
        }
    }

    private IEnumerator SwingRoutine()
    {
        isSwinging = true;

        Transform itemTransform = equippedItemRenderer.transform;
        Quaternion startRotation = Quaternion.Euler(0, 0, 0);

        float swingAngle = isFacingRight ? -90f : 90f;
        Quaternion endRotation = Quaternion.Euler(0, 0, swingAngle);

        float swingDuration = 0.15f;
        float elapsedTime = 0f;

        while (elapsedTime < swingDuration)
        {
            itemTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / swingDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        itemTransform.localRotation = endRotation;

        yield return new WaitForSeconds(0.05f);

        itemTransform.localRotation = startRotation;

        isSwinging = false;
    }

    private void SelectQuickSlot(int index)
    {
        if (Inventory.instance == null || Inventory.instance.quickSlots == null) return;
        if (index < 0 || index >= Inventory.instance.quickSlots.Length) return;

        selectedQuickSlotIndex = index;

        if (playerMove != null)
        {
            playerMove.event_time = false;
        }

        UpdateCurrentEquipment();
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

            if (equippedItemRenderer != null)
            {
                equippedItemRenderer.sprite = selectedItem.data.icon;
                equippedItemRenderer.enabled = true;
            }
        }
        else
        {
            currentEquipment = "";
            currentEquippedItemData = null;

            if (equippedItemRenderer != null)
            {
                equippedItemRenderer.sprite = null;
                equippedItemRenderer.enabled = false;
            }
        }
    }

    private void UpdateHighlightUI()
    {
        if (slotHighlightImages == null || slotHighlightImages.Length == 0) return;

        for (int i = 0; i < slotHighlightImages.Length; i++)
        {
            if (slotHighlightImages[i] != null)
            {
                slotHighlightImages[i].enabled = (i == selectedQuickSlotIndex);
            }
        }
    }
}