using UnityEngine;

public class EnemyBaseAI : MonoBehaviour
{
    public EnemyData data; // 인스펙터에서 생성한 EnemyData 할당

    private Transform target;       // 추격 대상 (플레이어)
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    private float lastAttackTime;   // 마지막 공격 시간
    private int currentHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // "Player" 태그를 가진 오브젝트를 타겟으로 설정
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;

        currentHealth = data.maxHealth;
    }

    void Update()
    {
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance <= data.attackRange)
        {
            // 1. 공격 사거리 안이면 정지 및 공격
            StopMoving();
            TryAttack();
        }
        else if (distance <= data.detectRange)
        {
            // 2. 감지 범위 안이면 추격
            ChasePlayer();
        }
        else
        {
            // 3. 범위를 벗어나면 대기
            StopMoving();
        }
    }

    void ChasePlayer()
    {
        // 플레이어 방향 계산
        Vector2 direction = (target.position - transform.position).normalized;

        // 이동 및 애니메이션 처리
        rb.linearVelocity = direction * data.moveSpeed;

        anim.SetBool("IsMoving", true);

        // 방향에 따른 스프라이트 반전 (플레이어가 왼쪽에 있으면 뒤집기)
        spriteRenderer.flipX = target.position.x < transform.position.x;
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsMoving", false);
    }

    void TryAttack()
    {
        if (Time.time >= lastAttackTime + data.attackCooldown)
        {
            anim.SetTrigger("Attack");
            lastAttackTime = Time.time;

            // 여기에 플레이어 데미지 입히는 로직 추가 가능
            Debug.Log($"{data.enemyName}이(가) 공격합니다! 데미지: {data.attackDamage}");
        }
    }

    // 데미지 처리 함수
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{data.enemyName} 사망");
        Destroy(gameObject);
    }
}