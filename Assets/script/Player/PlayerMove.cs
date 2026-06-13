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

    // --------------------------------------------------------
    // [회피 시스템 추가] 변수 선언
    // --------------------------------------------------------
    [Header("Dodge System")]
    public float dodgeSpeedMultiplier = 2.0f; // 평소보다 이동할 배수 (원하는 거리만큼 조정)
    public float dodgeDuration = 0.4f;        // 회피 지속 시간
    public bool isDodging = false;            // 현재 회피 중인지 (무적 상태 판별)

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

        if (Mouse.current.leftButton.wasPressedThisFrame && !event_time && !isDodging)
        {
            OnMouseClick();
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryDestroyNearestSpawnedObject();
        }

        // --------------------------------------------------------
        // [회피 시스템 추가] 스페이스바 입력 감지
        // (Input System의 Action Map을 사용 중이시라면 OnDodge 등의 함수로 분리하셔도 좋습니다.)
        // --------------------------------------------------------
        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isDodging && !event_time)
        {
            StartCoroutine(DodgeRoutine());
        }
    }

    private void LateUpdate()
    {
        if (isDodging) return; // [회피 시스템 추가] 회피 중에는 애니메이션 속도나 방향 전환 고정

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
        if (isDodging) return; // [회피 시스템 추가] 회피 중에는 코루틴에서 속도를 제어함

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

    // --------------------------------------------------------
    // [회피 시스템 추가] 회피 코루틴
    // --------------------------------------------------------
    private IEnumerator DodgeRoutine()
    {
        isDodging = true;

        // 회피 방향 결정 (가만히 서있을 때는 현재 바라보는 방향, 이동 중일 때는 이동 방향)
        Vector2 dodgeDir = inputVec;
        if (dodgeDir == Vector2.zero)
        {
            dodgeDir = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        }
        dodgeDir.Normalize();

        float timer = 0f;
        float currentAngle = 0f;
        float targetAngle = -360f; // 시계 방향 회전 (-360도)

        while (timer < dodgeDuration)
        {
            timer += Time.deltaTime;

            // 1. 회피 이동 (평소 속도 * 배수)
            rigid.linearVelocity = dodgeDir * (speed * dodgeSpeedMultiplier);

            // 2. 시계방향 회전
            float angleStep = (targetAngle / dodgeDuration) * Time.deltaTime;
            currentAngle += angleStep;

            // 주의: 스프라이트만 회전시킬지 전체를 회전시킬지 결정해야 합니다.
            // 여기서는 충돌체 등도 함께 회전해도 무방하다고 가정하여 본체를 회전시킵니다.
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);

            yield return null;
        }

        // 회피 종료 후 상태 초기화
        transform.rotation = Quaternion.identity; // 회전 0도로 복구
        rigid.linearVelocity = Vector2.zero;      // 관성 제거
        isDodging = false;
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

    private void ToggleQuickSlot(int index)
    {
        if (selectedQuickSlotIndex == index)
        {
            selectedQuickSlotIndex = -1;
        }
        else
        {
            selectedQuickSlotIndex = index;
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
        // --------------------------------------------------------
        // [회피 시스템 추가] isDodging 상태일 때 무적 판정 부여
        // --------------------------------------------------------
        if (isDead || event_time || isDodging) return;

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