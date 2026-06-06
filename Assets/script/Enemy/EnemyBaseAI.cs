using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections; // 코루틴 연출을 위해 필수

public class EnemyBaseAI : MonoBehaviour
{
    public enum EnemyAnimType { BlendTree, SimpleAnimation }

    [Header("에셋 데이터베이스 설정")]
    public EnemyDataSO stats;

    [Header("추격 대상 설정")]
    public Transform targetTransform;

    [Header("순찰 시간 설정")]
    public float minWalkTime = 1.5f;
    public float maxWalkTime = 3.5f;
    public float minIdleTime = 1.0f;
    public float maxIdleTime = 4.0f;

    [Header("상황별 애니메이션 스타일 교체")]
    public EnemyAnimType wanderAnimStyle = EnemyAnimType.BlendTree;
    public EnemyAnimType chaseAnimStyle = EnemyAnimType.SimpleAnimation;

    // ========================================================
    // ★ [완벽 복원] 기존 AnimalHealth에 있던 진짜 드랍 필드들 이식
    // ========================================================
    [Header("드랍 아이템 설정 (기존 AnimalHealth 구조 완벽 통합)")]
    [Tooltip("월드 바닥에 생성될 공용 필드 아이템 프리팹")]
    public GameObject fieldItemPrefab;

    [System.Serializable]
    public class DropRule
    {
        public string itemID;
        public int minDrop = 1;
        public int maxDrop = 2;
        [Range(0f, 100f)]
        public float dropChance = 100f;
    }
    [Tooltip("이 동물/몬스터가 죽을 때 뱉을 아이템 목록")]
    public List<DropRule> dropRules = new List<DropRule>();

    [Header("타일맵 이동 제한 설정")]
    public List<Tilemap> walkableTilemaps;
    public List<Tilemap> blockedTilemaps;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    private Vector2 moveDir;
    private float lastAttackTime;
    private int currentHealth;
    private bool isInitialized = false;
    private bool isDead = false;

    private bool isMoving = false;
    private bool isRunning = false;
    private bool isChasing = false;

    private float stateTimer;
    private float targetStateTime;
    private bool isWanderingMove = false;
    private Coroutine flashCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // 플레이어 타겟 자동 추적 백업
        if (targetTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) targetTransform = playerObj.transform;
        }
    }

    void Start()
    {
        // 에셋 데이터가 정상적으로 꽂혀있다면 즉시 가동
        if (stats != null)
        {
            currentHealth = stats.maxHealth;
            isInitialized = true;

            // 데이터에 기입된 기본 타입에 맞춰 초기 스타일 세팅
            if (stats.animType == "SimpleAnimation")
            {
                wanderAnimStyle = EnemyAnimType.SimpleAnimation;
                chaseAnimStyle = EnemyAnimType.SimpleAnimation;
            }
            else if (stats.animType == "BlendTree")
            {
                wanderAnimStyle = EnemyAnimType.BlendTree;
                chaseAnimStyle = EnemyAnimType.BlendTree;
            }

            SwitchWanderState();
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] 인스펙터창에 EnemyDataSO 에셋(Stats)이 누락되어 AI가 구동되지 않습니다!");
        }
    }

    void Update()
    {
        if (!isInitialized || isDead) return;

        float distance = (targetTransform != null) ? Vector2.Distance(transform.position, targetTransform.position) : float.MaxValue;

        // 1단계: 선공 몹(isAggressive)이고 공격 범위 내 진입 시 공격
        if (stats.isAggressive && distance <= stats.attackRange)
        {
            isChasing = true;
            isMoving = false;
            isRunning = false;
            moveDir = Vector2.zero;
            TryAttack();
        }
        // 2단계: 선공 몹이고 감지 범위 내 진입 시 플레이어 추격 기동
        else if (stats.isAggressive && distance <= stats.detectRange)
        {
            isChasing = true;
            isMoving = true;
            isRunning = true;
            moveDir = (targetTransform.position - transform.position).normalized;
        }
        // 3단계: 비선공 동물이거나, 선공 범위 밖일 때 평화롭게 순찰
        else
        {
            if (isChasing)
            {
                isChasing = false;
                isRunning = false;
                SwitchWanderState();
            }
            HandleWandering();
        }

        UpdateAnimation();
    }

    void HandleWandering()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= targetStateTime)
        {
            isWanderingMove = !isWanderingMove;
            SwitchWanderState();
        }
        isMoving = isWanderingMove;
    }

    void SwitchWanderState()
    {
        stateTimer = 0f;

        if (isWanderingMove)
        {
            moveDir = Random.insideUnitCircle.normalized;
            targetStateTime = Random.Range(minWalkTime, maxWalkTime);
        }
        else
        {
            moveDir = Vector2.zero;
            targetStateTime = Random.Range(minIdleTime, maxIdleTime);
        }
    }

    void FixedUpdate()
    {
        if (!isInitialized || !isMoving || isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speedMod = (WeatherManager.Instance != null) ? WeatherManager.Instance.GetSpeedModifier() : 1.0f;
        float currentSpeed = isRunning ? stats.moveSpeed : (stats.moveSpeed * 0.5f);
        Vector2 nextPos = rb.position + moveDir * (currentSpeed * speedMod) * Time.fixedDeltaTime;

        if (CanMoveTo(nextPos))
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (!isRunning)
            {
                isWanderingMove = !isWanderingMove;
                SwitchWanderState();
            }
        }
    }

    void TryAttack()
    {
        if (Time.time >= lastAttackTime + stats.attackCooldown)
        {
            if (anim != null) anim.SetTrigger("Attack");
            lastAttackTime = Time.time;

            if (targetTransform != null)
            {
                PlayerMove player = targetTransform.GetComponent<PlayerMove>();
                if (player != null) player.TakeDamage(stats.attackDamage);
            }
        }
    }

    void UpdateAnimation()
    {
        if (anim == null || isDead) return;

        anim.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            EnemyAnimType currentStyle = isRunning ? chaseAnimStyle : wanderAnimStyle;

            if (currentStyle == EnemyAnimType.BlendTree)
            {
                float absX = Mathf.Abs(moveDir.x);
                float absY = Mathf.Abs(moveDir.y);

                if (absX > absY)
                {
                    anim.SetFloat("DirX", 1f);
                    anim.SetFloat("DirY", 0f);

                    Transform flipTarget = isRunning ? targetTransform : this.transform;
                    float targetX = isRunning ? flipTarget.position.x : (transform.position.x + moveDir.x);
                    spriteRenderer.flipX = isRunning ? (targetX < transform.position.x) : (moveDir.x > 0);
                }
                else
                {
                    spriteRenderer.flipX = false;
                    anim.SetFloat("DirX", 0f);
                    anim.SetFloat("DirY", moveDir.y);
                }
            }
            else if (currentStyle == EnemyAnimType.SimpleAnimation)
            {
                float targetX = isRunning ? targetTransform.position.x : (transform.position.x + moveDir.x);
                spriteRenderer.flipX = isRunning ? (targetX > transform.position.x) : (moveDir.x > 0);
            }
        }
    }

    public bool CanMoveTo(Vector2 worldPos)
    {
        foreach (var tilemap in blockedTilemaps)
            if (tilemap != null && tilemap.HasTile(tilemap.WorldToCell(worldPos))) return false;
        foreach (var tilemap in walkableTilemaps)
            if (tilemap != null && tilemap.HasTile(tilemap.WorldToCell(worldPos))) return true;
        return false;
    }

    // ========================================================
    // ★ [완벽 복원] 피격 시 실시간 체력 차감 및 빨간색 깜빡임 타격 연출
    // ========================================================
    public void TakeDamage(int damageAmount)
    {
        if (!isInitialized || isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} 피격 발생! 데미지: {damageAmount}, 남은 HP: {currentHealth}");

        if (anim != null) anim.SetTrigger("Hit");

        if (spriteRenderer != null)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRedCoroutine());
        }

        if (currentHealth <= 0)
        {
            Kill();
        }
    }

    private IEnumerator FlashRedCoroutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Kill()
    {
        if (isDead) return;
        isDead = true;

        DropItems(); // 기존 정교한 매니저 연동형 드랍 함수 실행
        Destroy(gameObject);
    }

    // ========================================================
    // ★ [완벽 복원] 기존 AnimalHealth 내부의 핵심 아이템 스폰 시스템 이식
    // ========================================================
    private void DropItems()
    {
        if (fieldItemPrefab == null)
        {
            Debug.LogError("[드랍 실패] fieldItemPrefab이 비어있습니다! 인스펙터창에 FieldItem 프리팹을 넣어주세요.");
            return;
        }

        if (ItemDataManager.instance == null)
        {
            Debug.LogError("[드랍 실패] 씬에 ItemDataManager 싱글턴 인스턴스가 없습니다.");
            return;
        }

        if (dropRules.Count == 0)
        {
            Debug.LogWarning("[드랍 경고] 인스펙터창의 Drop Rules 목록이 비어있습니다.");
            return;
        }

        foreach (var rule in dropRules)
        {
            if (Random.Range(0f, 100f) <= rule.dropChance)
            {
                int count = Random.Range(rule.minDrop, rule.maxDrop + 1);
                if (count <= 0) continue;

                // 유저님의 기존 ItemDataManager 시스템에서 데이터 추출
                ItemData data = ItemDataManager.instance.GetItemDataByID(rule.itemID);
                if (data != null)
                {
                    Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
                    GameObject droppedObj = Instantiate(fieldItemPrefab, dropPos, Quaternion.identity);

                    FieldItem fieldItem = droppedObj.GetComponent<FieldItem>();
                    if (fieldItem != null)
                    {
                        fieldItem.Setup(data, count); // 기존 셋업 방식 호출
                        Debug.Log($"[아이템 드랍 성공]: {data.displayName} {count}개");
                    }
                    else
                    {
                        Debug.LogError("[드랍 실패] 스폰된 프리팹에 FieldItem 스크립트 컴포넌트가 누락되었습니다!");
                    }
                }
                else
                {
                    Debug.LogError($"[드랍 실패] ItemDB에서 '{rule.itemID}'를 찾을 수 없습니다. 대소문자나 오타를 확인하세요!");
                }
            }
        }
    }
}