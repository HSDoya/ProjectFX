using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class AnimalAI : MonoBehaviour
{
    public enum AnimalType { BlendTree, SimpleAnimation }

    [Header("타입 설정")]
    public AnimalType animalType;

    [Header("이동 설정")]
    public float moveSpeed = 1.5f;

    [Header("이동 시간 설정 (Random)")]
    public float minMoveTime = 2f;
    public float maxMoveTime = 5f;
    private float currentMoveTime;

    [Header("대기 시간 설정 (Random)")]
    public float minIdleTime = 1f;
    public float maxIdleTime = 4f;
    private float currentIdleTime;

    [Header("닭 전용 설정")]
    [Range(0, 100)]
    public float peckProbability = 30f; // 쉴 때 모이를 쪼을 확률

    private Vector2 moveDir;
    private float timer;
    private bool isMoving = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    [Header("타일맵 설정")]
    public List<Tilemap> walkableTilemaps;
    public List<Tilemap> blockedTilemaps;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        SetRandomMoveTime();
        SetRandomIdleTime();
        PickRandomDirection();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float targetTime = isMoving ? currentMoveTime : currentIdleTime;

        if (timer >= targetTime)
        {
            SwitchState(); // 여기서 발생하는 모호함 오류는 아래 메서드가 하나만 있으면 해결됩니다.
            timer = 0f;
        }

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (!isMoving)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speedMod = (WeatherManager.Instance != null) ? WeatherManager.Instance.GetSpeedModifier() : 1.0f;
        Vector2 nextPos = rb.position + moveDir * (moveSpeed * speedMod) * Time.fixedDeltaTime;

        if (CanMoveTo(nextPos))
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            SwitchState();
            timer = 0f;
        }
    }

    // ★ 이 메서드가 코드 안에 딱 "하나"만 있어야 합니다.
    void SwitchState()
    {
        isMoving = !isMoving;

        if (isMoving)
        {
            PickRandomDirection();
            SetRandomMoveTime();

            // 걷기 시작하면 모이 쪼기 애니메이션 해제
            if (anim != null) anim.SetBool("Peck", false);
        }
        else
        {
            SetRandomIdleTime();

            // 멈췄을 때 닭이라면 확률적으로 모이 쪼기 실행
            if (animalType == AnimalType.SimpleAnimation && anim != null)
            {
                float roll = Random.Range(0f, 100f);
                anim.SetBool("Peck", roll <= peckProbability);
            }
        }
    }

    void SetRandomMoveTime() => currentMoveTime = Random.Range(minMoveTime, maxMoveTime);
    void SetRandomIdleTime() => currentIdleTime = Random.Range(minIdleTime, maxIdleTime);

    void PickRandomDirection()
    {
        moveDir = Random.insideUnitCircle.normalized;
    }

    void UpdateAnimation()
    {
        if (anim == null) return;
        anim.SetBool("IsMoving", isMoving);

        if (animalType == AnimalType.BlendTree) UpdateBlendTreeAnimation();
        else UpdateSimpleAnimation();
    }

    void UpdateBlendTreeAnimation()
    {
        if (isMoving)
        {
            float absX = Mathf.Abs(moveDir.x);
            float absY = Mathf.Abs(moveDir.y);
            if (absX > absY)
            {
                anim.SetFloat("DirX", 1f);
                anim.SetFloat("DirY", 0f);
                spriteRenderer.flipX = moveDir.x > 0;
            }
            else
            {
                spriteRenderer.flipX = false;
                anim.SetFloat("DirX", 0f);
                anim.SetFloat("DirY", moveDir.y);
            }
        }
    }

    void UpdateSimpleAnimation()
    {
        if (isMoving)
        {
            spriteRenderer.flipX = moveDir.x > 0;
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
}