using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;

public class EnemyBaseAI : MonoBehaviour, IDamageable
{
    public enum EnemyAnimType { BlendTree, SimpleAnimation }

    // 몹은 특정 도구를 요구하지 않는다(아무 무기로나 때릴 수 있다).
    public string RequiredToolType => null;

    [Header("에셋 데이터베이스 설정")]
    public EnemyDataSO stats;

    [Header("추격 대상 설정")]
    public Transform targetTransform;

    [Header("순찰 및 행동 주기 설정 (초 단위)")]
    [Tooltip("가축/몬스터가 한 방향으로 걷는 최소 시간")]
    public float minWalkTime = 1.5f;
    [Tooltip("가축/몬스터가 한 방향으로 걷는 최대 시간")]
    public float maxWalkTime = 3.5f;
    [Tooltip("가축/몬스터가 멈춰서 대기하는 최소 시간")]
    public float minIdleTime = 1.0f;
    [Tooltip("가축/몬스터가 멈춰서 대기하는 최대 시간")]
    public float maxIdleTime = 4.0f;

    [Header("가축 피격 도망 설정")]
    [Tooltip("가축이 공격받았을 때 도망치는 지속 시간")]
    public float panicDuration = 2.0f;
    [Tooltip("가축이 도망칠 때의 이동 속도 배율")]
    public float fleeSpeedMultiplier = 1.2f;

    [Header("공격 판정 및 모션 타이밍 설정")]
    [Tooltip("Attack 트리거 발동 후 실제 데미지가 적용될 때까지의 시간")]
    public float attackWindup = 0.25f;
    [Tooltip("공격 모션이 완전히 끝나고 다시 움직이기까지 대기하는 시간(초)")]
    public float attackDuration = 0.6f;

    [Header("상황별 애니메이션 스타일 교체")]
    public EnemyAnimType wanderAnimStyle = EnemyAnimType.BlendTree;
    public EnemyAnimType chaseAnimStyle = EnemyAnimType.SimpleAnimation;

    [Header("드랍 아이템 설정")]
    [Tooltip("월드 바닥에 생성될 공용 필드 아이템 프리팹")]
    public GameObject fieldItemPrefab;

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
    private bool isAttacking = false; // ★ 공격 모션 재생 중 이동 및 중복 행동을 막는 플래그

    private bool isFleeing = false;
    private float fleeTimer = 0f;

    private float stateTimer;
    private float targetStateTime;
    private bool isWanderingMove = false;
    private Coroutine flashCoroutine;
    private Coroutine attackCoroutine; // ★ 공격 동작 제어 코루틴
    private Color baseColor = Color.white;

    // 파라미터 유무 사전 캐싱
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

        if (spriteRenderer != null) baseColor = spriteRenderer.color;

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

        // 공격 동작 중에는 이동 방향을 초기화하고 다른 행동 전환을 일시 차단
        if (isAttacking)
        {
            isMoving = false;
            isRunning = false;
            moveDir = Vector2.zero;
            UpdateAnimation();
            return;
        }

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
                    moveDir = ((Vector2)transform.position - (Vector2)targetTransform.position).normalized;
                }
            }
        }
        // 2. 선공 몹 전투 및 추격
        else if (stats.isAggressive)
        {
            float distance = (targetTransform != null) ? Vector2.Distance(transform.position, targetTransform.position) : float.MaxValue;

            // 공격 사거리 안쪽이고 쿨타임이 찼다면 공격 시도
            if (distance <= stats.attackRange)
            {
                isChasing = true;
                isMoving = false;
                isRunning = false;
                moveDir = Vector2.zero;

                if (Time.time >= lastAttackTime + stats.attackCooldown)
                {
                    TryAttack();
                }
            }
            // 사거리를 벗어났으나 감지 범위 내라면 즉시 추격 (공격 모션 잔류 차단)
            else if (distance <= stats.detectRange)
            {
                isChasing = true;
                isMoving = true;
                isRunning = true;
                moveDir = ((Vector2)targetTransform.position - (Vector2)transform.position).normalized;
            }
            // 플레이어를 놓쳤다면 평화 순찰 모드로 복귀
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
        // 3. 평소 순찰 (가축 및 비선공 상태)
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
        // 사망 상태이거나 공격 모션 실행 중일 때는 물리 이동을 즉시 정지
        if (!isInitialized || !isMoving || isDead || isAttacking)
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
        lastAttackTime = Time.time;
        isAttacking = true;
        isMoving = false;
        rb.linearVelocity = Vector2.zero;

        // 공격 시작 시 플레이어 방향으로 즉시 바라보기 보정
        if (targetTransform != null)
        {
            Vector2 toPlayer = (targetTransform.position - transform.position).normalized;
            AlignFacingDirection(toPlayer);
        }

        if (anim != null && hasParamAttack)
        {
            anim.SetTrigger("Attack");
        }

        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackSequenceRoutine());
    }

    private IEnumerator AttackSequenceRoutine()
    {
        // 1. 공격 준비 시간(윈드업) 대기
        yield return new WaitForSeconds(attackWindup);

        // 공격 준비 중 대상이 죽거나 범위를 벗어났는지 재검증
        if (!isDead && targetTransform != null)
        {
            float distance = Vector2.Distance(transform.position, targetTransform.position);
            if (distance <= stats.attackRange)
            {
                PlayerMove player = targetTransform.GetComponent<PlayerMove>();
                if (player != null) player.TakeDamage(stats.attackDamage, transform.position);
            }
        }

        // 2. 남은 공격 후딜레이(전체 모션 길이에서 선딜레이 제외) 대기
        float recoveryTime = Mathf.Max(0.05f, attackDuration - attackWindup);
        yield return new WaitForSeconds(recoveryTime);

        isAttacking = false;
        attackCoroutine = null;
    }

    void AlignFacingDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (hasParamDirX) anim.SetFloat("DirX", 1f);
            if (hasParamDirY) anim.SetFloat("DirY", 0f);
            spriteRenderer.flipX = (dir.x < 0f);
        }
        else
        {
            spriteRenderer.flipX = false;
            if (hasParamDirX) anim.SetFloat("DirX", 0f);
            if (hasParamDirY) anim.SetFloat("DirY", dir.y);
        }
    }

    void UpdateAnimation()
    {
        if (anim == null || isDead) return;

        if (hasParamIsMoving) anim.SetBool("IsMoving", isMoving);
        if (hasParamIsFleeing) anim.SetBool("IsFleeing", isFleeing);

        if (isMoving && !isAttacking)
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

                    if (isRunning && !isFleeing && targetTransform != null)
                    {
                        spriteRenderer.flipX = targetTransform.position.x < transform.position.x;
                    }
                    else
                    {
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
                if (isRunning && !isFleeing && targetTransform != null)
                {
                    spriteRenderer.flipX = targetTransform.position.x > transform.position.x;
                }
                else
                {
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

        // ★ 피격 시 진행 중이던 공격 동작 및 대기 중인 트리거 즉시 캔슬
        CancelAttackState();

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
                flashCoroutine = StartCoroutine(SpriteFlash.Flash(spriteRenderer, Color.red, 0.1f, baseColor));
            }
        }
    }

    private void CancelAttackState()
    {
        isAttacking = false;
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        // 큐에 밀려있던 공격 트리거 제거
        if (anim != null && hasParamAttack)
        {
            anim.ResetTrigger("Attack");
        }
    }

    public static event System.Action<int> OnEnemyKilled;

    private void Kill()
    {
        if (isDead) return;
        isDead = true;

        // 사망 시 공격 및 피격 관련 모든 모션 상태 캔슬
        CancelAttackState();
        if (anim != null && hasParamHit) anim.ResetTrigger("Hit");

        OnEnemyKilled?.Invoke(stats != null ? stats.expReward : 0);
        StartCoroutine(DieSequenceCoroutine());
    }

    private IEnumerator DieSequenceCoroutine()
    {
        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        if (spriteRenderer != null) spriteRenderer.color = baseColor;

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

        DropTable.SpawnDrops(dropRules, fieldItemPrefab, transform.position, 0.5f, name);
        Destroy(gameObject);
    }
}