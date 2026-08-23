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

    // 캐릭터/장착 무기가 공통으로 참조하는 시점 기준(마우스 포인터). PlayerQuickSlot은 이 값을 읽기만 한다.
    public bool IsFacingRight { get; private set; } = true;

    [SerializeField] private Inventory inventory;
    [SerializeField] private ObjectSpawner objectSpawner;

    // 퀵슬롯 선택/장착 아이템 상태는 PlayerQuickSlot이 단일 소유자로 관리한다.
    private PlayerQuickSlot playerQuickSlot;

    [Header("UI & Effect")]
    public GameObject attackRangeIndicator;

    // 마우스가 가리키는 밭 타일을 테두리로 표시해주는 커서. 별도 스프라이트 에셋 없이 코드로 생성한다.
    private SpriteRenderer tileHighlight;

    // --------------------------------------------------------
    // [회피 시스템 추가] 변수 선언
    // --------------------------------------------------------
    [Header("Dodge System")]
    public float dodgeSpeedMultiplier = 1.1f; // 평소보다 이동할 배수 (원하는 거리만큼 조정)
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
        CreateTileHighlight();
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
        UpdateTileHighlight();

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

        // ESC로 인벤토리 닫기 (열려 있을 때만 ToggleUI 호출 - 안 그러면 닫혀 있을 때 ESC로 오히려 열림)
        if (Keyboard.current.escapeKey.wasPressedThisFrame && inventory != null && inventory.isInventoryOpen)
        {
            inventory.ToggleUI();
        }
    }

    private void LateUpdate()
    {
        if (isDodging) return; // [회피 시스템 추가] 회피 중에는 애니메이션 속도나 방향 전환 고정

        anim.SetFloat("Speed", inputVec.magnitude);

        // 시점은 이동 방향이 아니라 마우스 포인터 기준 (제자리에서도 마우스를 보고 즉시 돌아본다)
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        IsFacingRight = mouseWorldPos.x >= transform.position.x;
        spriteRenderer.flipX = !IsFacingRight;
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

        if (TryGetTargetedTile(out Vector3Int tilePos))
        {
            HandleFarmAction(tilePos);
        }
    }

    // 마우스가 가리키는 칸과, 그 칸 중심까지 플레이어가 상호작용 가능한 거리 안에 있는지를 함께 반환.
    // CellToWorld는 칸의 중심이 아니라 모서리 좌표를 반환하므로, 반드시 GetCellCenterWorld로 거리를 재야
    // 어느 방향에서 접근하든 판정 거리가 일관된다(모서리 기준이면 접근 방향에 따라 들쭉날쭉해짐).
    private bool TryGetTargetedTile(out Vector3Int tilePos)
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0;

        tilePos = farmTilemap.WorldToCell(mouseWorldPos);
        tilePos.z = 0;

        return Vector3.Distance(transform.position, farmTilemap.GetCellCenterWorld(tilePos)) <= 1.5f;
    }

    // 마우스가 가리키는 칸에 밭 타일이 있고 상호작용 범위 안이면, 그 칸 중심에 테두리 커서를 띄운다.
    private void UpdateTileHighlight()
    {
        if (tileHighlight == null || farmTilemap == null || landTileManager == null) return;

        if (TryGetTargetedTile(out Vector3Int tilePos) && landTileManager.HasFarmTile(tilePos))
        {
            tileHighlight.transform.position = farmTilemap.GetCellCenterWorld(tilePos);
            tileHighlight.enabled = true;
        }
        else
        {
            tileHighlight.enabled = false;
        }
    }

    // 별도 스프라이트 에셋 없이, 코드로 정사각형 테두리 스프라이트를 생성해 커서로 사용한다.
    private void CreateTileHighlight()
    {
        GameObject go = new GameObject("TileHighlight");
        tileHighlight = go.AddComponent<SpriteRenderer>();
        tileHighlight.sprite = CreateHighlightSprite();
        tileHighlight.sortingOrder = 10; // farmTilemap/cropTilemap보다 위에 그려지도록
        tileHighlight.enabled = false;

        Vector3 cellSize = farmTilemap != null ? farmTilemap.cellSize : Vector3.one;
        go.transform.localScale = cellSize;
    }

    private Sprite CreateHighlightSprite()
    {
        const int size = 32;
        const int border = 3;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color line = new Color(1f, 1f, 0.2f, 0.9f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < border || x >= size - border || y < border || y >= size - border;
                tex.SetPixel(x, y, isBorder ? line : clear);
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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
            // TODO(임시): 아직 낫(Sickle) 아이템이 없어서 호미가 갈기/수확을 겸함.
            // 낫 아이템을 DB에 type="Harvest"로 추가하면, 아래 IsHarvestable 분기를 지우고
            // 이 아래 else if (toolType == "Harvest") 분기로 수확을 옮길 것.
            if (landTileManager.IsHarvestable(tilePosition))
                landTileManager.HarvestCrop(tilePosition);
            else
                landTileManager.PlowSoil(tilePosition);
        }
        else if (toolType == "WateringCan") // 기존 "Water"에서 CSV 데이터와 동일하게 수정
        {
            landTileManager.WaterTile(tilePosition);
        }
        else if (toolType == "Seed") // 씨앗 아이템의 type은 "Seed"로 설정
        {
            // 심기에 성공했을 때만 인벤토리에서 씨앗 1개를 소모한다.
            if (landTileManager.PlantSeed(tilePosition, equipped) && Inventory.instance != null)
            {
                Inventory.instance.TryTakeOneAt(playerQuickSlot.selectedQuickSlotIndex, true, out _);
            }
        }
        // 낫 등 수확 전용 도구가 추가되면 여기서 처리 (현재는 위 Hoe 분기가 임시로 대신함)
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