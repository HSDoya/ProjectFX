using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class AnimalAI : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 1.5f;
    public float changeDirTime = 3f;

    private Vector2 moveDir;
    private float timer;
    private Rigidbody2D rb;

    [Header("이동 가능 타일맵")]
    public List<Tilemap> walkableTilemaps;

    [Header("이동 불가 타일맵")]
    public List<Tilemap> blockedTilemaps;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        PickRandomDirection();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= changeDirTime)
        {
            PickRandomDirection();
            timer = 0f;
        }
    }

    void FixedUpdate()
    {
        Vector2 nextPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;

        // ⭐ 이동 가능 여부 판단
        if (CanMoveTo(nextPos))
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            // 막히면 방향 다시 선택
            PickRandomDirection();
        }
    }

    void PickRandomDirection()
    {
        moveDir = Random.insideUnitCircle.normalized;
    }

    // ===============================
    // ⭐ 핵심 : 타일 이동 판정
    // ===============================
    public bool CanMoveTo(Vector2 worldPos)
    {
        // 1️⃣ 이동 불가 타일 먼저 검사 (하나라도 걸리면 바로 false)
        foreach (var tilemap in blockedTilemaps)
        {
            if (tilemap == null) continue;

            Vector3Int cell = tilemap.WorldToCell(worldPos);
            if (tilemap.HasTile(cell))
                return false;
        }

        // 2️⃣ 이동 가능 타일 검사 (하나라도 있으면 true)
        foreach (var tilemap in walkableTilemaps)
        {
            if (tilemap == null) continue;

            Vector3Int cell = tilemap.WorldToCell(worldPos);
            if (tilemap.HasTile(cell))
                return true;
        }

        // 3️⃣ 아무 타일에도 해당 안 되면 이동 불가
        return false;
    }

    // ===============================
    // 도살 처리 (기존 로직 유지)
    // ===============================
    public void KillAndDrop(Vector3 pos)
    {
        // TODO : 드랍 아이템 생성
        Destroy(gameObject);
    }
}
