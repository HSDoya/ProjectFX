using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class EnemyBaseAI : MonoBehaviour
{
    public enum EnemyAnimType { BlendTree, SimpleAnimation }

    [Header("CSV 데이터 설정")]
    public string enemyItemID; // 엑셀(CSV) ID (예: Slime_01, Zombie_01)

    [Header("추격 대상 설정")]
    public Transform targetTransform;

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

    // 상태 관리를 위한 변수들
    private bool isMoving = false;
    private bool isRunning = false; // 내부 속도 계산용으로만 유지 (애니메이터 파라미터는 삭제)
    private bool isChasing = false;

    private float stateTimer;
    private float targetStateTime;
    private bool isWanderingMove = false;

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
                SwitchWanderState();
            }
        }
    }

    void Update()
    {
        if (!isInitialized) return;

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
            isRunning = true; // 플레이어를 쫓을 때 내부적으로 뛰어오는 속도를 내기 위해 유지
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
        if (stateTimer >= targetStateTime) SwitchWanderState();
        isMoving = isWanderingMove;
    }

    void SwitchWanderState()
    {
        stateTimer = 0f;
        isWanderingMove = !isWanderingMove;

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
        if (!isInitialized || !isMoving)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speedMod = (WeatherManager.Instance != null) ? WeatherManager.Instance.GetSpeedModifier() : 1.0f;

        // 내부적(isRunning)으로 추격 중일 땐 정상 속도, 순찰 중일 땐 0.5배 속도로 걷는 물리 연산은 유지합니다.
        float currentSpeed = isRunning ? stats.moveSpeed : (stats.moveSpeed * 0.5f);
        Vector2 nextPos = rb.position + moveDir * (currentSpeed * speedMod) * Time.fixedDeltaTime;

        if (CanMoveTo(nextPos)) rb.MovePosition(nextPos);
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (!isRunning) SwitchWanderState();
        }
    }

    void TryAttack()
    {
        if (Time.time >= lastAttackTime + stats.attackCooldown)
        {
            if (anim != null) anim.SetTrigger("Attack");
            lastAttackTime = Time.time;
            Debug.Log($"[{stats.enemyName}]이(가) [{targetTransform.name}]을(를) 공격 시도!");

            if (targetTransform != null)
            {
                PlayerMove player = targetTransform.GetComponent<PlayerMove>();

                if (player != null)
                {
                    // ★ 수정: 반올림(Mathf.RoundToInt) 제거, stats.attackDamage를 그대로 전달
                    float damageToPlayer = stats.attackDamage;
                    Debug.Log($"-> 타겟(PlayerMove) 발견 성공! 전달할 데미지: {damageToPlayer}");

                    player.TakeDamage(damageToPlayer);
                }
                else
                {
                    Debug.LogWarning($"-> 주의: {targetTransform.name} 오브젝트에서 PlayerMove 컴포넌트를 찾을 수 없습니다!");
                }
            }
        }
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        // ★ 변경점: 애니메이터에게 'IsMoving' 파라미터만 전달합니다. (IsRunning 전달 제거)
        anim.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            if (stats.animType == "BlendTree")
            {
                float absX = Mathf.Abs(moveDir.x);
                float absY = Mathf.Abs(moveDir.y);

                if (absX > absY)
                {
                    anim.SetFloat("DirX", 1f);
                    anim.SetFloat("DirY", 0f);
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
            else // SimpleAnimation
            {
                float targetX = isRunning ? targetTransform.position.x : (transform.position.x + moveDir.x);
                spriteRenderer.flipX = targetX > transform.position.x;
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
        if (!isInitialized) return;
        currentHealth -= damageAmount;
        if (anim != null) anim.SetTrigger("Hit");
        if (currentHealth <= 0)
        {
            // ==========================================
            // ★ 수정: 분리된 컴포넌트(ItemDropper)를 활용하여 아이템 드랍
            // ==========================================
            ItemDropper dropper = GetComponent<ItemDropper>();
            if (dropper != null)
            {
                dropper.DropItems();
            }

            Destroy(gameObject);
        }
    }
}