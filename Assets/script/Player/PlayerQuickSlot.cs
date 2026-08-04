using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

// 퀵슬롯 선택 상태(selectedQuickSlotIndex)와 현재 장착 아이템의 단일 소유자.
// PlayerMove는 이 값을 읽기만 하고 직접 갱신하지 않는다 (예전엔 PlayerMove도 자체적으로
// 같은 상태를 따로 들고 있어서, 같은 번호를 연타하면 한쪽만 빈손으로 토글되고
// 다른 쪽(이 스크립트)은 그대로 남아 있는 불일치가 있었다).
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

    // 게임 시작 시 빈손(-1)으로 시작
    public int selectedQuickSlotIndex = -1;
    public string currentEquipment = "";
    public ItemData currentEquippedItemData = null;

    // 선택된 슬롯/장착 아이템이 바뀔 때마다 호출 (PlayerMove의 사거리 표시 갱신 등에 사용)
    public event Action OnEquippedChanged;

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

        UpdateCurrentEquipment();
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
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleQuickSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleQuickSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleQuickSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleQuickSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleQuickSlot(4);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) ToggleQuickSlot(5);
        else if (Input.GetKeyDown(KeyCode.Alpha7)) ToggleQuickSlot(6);
        else if (Input.GetKeyDown(KeyCode.Alpha8)) ToggleQuickSlot(7);
        else if (Input.GetKeyDown(KeyCode.Alpha9)) ToggleQuickSlot(8);
        else if (Input.GetKeyDown(KeyCode.Alpha0)) ToggleQuickSlot(9);

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

    // 숫자 키 전용: 이미 선택된 슬롯을 다시 누르면 빈손(-1)으로 토글
    private void ToggleQuickSlot(int index)
    {
        if (selectedQuickSlotIndex == index)
            SelectQuickSlot(-1);
        else
            SelectQuickSlot(index);
    }

    private void SelectQuickSlot(int index)
    {
        if (Inventory.instance == null || Inventory.instance.quickSlots == null) return;
        // -1(빈손)은 유효한 상태이므로 하한은 -1로 체크
        if (index < -1 || index >= Inventory.instance.quickSlots.Length) return;

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

        if (selectedQuickSlotIndex < 0 || selectedQuickSlotIndex >= Inventory.instance.quickSlots.Length)
        {
            currentEquipment = "";
            currentEquippedItemData = null;

            if (equippedItemRenderer != null)
            {
                equippedItemRenderer.sprite = null;
                equippedItemRenderer.enabled = false;
            }

            OnEquippedChanged?.Invoke();
            return;
        }

        Item selectedItem = Inventory.instance.quickSlots[selectedQuickSlotIndex];

        if (selectedItem != null && selectedItem.data != null)
        {
            currentEquipment = selectedItem.data.itemID;
            currentEquippedItemData = selectedItem.data;

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

        OnEquippedChanged?.Invoke();
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
