using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;
using Kinnly;

public class PlayerMove : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed = 5f;
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    public Tilemap farmTilemap;
    public Tilemap waterTilemap;
    public landtiles landTileManager;
    private string currentEquipment = "";

    public bool event_time;
    Animator anim;
    private GameObject collidedObject = null;

    [SerializeField] private Inventory inventory;
    [SerializeField] private ObjectSpawner objectSpawner; // 드래그 연결 필요

    // ★ 추가된 변수: 현재 선택된 퀵슬롯 번호 (0~13)
    public int selectedQuickSlotIndex = 0;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        event_time = false;
    }

    // ★ 추가: 인벤토리 아이템 변경 시 손에 든 장비도 동기화하도록 이벤트 구독
    private void Start()
    {
        if (inventory != null)
        {
            inventory.onItemChangedCallback += UpdateCurrentEquipment;
        }
    }

    // ★ 추가: 메모리 누수 방지용 이벤트 해제
    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.onItemChangedCallback -= UpdateCurrentEquipment;
        }
    }

    private void Update()
    {
        Quickslot();

        // 마우스 클릭 시 농사/도살 행동 처리
        if (Mouse.current.leftButton.wasPressedThisFrame && !event_time)
        {
            OnMouseClick();
        }

        // F키 누를 시 스폰된 오브젝트 파괴
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryDestroyNearestSpawnedObject();
        }
    }

    private void LateUpdate()
    {
        anim.SetFloat("Speed", inputVec.magnitude);
        if (inputVec.x != 0)
        {
            spriteRenderer.flipX = inputVec.x < 0;
        }
    }

    private void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
        if (!event_time)
        {
            anim.SetBool("Fishing", false);
        }
    }

    private void FixedUpdate()
    {
        if (!event_time)
            rigid.linearVelocity = inputVec * speed;
        else
            rigid.linearVelocity = Vector2.zero;
    }

    // 마우스 클릭 함수 (동물 도살 및 농사)
    private void OnMouseClick()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0;

        // 동물 제거 시도
        // 🔪 Knife 도살 처리
        if (currentEquipment == "Knife")
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

            if (hit != null)
            {
                AnimalHealth health = hit.GetComponent<AnimalHealth>();

                if (health != null)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);

                    if (dist <= 1.5f)
                    {
                        health.Kill();
                        Debug.Log("도살 성공");
                        return;
                    }
                }
            }
        }

        // 기존 농사 처리
        Vector3Int tilePos = farmTilemap.WorldToCell(mouseWorldPos);
        tilePos.z = 0;

        if (Vector3.Distance(transform.position, farmTilemap.CellToWorld(tilePos)) <= 1.5f)
        {
            HandleFarmAction(tilePos);
        }
    }

    private void HandleFarmAction(Vector3Int tilePosition)
    {
        if (currentEquipment == "Hoe")
        {
            landTileManager.PlowSoil(tilePosition);
        }
        else if (currentEquipment == "Seeds")
        {
            landTileManager.PlantSeed(tilePosition);
        }
        else if (currentEquipment == "Water")
        {
            landTileManager.WaterTile(tilePosition);
        }
        else if (currentEquipment == "Harvest")
        {
            landTileManager.HarvestCrop(tilePosition);
        }
    }

    private void OnInventory()
    {
        if (inventory != null)
        {
            inventory.ToggleUI();
            Debug.Log($"인벤토리 상태: {inventory.isInventoryOpen}");
        }
    }

    // ★ 수정됨: 실제 인벤토리(Inventory.instance.quickSlots) 데이터를 기반으로 장비 변경
    private void Quickslot()
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

    // ★ 추가: 공통 슬롯 선택 로직
    private void SelectQuickSlot(int index)
    {
        if (Inventory.instance == null || Inventory.instance.quickSlots == null) return;
        if (index < 0 || index >= Inventory.instance.quickSlots.Length) return;

        selectedQuickSlotIndex = index;
        event_time = false;

        UpdateCurrentEquipment(); // 선택 직후 인벤토리 데이터를 읽어 장착 갱신
    }

    // ★ 추가: 인벤토리의 퀵슬롯 배열에서 현재 아이템 정보를 가져와 currentEquipment에 등록
    private void UpdateCurrentEquipment()
    {
        if (Inventory.instance == null || Inventory.instance.quickSlots == null) return;
        if (selectedQuickSlotIndex < 0 || selectedQuickSlotIndex >= Inventory.instance.quickSlots.Length) return;

        Item selectedItem = Inventory.instance.quickSlots[selectedQuickSlotIndex];

        if (selectedItem != null && selectedItem.data != null)
        {
            // CSV에 등록된 itemID (예: Hoe, Seeds, Water)를 currentEquipment로 설정
            currentEquipment = selectedItem.data.itemID;
            Debug.Log($"[퀵슬롯 {selectedQuickSlotIndex + 1}번] 장착됨: {currentEquipment}");
        }
        else
        {
            currentEquipment = ""; // 빈 칸이면 장착 해제
        }
    }

    private void TryDestroyNearestSpawnedObject()
    {
        if (objectSpawner == null || objectSpawner.spawnedObjects.Count == 0) return;

        GameObject closest = null;
        float minDist = 1.5f;

        for (int i = objectSpawner.spawnedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = objectSpawner.spawnedObjects[i];

            if (obj == null)
            {
                objectSpawner.spawnedObjects.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < minDist)
            {
                closest = obj;
                minDist = dist;
            }
        }

        if (closest != null)
        {
            objectSpawner.spawnedObjects.Remove(closest);
            Destroy(closest);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("sea") || collision.gameObject.CompareTag("farmTile"))
        {
            collidedObject = collision.gameObject;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject == collidedObject)
        {
            collidedObject = null;
            Debug.Log("충돌 객체 해제됨");
        }
    }
}