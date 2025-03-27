using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class AnimalAI : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;
    public float moveSpeed = 2f;
    public float idleTimeMin = 1f;
    public float idleTimeMax = 3f;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveDirection;
    private bool isIdle = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        StartCoroutine(MoveRoutine());
    }

    IEnumerator MoveRoutine()
    {
        while (true)
        {
            ChooseNewDirection();
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            moveDirection = Vector2.zero;
            anim.SetFloat("Run", 0);
            isIdle = true;
            yield return new WaitForSeconds(Random.Range(idleTimeMin, idleTimeMax));
            isIdle = false;
        }
    }

    void FixedUpdate()
    {
        if (isIdle) return;

        Vector2 newPos = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        Vector3Int tilePos = groundTilemap.WorldToCell(newPos);

        if (IsWalkable(tilePos))
        {
            rb.MovePosition(newPos);
            anim.SetFloat("Run", moveDirection.magnitude);
            if (moveDirection.x != 0)
            {
                spriteRenderer.flipX = moveDirection.x < 0;
            }
        }
        else
        {
            anim.SetFloat("Run", 0);
            moveDirection = Vector2.zero;
        }
    }

    bool IsWalkable(Vector3Int tilePos)
    {
        return groundTilemap.HasTile(tilePos) && !waterTilemap.HasTile(tilePos);
    }

    void ChooseNewDirection()
    {
        int random = Random.Range(0, 4);
        switch (random)
        {
            case 0: moveDirection = Vector2.up; break;
            case 1: moveDirection = Vector2.down; break;
            case 2: moveDirection = Vector2.left; break;
            case 3: moveDirection = Vector2.right; break;
        }
    }
}
