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
    private Collider2D col;
    public Tilemap farmTilemap;
    public Tilemap waterTilemap;
    public landtiles landTileManager;
    private Coroutine hitFeedbackCoroutine;

    public bool event_time;
    Animator anim;
    private GameObject collidedObject = null;

    // TODO(임시): 가구 설치 테스트용 무적. 테스트가 끝나면 이 필드와 아래 TakeDamage의 조건을 지울 것.
    [Header("디버그 (테스트용 - 정식 빌드 전에 반드시 끌 것)")]
    public bool debugInvincible = false;

    [Header("피격 반응 설정")]
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.15f;
    [Tooltip("최초 피격 후 추가 피격을 무시하는 시간. 여러 몹에게 둘러싸였을 때 순식간에 녹지 않게 해준다.")]
    public float hitInvulnerabilityDuration = 1f;
    [Tooltip("맞은 직후 빨갛게 보이는 시간. 이후 무적이 끝날 때까지는 반투명으로 표시된다.")]
    public float hitFlashDuration = 0.15f;
    [Range(0f, 1f)]
    [Tooltip("무적 중 플레이어 스프라이트의 불투명도")]
    public float invulnerableAlpha = 0.45f;
    private bool isKnockedBack = false;
    private bool isHitInvulnerable = false;

    [Header("공격 판정 설정")]
    public float attackHitRadius = 0.6f;
    [Tooltip("무기 사용 간격(공격 속도). 작을수록 빠르게 휘두른다.")]
    public float attackCooldown = 0.5f;
    [Tooltip("농기구 사용 간격")]
    public float toolCooldown = 0.4f;

    // 무기/도구가 쿨타임을 공유한다(무기와 도구를 번갈아 바꿔서 연타하는 것도 함께 막힌다).
    private float actionCooldownTimer = 0f;
    private float actionCooldownDuration = 0f;

    // 쿨타임 UI가 읽어가는 남은 비율. 1이면 방금 사용, 0이면 사용 가능.
    // 상태는 여기서만 들고 UI는 매 프레임 읽기만 한다(스태미너/체력과 동일한 방식).
    public float ActionCooldownRatio01 =>
        (actionCooldownDuration > 0f && actionCooldownTimer > 0f)
            ? Mathf.Clamp01(actionCooldownTimer / actionCooldownDuration)
            : 0f;

    [Header("사망 / 리스폰 설정")]
    public float respawnDelay = 5f;

    [Header("가구 설치 설정")]
    public float furniturePlacementRange = 2.5f;
    public Color placementValidColor = new Color(0.2f, 1f, 0.3f, 0.35f);
    public Color placementInvalidColor = new Color(1f, 0.2f, 0.2f, 0.35f);

    // 설치 미리보기는 밭 커서(tileHighlight)와 같은 오브젝트를 색/크기만 바꿔서 재사용한다.
    // 테두리만 있는 밭 커서와 달리 설치 구역은 면으로 보여야 해서 스프라이트만 따로 만들어 둔다.
    private Sprite highlightBorderSprite;
    private Sprite highlightAreaSprite;

    // 캐릭터/장착 무기가 공통으로 참조하는 시점 기준(마우스 포인터). PlayerQuickSlot은 이 값을 읽기만 한다.
    public bool IsFacingRight { get; private set; } = true;

    [SerializeField] private Inventory inventory;
    [SerializeField] private ObjectSpawner objectSpawner;
    [SerializeField] private CraftingUI craftingUI;

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

    // --------------------------------------------------------
    // [스태미너 시스템 추가] Shift 달리기 + 회피가 공유하는 스태미너
    // --------------------------------------------------------
    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float runSpeedMultiplier = 1.5f;       // 달리기 시 이동 속도 배율
    public float runStaminaDrainPerSecond = 15f;  // 달리는 동안 초당 소모량
    public float dodgeStaminaCost = 20f;          // 회피 1회당 소모량
    [Tooltip("무기(검/도끼/곡괭이) 1회 사용 스태미너")]
    public float attackStaminaCost = 8f;
    [Tooltip("농기구(괭이/물뿌리개/씨앗) 1회 사용 스태미너")]
    public float toolStaminaCost = 5f;
    public float staminaRegenPerSecond = 10f;     // 회복 속도(달리기/회피를 안 쓸 때)
    public float staminaRegenDelay = 1.5f;        // 마지막 소모 후 회복이 시작되기까지 대기 시간

    private bool isRunning = false;
    private float staminaRegenTimer = 0f;

    // Shift가 "지금 눌려있는지"는 Run 액션의 현재 상태를 매 프레임 직접 조회해서 판단한다.
    // (OnRun(InputValue) 콜백으로 press/release를 따로 캐싱하는 방식은 이벤트가 씹히면
    // isRunKeyHeld가 true로 눌러붙어 Shift를 떼도 계속 달리는 버그가 생길 수 있어 제거했다.)
    private InputAction runAction;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        playerQuickSlot = GetComponent<PlayerQuickSlot>();
        event_time = false;
        CreateTileHighlight();

        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) runAction = playerInput.actions["Run"];
    }

    private void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;

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

        if (Mouse.current.leftButton.wasPressedThisFrame && !event_time && !isDodging && !isDead)
        {
            OnMouseClick();
        }

        // F는 설치한 가구 회수를 먼저 시도하고, 주변에 가구가 없을 때만 기존 오브젝트 제거로 넘어간다.
        if (Keyboard.current.fKey.wasPressedThisFrame && !isDead)
        {
            if (!TryPickUpFurniture()) TryDestroyNearestSpawnedObject();
        }

        // --------------------------------------------------------
        // [회피 시스템 추가] 회피 쿨타임 감소 (발동 자체는 Input System의 OnDash로 옮김)
        // --------------------------------------------------------
        if (dodgeCooldownTimer > 0f)
        {
            dodgeCooldownTimer -= Time.deltaTime;
        }

        if (actionCooldownTimer > 0f)
        {
            actionCooldownTimer -= Time.deltaTime;
        }

        UpdateStamina();

        // ESC로 인벤토리/제작창 닫기 (열려 있을 때만 호출 - 안 그러면 닫혀 있을 때 ESC로 오히려 열림)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (inventory != null && inventory.isInventoryOpen) inventory.ToggleUI();
            if (craftingUI != null && craftingUI.IsOpen) craftingUI.Close();
        }

        // E로 제작창 토글 (테스트용 키 바인딩 - Input Actions 에셋에 정식으로 옮겨도 됨)
        if (Keyboard.current.eKey.wasPressedThisFrame && craftingUI != null)
        {
            craftingUI.ToggleUI();
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

    // [회피 시스템 추가] 회피 발동을 Input System으로 이전. Send Messages 방식은 키를 뗄 때도
    // 이 콜백을 호출하므로(isPressed=false), 눌리는 순간에만 반응하도록 가드가 반드시 필요하다.
    private void OnDash(InputValue value)
    {
        if (!value.isPressed) return;
        if (isDodging || event_time || isDead || dodgeCooldownTimer > 0f) return;
        if (currentStamina < dodgeStaminaCost)
        {
            Debug.Log("스태미너가 부족해 회피할 수 없습니다.");
            return;
        }

        currentStamina -= dodgeStaminaCost;
        staminaRegenTimer = staminaRegenDelay;
        dodgeCooldownTimer = dodgeCooldown;
        StartCoroutine(DodgeRoutine());
    }

    // [스태미너 시스템 추가] 달리기 상태 판정 + 소모/회복을 한 곳에서 처리.
    // isRunning에 !isDodging을 포함시켜두면 회피 중엔 자동으로 달리기가 멈추고,
    // 회피가 끝나면 Shift가 눌려있는 한 별도 처리 없이 다시 달리기로 돌아온다.
    private void UpdateStamina()
    {
        bool isRunKeyHeld = runAction != null && runAction.IsPressed();
        bool wantsToRun = isRunKeyHeld && inputVec.sqrMagnitude > 0.0001f && !isDodging && !event_time;

        if (wantsToRun && currentStamina > 0f)
        {
            isRunning = true;
            currentStamina = Mathf.Max(0f, currentStamina - runStaminaDrainPerSecond * Time.deltaTime);
            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            isRunning = false;
            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= Time.deltaTime;
            }
            else
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenPerSecond * Time.deltaTime);
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDodging || isKnockedBack || isDead) return; // 회피/넉백/사망(리스폰 대기) 중에는 이동 로직을 건너뜀

        if (!event_time)
        {
            float speedModifier = (WeatherManager.Instance != null) ? WeatherManager.Instance.GetSpeedModifier() : 1.0f;
            float runModifier = isRunning ? runSpeedMultiplier : 1f;
            rigid.linearVelocity = inputVec * (speed * speedModifier * runModifier);
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

        if (equipped != null && equipped.itemType == ItemType.Furniture && equipped.placedPrefab != null)
        {
            TryPlaceFurniture(equipped);
            return;
        }

        if (equipped != null && equipped.equipSlot == EquipmentSlotType.Weapon)
        {
            if (!TryConsumeActionCost(attackStaminaCost, attackCooldown)) return;
            if (playerQuickSlot != null) playerQuickSlot.PlaySwing();

            Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorldPos, attackHitRadius);

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

        // 장착한 도구가 없으면 농사 동작 자체가 없으므로(HandleFarmAction이 바로 반환) 스태미너도 소모하지 않는다.
        if (equipped != null && TryGetTargetedTile(out Vector3Int tilePos))
        {
            if (!TryConsumeActionCost(toolStaminaCost, toolCooldown)) return;
            if (playerQuickSlot != null) playerQuickSlot.PlaySwing();

            HandleFarmAction(tilePos);
        }
    }

    // 행동 1회에 필요한 쿨타임과 스태미너를 함께 검사하고 소모한다.
    // 둘 중 하나라도 부족하면 아무것도 소모하지 않고 false를 반환한다(일부만 깎이는 상황 방지).
    private bool TryConsumeActionCost(float staminaCost, float cooldown)
    {
        if (actionCooldownTimer > 0f) return false;

        if (currentStamina < staminaCost)
        {
            Debug.Log("스태미너가 부족해 행동할 수 없습니다.");
            return false;
        }

        currentStamina -= staminaCost;
        staminaRegenTimer = staminaRegenDelay; // 회피와 동일하게, 소모 직후에는 회복을 잠시 지연
        actionCooldownTimer = cooldown;
        actionCooldownDuration = cooldown;
        return true;
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
    // 가구를 들고 있을 때는 같은 커서를 설치 구역 미리보기로 바꿔서 쓴다.
    private void UpdateTileHighlight()
    {
        if (tileHighlight == null || farmTilemap == null || landTileManager == null) return;

        ItemData equipped = playerQuickSlot != null ? playerQuickSlot.currentEquippedItemData : null;
        if (!isDead && equipped != null && equipped.itemType == ItemType.Furniture && equipped.placedPrefab != null)
        {
            UpdateFurniturePreview(equipped);
            return;
        }

        tileHighlight.sprite = highlightBorderSprite;
        tileHighlight.color = Color.white;
        tileHighlight.transform.localScale = farmTilemap.cellSize;

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

        highlightBorderSprite = CreateHighlightSprite(0f);
        highlightAreaSprite = CreateHighlightSprite(0.45f);

        tileHighlight.sprite = highlightBorderSprite;
        tileHighlight.sortingOrder = 10; // farmTilemap/cropTilemap보다 위에 그려지도록
        tileHighlight.enabled = false;

        Vector3 cellSize = farmTilemap != null ? farmTilemap.cellSize : Vector3.one;
        go.transform.localScale = cellSize;
    }

    // fillAlpha가 0이면 테두리만(밭 커서), 0보다 크면 안쪽까지 채운 구역 표시(가구 설치 미리보기)가 된다.
    private Sprite CreateHighlightSprite(float fillAlpha)
    {
        const int size = 32;
        const int border = 3;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color line = new Color(1f, 1f, 1f, 0.9f);
        Color fill = new Color(1f, 1f, 1f, fillAlpha);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < border || x >= size - border || y < border || y >= size - border;
                tex.SetPixel(x, y, isBorder ? line : fill);
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // 가구를 들고 있는 동안 마우스 위치에 설치 구역을 띄운다. 설치 가능하면 초록, 불가능하면 빨강.
    private void UpdateFurniturePreview(ItemData furniture)
    {
        Vector2 center = GetMouseWorldPosition();
        Vector2 footprint = GetFootprintSize(furniture);

        tileHighlight.sprite = highlightAreaSprite;
        tileHighlight.transform.position = center;
        tileHighlight.transform.localScale = new Vector3(footprint.x, footprint.y, 1f);
        tileHighlight.color = CanPlaceFurniture(center, footprint) ? placementValidColor : placementInvalidColor;
        tileHighlight.enabled = true;
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }

    private static Vector2 GetFootprintSize(ItemData furniture)
    {
        var placeable = furniture.placedPrefab.GetComponent<PlaceableFurniture>();
        return placeable != null ? placeable.footprintSize : Vector2.one;
    }

    // 설치하려는 구역이 비어 있는지 검사. 물 타일 위나 다른 오브젝트와 겹치는 자리에는 설치할 수 없다.
    // 플레이어 자신도 겹침 대상에 포함한다(제 자리에 설치해서 가구 안에 갇히는 것을 막기 위함).
    private bool CanPlaceFurniture(Vector2 center, Vector2 footprint)
    {
        if (Vector2.Distance(transform.position, center) > furniturePlacementRange) return false;

        if (IsFootprintOverWater(center, footprint)) return false;

        foreach (var hit in Physics2D.OverlapBoxAll(center, footprint, 0f))
        {
            if (hit == null || hit.isTrigger) continue;
            // 땅/벽 타일맵 콜라이더는 바닥이므로 겹쳐도 설치를 막지 않는다(물은 위에서 따로 검사함).
            if (hit.GetComponentInParent<Tilemap>() != null) continue;
            return false;
        }

        return true;
    }

    // 중심만 보면 가구의 절반이 물에 걸친 자리도 통과하므로 네 모서리까지 함께 검사한다.
    private bool IsFootprintOverWater(Vector2 center, Vector2 footprint)
    {
        if (waterTilemap == null) return false;

        Vector2 half = footprint * 0.5f;
        Vector2[] points =
        {
            center,
            center + new Vector2(-half.x, -half.y),
            center + new Vector2(half.x, -half.y),
            center + new Vector2(-half.x, half.y),
            center + new Vector2(half.x, half.y)
        };

        foreach (var point in points)
        {
            if (waterTilemap.HasTile(waterTilemap.WorldToCell(point))) return true;
        }

        return false;
    }

    private void TryPlaceFurniture(ItemData furniture)
    {
        Vector2 center = GetMouseWorldPosition();
        Vector2 footprint = GetFootprintSize(furniture);

        if (!CanPlaceFurniture(center, footprint)) return;
        if (Inventory.instance == null || playerQuickSlot == null) return;

        // 인벤토리에서 먼저 빼내고, 실제로 빠졌을 때만 설치한다.
        // 설치부터 하면 슬롯이 이미 비어 있어도 가구가 공짜로 생긴다.
        if (!Inventory.instance.TryTakeOneAt(playerQuickSlot.selectedQuickSlotIndex, true, out Item taken)) return;

        // 슬롯 상태가 어긋나 엉뚱한 아이템이 빠졌다면 되돌린다(다른 아이템이 소모되는 것을 방지).
        if (taken == null || taken.data != furniture)
        {
            if (taken != null) Inventory.instance.AddItem(taken);
            return;
        }

        GameObject placed = Instantiate(furniture.placedPrefab, center, Quaternion.identity);

        // 회수할 때 어떤 아이템으로 돌려줄지 설치한 쪽에서 알려준다.
        var placeable = placed.GetComponent<PlaceableFurniture>();
        if (placeable != null) placeable.sourceItem = furniture;
    }

    // 주변에 설치된 가구가 있으면 회수해서 인벤토리로 되돌린다. 성공하면 true.
    private bool TryPickUpFurniture()
    {
        PlaceableFurniture closest = null;
        float minDist = furniturePlacementRange;

        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, furniturePlacementRange))
        {
            var furniture = hit.GetComponentInParent<PlaceableFurniture>();
            if (furniture == null || furniture.sourceItem == null) continue;

            float dist = Vector2.Distance(transform.position, furniture.transform.position);
            if (dist <= minDist)
            {
                closest = furniture;
                minDist = dist;
            }
        }

        if (closest == null) return false;

        // 인벤토리가 가득 차면 가구를 그대로 둔다(넣지도 못하고 사라지면 아이템이 증발하므로).
        if (Inventory.instance == null || !Inventory.instance.AddItem(new Item(closest.sourceItem, 1))) return false;

        Destroy(closest.gameObject);
        return true;
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

    public void TakeDamage(float damage, Vector2? sourcePosition = null)
    {
        // --------------------------------------------------------
        // [회피 시스템 추가] isDodging 상태일 때 무적 판정 부여
        // isHitInvulnerable: 피격 직후 짧은 무적 시간(중복 피격 방지)
        // --------------------------------------------------------
        if (debugInvincible) return;
        if (isDead || event_time || isDodging || isHitInvulnerable) return;

        // 장비창(Armor/Hat/Shoes/Accessory)에 장착된 방어구 def 합계만큼 피해 경감
        float defense = EquipmentManager.instance != null ? EquipmentManager.instance.GetTotalDefense() : 0f;
        damage = Mathf.Max(damage - defense, 0f);
        currentHealth -= damage;

        if (hitFeedbackCoroutine != null)
        {
            StopCoroutine(hitFeedbackCoroutine);
        }
        hitFeedbackCoroutine = StartCoroutine(HitFeedbackRoutine());

        if (sourcePosition.HasValue)
        {
            Vector2 dir = (Vector2)transform.position - sourcePosition.Value;
            if (dir.sqrMagnitude > 0.0001f) StartCoroutine(KnockbackRoutine(dir.normalized));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 무적 플래그와 스프라이트 색을 한 코루틴이 모두 소유한다. 예전엔 색(FlashRed)과 무적
    // (HitInvulnerability)을 별도 코루틴으로 돌려서, 둘의 길이가 어긋나면 무적인데 평상시 색으로
    // 보이는(= 플레이어가 무적인지 알 수 없는) 상태가 생겼다.
    private IEnumerator HitFeedbackRoutine()
    {
        isHitInvulnerable = true;

        // 맞은 순간은 빨갛게
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(hitFlashDuration);

        // 남은 무적 시간 동안은 반투명 - 지금 무적이라는 걸 눈으로 알 수 있게
        float translucentTime = hitInvulnerabilityDuration - hitFlashDuration;
        if (translucentTime > 0f)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, invulnerableAlpha);
            yield return new WaitForSeconds(translucentTime);
        }

        spriteRenderer.color = Color.white;
        isHitInvulnerable = false;
        hitFeedbackCoroutine = null;
    }

    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        isKnockedBack = true;
        rigid.linearVelocity = direction * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);
        if (isKnockedBack) rigid.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 피격 연출 코루틴을 중간에 끊으므로, 그 코루틴이 끝에서 되돌려놓던 색과 무적 플래그를
        // 여기서 직접 정리한다(안 하면 무적 상태로 눌러붙은 채 부활한다).
        if (hitFeedbackCoroutine != null)
        {
            StopCoroutine(hitFeedbackCoroutine);
            hitFeedbackCoroutine = null;
        }
        spriteRenderer.color = Color.white;
        isHitInvulnerable = false;

        isKnockedBack = false;
        rigid.linearVelocity = Vector2.zero;

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        currentHealth = maxHealth;
        spriteRenderer.enabled = true;
        if (col != null) col.enabled = true;
        isDead = false;
    }
}