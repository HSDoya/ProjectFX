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

        anim.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            float absX = Mathf.Abs(moveDir.x);
            float absY = Mathf.Abs(moveDir.y);

            // 1. 좌우 이동이 상하 이동보다 클 때 (옆으로 걷기)
            if (absX > absY)
            {
                // [핵심] 블렌드 트리에는 무조건 '오른쪽(1, 0)' 위치의 애니메이션을 틀라고 시킵니다.
                anim.SetFloat("DirX", 1f);
                anim.SetFloat("DirY", 0f);

                // 실제 방향은 스프라이트를 뒤집어서 해결합니다.
                spriteRenderer.flipX = moveDir.x > 0;
            }
            // 2. 상하 이동이 더 클 때 (위/아래 걷기)
            else
            {
                spriteRenderer.flipX = false;

                anim.SetFloat("DirX", 0f);
                anim.SetFloat("DirY", moveDir.y); // 위(1) 또는 아래(-1)
            }
        }
    }
}