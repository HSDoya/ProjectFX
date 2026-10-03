using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;

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

    [Header("가축 피격 도망 설정")]
    public float panicDuration = 3.0f;
    public float fleeSpeedMultiplier = 1.6f;

    [Header("상황별 애니메이션 스타일 교체")]
    public EnemyAnimType wanderAnimStyle = EnemyAnimType.BlendTree;
    public EnemyAnimType chaseAnimStyle = EnemyAnimType.SimpleAnimation;

    [Header("드랍 아이템 설정")]
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
    private Collider2D col;

    private Vector2 moveDir;
    private float lastAttackTime;
    private int currentHealth;
    private bool isInitialized = false;
    private bool isDead = false;

    private bool isMoving = false;
    private bool isRunning = false;
    private bool isChasing = false;

    private bool isFleeing = false;
    private float fleeTimer = 0f;

    private float stateTimer;
    private float targetStateTime;
    private bool isWanderingMove = false;
    private Coroutine flashCoroutine;

    // ★ [추가]: 애니메이터 파라미터 유무 사전 캐싱 (없는 파라미터 호출 에러 원천 방지)
    private bool hasParamIsMoving = false;
    private bool hasParamIsFleeing = false;
    private bool hasParamDirX = false;
    private bool hasParamDirY = false;
    private bool hasParamHit = false;
    private bool hasParamAttack = false;
    private bool hasParamDie = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (col != null)
        {
            col.enabled = true;
        }

        if (targetTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) targetTransform = playerObj.transform;
        }

        // ★ [추가]: 이 몹/가축의 애니메이터에 등록된 파라미터 목록 검사
        CacheAnimatorParameters();
    }

    void CacheAnimatorParameters()
    {
        if (anim == null) return;

        foreach (var p in anim.parameters)
        {
            if (p.name == "IsMoving") hasParamIsMoving = true;
            else if (p.name == "IsFleeing") hasParamIsFleeing = true;
            else if (p.name == "DirX") hasParamDirX = true;
            else if (p.name == "DirY") hasParamDirY = true;
            else if (p.name == "Hit") hasParamHit = true;
            else if (p.name == "Attack") hasParamAttack = true;
            else if (p.name == "Die") hasParamDie = true;
        }
    }

    void Start()
    {
        if (stats != null)
        {
            currentHealth = stats.maxHealth;
            isInitialized = true;

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

        // 1. 가축 피격 도망 처리
        if (isFleeing)
        {
            fleeTimer -= Time.deltaTime;
            if (fleeTimer <= 0f)
            {
                isFleeing = false;
                isRunning = false;
                SwitchWanderState();
            }
            else
            {
                isMoving = true;
                isRunning = true;
                if (targetTransform != null)
                {
                    // 플레이어의 반대 방향으로 도망
                    moveDir = ((Vector2)transform.position - (Vector2)targetTransform.position).normalized;
                }
            }
        }
        // 2. 선공 몹 전투 및 추격
        else if (stats.isAggressive)
        {
            float distance = (targetTransform != null) ? Vector2.Distance(transform.position, targetTransform.position) : float.MaxValue;

            if (distance <= stats.attackRange)
            {
                isChasing = true;
                isMoving = false;
                isRunning = false;
                moveDir = Vector2.zero;
                TryAttack();
            }
            else if (distance <= stats.detectRange)
            {
                isChasing = true;
                isMoving = true;
                isRunning = true;
                moveDir = ((Vector2)targetTransform.position - (Vector2)transform.position).normalized;
            }
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
        }
        // 3. 평소 순찰
        else
        {
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

        float currentSpeed;
        if (isFleeing)
        {
            currentSpeed = stats.moveSpeed * fleeSpeedMultiplier;
        }
        else
        {
            currentSpeed = isRunning ? stats.moveSpeed : (stats.moveSpeed * 0.5f);
        }

        Vector2 nextPos = rb.position + moveDir * (currentSpeed * speedMod) * Time.fixedDeltaTime;

        if (CanMoveTo(nextPos))
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (isFleeing)
            {
                // 벽에 막히면 옆 방향으로 탈출
                moveDir = Vector2.Perpendicular(moveDir) * (Random.value > 0.5f ? 1f : -1f);
            }
            else if (!isRunning)
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
            if (anim != null && hasParamAttack) anim.SetTrigger("Attack");
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

        // 파라미터가 있을 때만 안전하게 전달
        if (hasParamIsMoving) anim.SetBool("IsMoving", isMoving);
        if (hasParamIsFleeing) anim.SetBool("IsFleeing", isFleeing);

        if (isMoving)
        {
            EnemyAnimType currentStyle = (isRunning || isFleeing) ? chaseAnimStyle : wanderAnimStyle;

            if (currentStyle == EnemyAnimType.BlendTree)
            {
                float absX = Mathf.Abs(moveDir.x);
                float absY = Mathf.Abs(moveDir.y);

                if (absX > absY)
                {
                    if (hasParamDirX) anim.SetFloat("DirX", 1f);
                    if (hasParamDirY) anim.SetFloat("DirY", 0f);

                    // ★ [수정]: 플레이어를 추격할 때만 플레이어 위치를 보고, 도망치거나 순찰할 때는 무조건 진행 방향(moveDir.x)을 응시
                    if (isRunning && !isFleeing && targetTransform != null)
                    {
                        spriteRenderer.flipX = targetTransform.position.x < transform.position.x;
                    }
                    else
                    {
                        // 원본 스프라이트가 우측 기준일 때: 왼쪽으로 가면 flipX = true, 오른쪽으로 가면 flipX = false
                        spriteRenderer.flipX = moveDir.x < 0f;
                    }
                }
                else
                {
                    spriteRenderer.flipX = false;
                    if (hasParamDirX) anim.SetFloat("DirX", 0f);
                    if (hasParamDirY) anim.SetFloat("DirY", moveDir.y);
                }
            }
            else if (currentStyle == EnemyAnimType.SimpleAnimation)
            {
                // ★ [수정]: 닭/슬라임도 도망칠 때 플레이어가 아니라 실제 도망 방향(moveDir.x)을 바라보도록 통일
                if (isRunning && !isFleeing && targetTransform != null)
                {
                    spriteRenderer.flipX = targetTransform.position.x > transform.position.x;
                }
                else
                {
                    // 진행 방향이 오른쪽이면 flipX = true, 왼쪽이면 false (기존 닭 프리팹 기준)
                    spriteRenderer.flipX = moveDir.x > 0f;
                }
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

    public void TakeDamage(int damageAmount)
    {
        if (!isInitialized || isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} 피격 발생! 데미지: {damageAmount}, 남은 HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Kill();
        }
        else
        {
            if (!stats.isAggressive)
            {
                isFleeing = true;
                fleeTimer = panicDuration;
            }

            if (anim != null && hasParamHit) anim.SetTrigger("Hit");

            if (spriteRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRedCoroutine());
            }
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

        StartCoroutine(DieSequenceCoroutine());
    }

    private IEnumerator DieSequenceCoroutine()
    {
        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        float dieAnimDuration = 0.5f;

        if (anim != null && hasParamDie)
        {
            anim.SetTrigger("Die");

            yield return new WaitForEndOfFrame();
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Die"))
            {
                dieAnimDuration = stateInfo.length;
            }
        }

        yield return new WaitForSeconds(dieAnimDuration);

        DropItems();
        Destroy(gameObject);
    }

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

                ItemData data = ItemDataManager.instance.GetItemDataByID(rule.itemID);
                if (data != null)
                {
                    Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
                    GameObject droppedObj = Instantiate(fieldItemPrefab, dropPos, Quaternion.identity);

                    FieldItem fieldItem = droppedObj.GetComponent<FieldItem>();
                    if (fieldItem != null)
                    {
                        fieldItem.Setup(data, count);
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