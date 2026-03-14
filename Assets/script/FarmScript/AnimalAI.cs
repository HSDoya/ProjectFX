using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class AnimalAI : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 1.5f;
    public float changeDirTime = 3f;
    public float idleTime = 1.5f; // 가만히 서 있는 시간 추가

    private Vector2 moveDir;
    private float timer;
    private bool isMoving = true; // 현재 움직이는 상태인지 확인

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer; // 좌우 반전을 위한 변수

    [Header("이동 가능 타일맵")]
    public List<Tilemap> walkableTilemaps;

    [Header("이동 불가 타일맵")]
    public List<Tilemap> blockedTilemaps;

    private Animator anim; // 애니메이터 변수 추가

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>(); // 애니메이터 초기화
        PickRandomDirection();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float targetTime = isMoving ? changeDirTime : idleTime;

        if (timer >= targetTime)
        {
            SwitchState();
            timer = 0f;
        }

        // ⭐ 애니메이션 파라미터 업데이트
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (!isMoving)
        {
            rb.linearVelocity = Vector2.zero; // 멈춰있을 때 속도 초기화
            return;
        }

        // 날씨 영향을 받고 싶다면 WeatherManager 배율을 곱할 수 있습니다.
        float speedMod = (WeatherManager.Instance != null) ? WeatherManager.Instance.GetSpeedModifier() : 1.0f;
        Vector2 nextPos = rb.position + moveDir * (moveSpeed * speedMod) * Time.fixedDeltaTime;

        if (CanMoveTo(nextPos))
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            // 막히면 대기 상태로 전환하고 방향 다시 정하기
            SwitchState();
        }
    }

    void SwitchState()
    {
        isMoving = !isMoving; // 상태 반전

        if (isMoving)
        {
            PickRandomDirection();
        }
    }

    void PickRandomDirection()
    {
        moveDir = Random.insideUnitCircle.normalized;
    }

    // ⭐ 핵심: 이동 방향에 따라 스프라이트 뒤집기
    void UpdateSpriteFlip()
    {
        if (!isMoving || moveDir.x == 0) return;

        // moveDir.x가 0보다 작으면 왼쪽(true), 크면 오른쪽(false)
        // (참고: 스프라이트의 기본 방향이 오른쪽을 보고 있을 때 기준입니다)
        spriteRenderer.flipX = moveDir.x < 0;
    }

    public bool CanMoveTo(Vector2 worldPos)
    {
        foreach (var tilemap in blockedTilemaps)
        {
            if (tilemap == null) continue;
            Vector3Int cell = tilemap.WorldToCell(worldPos);
            if (tilemap.HasTile(cell)) return false;
        }

        foreach (var tilemap in walkableTilemaps)
        {
            if (tilemap == null) continue;
            Vector3Int cell = tilemap.WorldToCell(worldPos);
            if (tilemap.HasTile(cell)) return true;
        }

        return false;
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        // 1. 움직이고 있는지 여부를 전달
        anim.SetBool("IsMoving", isMoving);

        // 2. 움직일 때만 방향 데이터를 업데이트 (멈췄을 때 마지막 방향을 기억하게 함)
        if (isMoving)
        {
            anim.SetFloat("DirX", moveDir.x);
            anim.SetFloat("DirY", moveDir.y);

            // 만약 상하 애니메이션이 있고, 좌우 반전은 좌우 이동시에만 쓰고 싶다면:
            if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
            {
                spriteRenderer.flipX = moveDir.x < 0;
            }
        }
    }
}