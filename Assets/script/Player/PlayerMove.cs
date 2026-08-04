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

    // 퀵슬롯 선택/장착 아이템 상태는 PlayerQuickSlot이 단일 소유자로 관리한다.
    private PlayerQuickSlot playerQuickSlot;

    [Header("UI & Effect")]
    public GameObject attackRangeIndicator;

    // --------------------------------------------------------
    // [회피 시스템 추가] 변수 선언
    // --------------------------------------------------------
    [Header("Dodge System")]
    public float dodgeSpeedMultiplier = 1.5f; // 평소보다 이동할 배수 (원하는 거리만큼 조정)
    public float dodgeDuration = 0.4f;        // 회피 지속 시간
    public float dodgeCooldown = 0.8f;        // 회피 종료 후 재사용까지 대기 시간
    public bool isDodging = false;            // 현재 회피 중인지 (무적 상태 판별)

    private float dodgeCooldownTimer = 0f;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        playerQuickSlot = GetComponent<PlayerQuickSlot>();
        event_time = false;
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (playerQuickSlot != null)
        {
            playerQuickSlot.OnEquippedChanged += UpdateAttackRangeIndicator;
        }
        UpdateAttackRangeIndicator();
    }

    private void OnDestroy()
    {
        if (playerQuickSlot != null)
        {
            playerQuickSlot.OnEquippedChanged -= UpdateAttackRangeIndicator;
        }
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && !event_time && !isDodging)
        {
            OnMouseClick();
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryDestroyNearestSpawnedObject();
        }

        // --------------------------------------------------------
        // [회피 시스템 추가] 스페이스바 입력 감지 (쿨타임 중에는 재발동 불가)
        // (Input System의 Action Map을 사용 중이시라면 OnDodge 등의 함수로 분리하셔도 좋습니다.)
        // --------------------------------------------------------
        if (dodgeCooldownTimer > 0f)
        {
            dodgeCooldownTimer -= Time.deltaTime;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isDodging && !event_time && dodgeCooldownTimer <= 0f)
        {
            dodgeCooldownTimer = dodgeCooldown;
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

        ItemData equipped = playerQuickSlot != null ? playerQuickSlot.currentEquippedItemData : null;

        if (equipped != null && equipped.equipSlot == EquipmentSlotType.Weapon)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorldPos, 0.3f);

            foreach (var hit in hits)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist > 1.5f) continue;

                int damage = equipped.atk;
                if (damage <= 0) damage = 1;

                TreeHealth tree = hit.GetComponent<TreeHealth>();
                if (tree != null)
                {
                    if (equipped.type == "Axe")
                    {
                        tree.TakeDamage(damage);
                        break;
                    }
                    else
                    {
                        Debug.Log("이 무기로는 나무를 벨 수 없습니다! 도끼가 필요합니다.");
                    }
                }
                StoneHealth stone = hit.GetComponent<StoneHealth>();
                if (stone != null)
                {
                    if (equipped.type == "Pick") // CSV에서 곡괭이의 type은 "Pick"
                    {
                        stone.TakeDamage(damage);
                        break; // 한 번에 하나의 돌만 타격
                    }
                    else
                    {
                        Debug.Log("이 도구로는 돌을 깰 수 없습니다! 곡괭이가 필요합니다.");
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
        ItemData equipped = playerQuickSlot != null ? playerQuickSlot.currentEquippedItemData : null;

        // 장착된 아이템 데이터가 없으면 무시
        if (equipped == null) return;

        // 아이템 DB(CSV)의 'type' 컬럼 데이터를 기준으로 분기
        string toolType = equipped.type;

        if (toolType == "Hoe")
        {
            landTileManager.PlowSoil(tilePosition);
        }
        else if (toolType == "WateringCan") // 기존 "Water"에서 CSV 데이터와 동일하게 수정
        {
            landTileManager.WaterTile(tilePosition);
        }
        else if (toolType == "Seed") // 추후 씨앗 아이템을 DB에 추가할 때 type을 "Seed"로 설정하세요
        {
            landTileManager.PlantSeed(tilePosition);
        }
        // 수확의 경우, 빈손(도구 없음)일 때 동작하게 하려면 별도의 조건 처리가 필요할 수 있습니다.
        else if (toolType == "Harvest")
        {
            landTileManager.HarvestCrop(tilePosition);
        }
    }

    private void OnInventory()
    {
        if (inventory != null)
        {
            inventory.ToggleUI();
        }
    }

    // PlayerQuickSlot이 관리하는 장착 아이템이 바뀔 때마다 호출되어 사거리 표시 UI를 갱신
    private void UpdateAttackRangeIndicator()
    {
        if (attackRangeIndicator == null) return;

        ItemData equipped = playerQuickSlot != null ? playerQuickSlot.currentEquippedItemData : null;
        bool isWeapon = equipped != null && equipped.equipSlot == EquipmentSlotType.Weapon;
        attackRangeIndicator.SetActive(isWeapon);
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

        // 장비창(Armor/Hat/Shoes/Accessory)에 장착된 방어구 def 합계만큼 피해 경감
        float defense = EquipmentManager.instance != null ? EquipmentManager.instance.GetTotalDefense() : 0f;
        damage = Mathf.Max(damage - defense, 0f);
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