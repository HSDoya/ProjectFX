using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;
using Kinnly;

public class PlayerMove : MonoBehaviour
{
    [Header("Player Stats")]
    public float maxHealth = 100;
    public float currentHealth;
    public bool isDead = false;

    public Vector2 inputVec;
    public float speed = 5f;
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    public Tilemap farmTilemap;
    public Tilemap waterTilemap;
    public landtiles landTileManager;
    private Coroutine flashCoroutine;

    public bool event_time;
    Animator anim;
    private GameObject collidedObject = null;

    [SerializeField] private Inventory inventory;
    [SerializeField] private ObjectSpawner objectSpawner;

    // ★ 수정: 게임 시작 시 빈손(-1)으로 시작하도록 초기값 설정
    public int selectedQuickSlotIndex = -1;

    private string currentEquipment = "";
    private ItemData currentEquippedItemData = null;

    [Header("UI & Effect")]
    public GameObject attackRangeIndicator;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        event_time = false;
    }

    private void Start()
    {
        if (inventory != null)
        {
            inventory.onItemChangedCallback += UpdateCurrentEquipment;
        }

        currentHealth = maxHealth;

        // ★ 수정: 시작 시 확실하게 빈손 처리 및 사거리 UI 끄기
        selectedQuickSlotIndex = -1;
        if (attackRangeIndicator != null)
        {
            attackRangeIndicator.SetActive(false);
        }
        UpdateCurrentEquipment();
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
        Quickslot();

        if (Mouse.current.leftButton.wasPressedThisFrame && !event_time)
        {
            OnMouseClick();
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryDestroyNearestSpawnedObject();
        }

        // ★ 수정: Update() 안에 있던 중복된 키보드 입력 for문 삭제 (Quickslot 함수로 통합)
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
        {
            float speedModifier = (WeatherManager.Instance != null) ? WeatherManager.Instance.GetSpeedModifier() : 1.0f;
            rigid.linearVelocity = inputVec * (speed * speedModifier);
        }
        else
        {
            rigid.linearVelocity = Vector2.zero;
        }
    }

    private void OnMouseClick()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0;

        if (currentEquippedItemData != null && currentEquippedItemData.equipSlot == EquipmentSlotType.Weapon)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorldPos, 0.3f);

            foreach (var hit in hits)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist > 1.5f) continue;

                int damage = currentEquippedItemData.atk;
                if (damage <= 0) damage = 1;

                AnimalHealth animal = hit.GetComponent<AnimalHealth>();
                if (animal != null)
                {
                    animal.TakeDamage(damage);
                    break;
                }

                TreeHealth tree = hit.GetComponent<TreeHealth>();
                if (tree != null)
                {
                    if (currentEquippedItemData.type == "Axe")
                    {
                        tree.TakeDamage(damage);
                        break;
                    }
                    else
                    {
                        Debug.Log("이 무기로는 나무를 벨 수 없습니다! 도끼가 필요합니다.");
                    }
                }

                EnemyBaseAI enemy = hit.GetComponent<EnemyBaseAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    Debug.Log($"[{enemy.name}]에게 무기 데미지 {damage}를 입혔습니다!");
                    break;
                }
            }
            return;
        }

        Vector3Int tilePos = farmTilemap.WorldToCell(mouseWorldPos);
        tilePos.z = 0;

        if (Vector3.Distance(transform.position, farmTilemap.CellToWorld(tilePos)) <= 1.5f)
        {
            HandleFarmAction(tilePos);
        }
    }

    private void HandleFarmAction(Vector3Int tilePosition)
    {
        if (currentEquipment == "Hoe") landTileManager.PlowSoil(tilePosition);
        else if (currentEquipment == "Seeds") landTileManager.PlantSeed(tilePosition);
        else if (currentEquipment == "Water") landTileManager.WaterTile(tilePosition);
        else if (currentEquipment == "Harvest") landTileManager.HarvestCrop(tilePosition);
    }

    private void OnInventory()
    {
        if (inventory != null)
        {
            inventory.ToggleUI();
        }
    }

    private void Quickslot()
    {
        // ★ 수정: 숫자 키를 누를 때 무조건 장착이 아니라 '토글(Toggle)' 되도록 변경
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleQuickSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleQuickSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleQuickSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleQuickSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleQuickSlot(4);

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

    // ★ 추가: 같은 키를 누르면 맨손(-1)으로 바꾸는 토글 전용 함수
    private void ToggleQuickSlot(int index)
    {
        if (selectedQuickSlotIndex == index)
        {
            selectedQuickSlotIndex = -1; // 이미 들고 있는 무기면 집어넣기
        }
        else
        {
            selectedQuickSlotIndex = index; // 다른 무기면 꺼내기
        }
        event_time = false;
        UpdateCurrentEquipment();
    }

    private void SelectQuickSlot(int index)
    {
        if (Inventory.instance == null || Inventory.instance.quickSlots == null) return;
        if (index < 0 || index >= Inventory.instance.quickSlots.Length) return;

        selectedQuickSlotIndex = index;
        event_time = false;
        UpdateCurrentEquipment();
    }

    private void UpdateCurrentEquipment()
    {
        if (Inventory.instance == null || Inventory.instance.quickSlots == null) return;

        // ★ 수정: 빈손(-1)일 때 강제로 사거리 UI를 끄고 초기화!
        if (selectedQuickSlotIndex < 0 || selectedQuickSlotIndex >= Inventory.instance.quickSlots.Length)
        {
            currentEquipment = "";
            currentEquippedItemData = null;
            if (attackRangeIndicator != null)
            {
                attackRangeIndicator.SetActive(false);
            }
            return;
        }

        Item selectedItem = Inventory.instance.quickSlots[selectedQuickSlotIndex];

        if (selectedItem != null && selectedItem.data != null)
        {
            currentEquipment = selectedItem.data.itemID;
            currentEquippedItemData = selectedItem.data;

            if (attackRangeIndicator != null)
            {
                bool isWeapon = (currentEquippedItemData.equipSlot == EquipmentSlotType.Weapon);
                attackRangeIndicator.SetActive(isWeapon);
            }
            Debug.Log($"[퀵슬롯 {selectedQuickSlotIndex + 1}번] 장착됨: {currentEquipment}");
        }
        else
        {
            currentEquipment = "";
            currentEquippedItemData = null;

            // ★ 수정: 빈 칸일 때도 강제로 사거리 UI 끄기
            if (attackRangeIndicator != null)
            {
                attackRangeIndicator.SetActive(false);
            }
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
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead || event_time) return;

        currentHealth -= damage;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashRedCoroutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashRedCoroutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        currentHealth = maxHealth;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        spriteRenderer.color = Color.white;
    }
}