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
    private Collider2D col; // 사망 시 충돌체 비활성화를 위해 추가

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
        col = GetComponent<Collider2D>(); // 콜라이더 컴포넌트 캐싱

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (targetTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) targetTransform = playerObj.transform;
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

        float distance = (targetTransform != null) ? Vector2.Distance(transform.position, targetTransform.position) : float.MaxValue;

        if (stats.isAggressive && distance <= stats.attackRange)
        {
            isChasing = true;
            isMoving = false;
            isRunning = false;
            moveDir = Vector2.zero;
            TryAttack();
        }
        else if (stats.isAggressive && distance <= stats.detectRange)
        {
            isChasing = true;
            isMoving = true;
            isRunning = true;
            moveDir = (targetTransform.position - transform.position).normalized;
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
            // 아직 살아있을 때만 Hit 애니메이션 및 피격 깜빡임 연출
            if (anim != null) anim.SetTrigger("Hit");

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

    // ========================================================
    // [개선] 사망 애니메이션 재생 후 드랍 및 파괴 처리
    // ========================================================
    private void Kill()
    {
        if (isDead) return;
        isDead = true;

        // 사망 처리 코루틴 시동
        StartCoroutine(DieSequenceCoroutine());
    }

    private IEnumerator DieSequenceCoroutine()
    {
        // 1. 물리/이동 정지 및 추가 타격 방지
        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false; // 충돌체를 꺼서 플레이어가 미는 현상 방지

        // 2. 피격 빨간색 제거 및 색상 초기화
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        // 3. 사망 애니메이션 트리거 신호 발송
        float dieAnimDuration = 0.5f; // 기본 대기 시간 (기본값)

        if (anim != null)
        {
            anim.SetTrigger("Die");

            // 애니메이터에 재생 중인 Die 클립의 실제 길이를 가져옵니다.
            yield return new WaitForEndOfFrame(); // 트리거 전환 대기
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Die"))
            {
                dieAnimDuration = stateInfo.length;
            }
        }

        // 4. 사망 애니메이션 재생 시간만큼 대기
        yield return new WaitForSeconds(dieAnimDuration);

        // 5. 애니메이션 완료 후 아이템 드랍 및 오브젝트 파괴
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