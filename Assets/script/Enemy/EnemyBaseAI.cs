using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class EnemyBaseAI : MonoBehaviour
{
    public enum EnemyAnimType { BlendTree, SimpleAnimation }

    [Header("CSV 데이터 설정")]
    public string enemyItemID;

    [Header("애니메이션 타입 설정")]
    public EnemyAnimType animType;

    [Header("추격 대상 설정")]
    [Tooltip("플레이어, NPC, 가축 등을 여기에 드래그하세요.")]
    public Transform targetTransform;

    // ★ 동물 AI에서 가져온 랜덤 순찰(Wandering) 설정
    [Header("순찰 설정 (타겟이 없을 때)")]
    public float minWalkTime = 1.5f;
    public float maxWalkTime = 3.5f;
    public float minIdleTime = 1.0f;
    public float maxIdleTime = 3.0f;

    [Header("타일맵 이동 제한 설정")]
    public List<Tilemap> walkableTilemaps;
    public List<Tilemap> blockedTilemaps;

    private EnemyRawData stats;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    private Vector2 moveDir;
    private float lastAttackTime;
    private int currentHealth;
    private bool isInitialized = false;

    // ★ 상태 관리를 위한 변수들
    private bool isMoving = false;
    private bool isRunning = false;
    private bool isChasing = false; // 현재 타겟을 쫓는 중인가?

    // ★ 순찰용 타이머 및 상태
    private float stateTimer;
    private float targetStateTime;
    private bool isWanderingMove = false; // 순찰 중 '이동' 상태인가?

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

        if (targetTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) targetTransform = playerObj.transform;
        }
    }

    void Start()
    {
        if (EnemyDataManager.Instance != null)
        {
            stats = EnemyDataManager.Instance.GetEnemyData(enemyItemID);
            if (stats != null)
            {
                currentHealth = stats.maxHealth;
                isInitialized = true;
            }
        }

        // 시작할 때 첫 순찰 상태 세팅
        SwitchWanderState();
    }

    void Update()
    {
        if (!isInitialized) return;

        // 1. 타겟 존재 여부 및 거리 체크
        float distance = (targetTransform != null) ? Vector2.Distance(transform.position, targetTransform.position) : float.MaxValue;

        // 2. AI 상태 분기 결정
        if (distance <= stats.attackRange)
        {
            // [공격 상태]
            isChasing = true;
            isMoving = false;
            isRunning = false;
            moveDir = Vector2.zero;
            TryAttack();
        }
        else if (distance <= stats.detectRange)
        {
            // [추격 상태 (달리기)]
            isChasing = true;
            isMoving = true;
            isRunning = true;
            moveDir = (targetTransform.position - transform.position).normalized;
        }
        else
        {
            // [순찰 상태 (동물처럼 걷기/쉬기 랜덤 반복)]
            if (isChasing)
            {
                // 방금 전까지 쫓아가다가 대상을 놓친 상황이라면 즉시 순찰 상태로 초기화
                isChasing = false;
                isRunning = false;
                SwitchWanderState();
            }

            HandleWandering();
        }

        UpdateAnimation();
    }

    // ★ 동물 로직 이식: 타겟이 없을 때 주변을 어슬렁거리는 함수
    void HandleWandering()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= targetStateTime)
        {
            SwitchWanderState();
        }

        // 순찰 이동 중일 때만 움직임 활성화
        isMoving = isWanderingMove;
    }

    // ★ 순찰 상태(걷기 <-> 대기)를 바꾸고 시간을 랜덤화하는 함수
    void SwitchWanderState()
    {
        stateTimer = 0f;
        isWanderingMove = !isWanderingMove; // 상태 반전

        if (isWanderingMove)
        {
            // 무작위 방향으로 걷기 시작
            moveDir = Random.insideUnitCircle.normalized;
            targetStateTime = Random.Range(minWalkTime, maxWalkTime);
        }
        else
        {
            // 제자리에 멈춰서 쉬기
            moveDir = Vector2.zero;
            targetStateTime = Random.Range(minIdleTime, maxIdleTime);
        }
    }

    void FixedUpdate()
    {
        if (!isInitialized || !isMoving)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speedMod = (WeatherManager.Instance != null) ? WeatherManager.Instance.GetSpeedModifier() : 1.0f;

        // ★ 추격 중(isRunning)일 때는 제 속도로 뛰고, 평소 순찰 중일 때는 속도를 절반(0.5배)으로 낮춰서 느긋하게 걷게 만듭니다.
        float currentSpeed = isRunning ? stats.moveSpeed : (stats.moveSpeed * 0.5f);

        Vector2 nextPos = rb.position + moveDir * (currentSpeed * speedMod) * Time.fixedDeltaTime;

        if (CanMoveTo(nextPos))
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (!isRunning) // 순찰 중에 벽에 막히면 즉시 쉬거나 다른 방향으로 전환
            {
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
            Debug.Log($"[{stats.enemyName}]이(가) [{targetTransform.name}]을(를) 공격했습니다!");
        }
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        anim.SetBool("IsMoving", isMoving);
        anim.SetBool("IsRunning", isRunning); // 플레이어를 쫓을 때만 True가 됨

        if (isMoving)
        {
            if (animType == EnemyAnimType.BlendTree)
            {
                float absX = Mathf.Abs(moveDir.x);
                float absY = Mathf.Abs(moveDir.y);

                if (absX > absY)
                {
                    anim.SetFloat("DirX", 1f);
                    anim.SetFloat("DirY", 0f);

                    // 추격 중이면 타겟 기준, 순찰 중이면 자신이 걷는 방향 기준으로 FlipX 처리
                    Transform flipTarget = isRunning ? targetTransform : this.transform;
                    float targetX = isRunning ? flipTarget.position.x : (transform.position.x + moveDir.x);

                    spriteRenderer.flipX = targetX < transform.position.x;
                }
                else
                {
                    spriteRenderer.flipX = false;
                    anim.SetFloat("DirX", 0f);
                    anim.SetFloat("DirY", moveDir.y);
                }
            }
            else
            {
                // SimpleAnimation의 경우 좌우 이미지 뒤집기
                float targetX = isRunning ? targetTransform.position.x : (transform.position.x + moveDir.x);
                spriteRenderer.flipX = targetX > transform.position.x;
            }
        }
    }

    public bool CanMoveTo(Vector2 worldPos)
    {
        foreach (var tilemap in blockedTilemaps)
        {
            if (tilemap != null && tilemap.HasTile(tilemap.WorldToCell(worldPos))) return false;
        }
        foreach (var tilemap in walkableTilemaps)
        {
            if (tilemap != null && tilemap.HasTile(tilemap.WorldToCell(worldPos))) return true;
        }
        return false;
    }

    public void TakeDamage(int damageAmount)
    {
        if (!isInitialized) return;

        currentHealth -= damageAmount;
        if (anim != null) anim.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
}