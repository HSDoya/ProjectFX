using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerMove : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed = 5f;
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    public Tilemap farmTilemap;
    public Tilemap waterTilemap;
    public float gatherRange = 1.5f;

    private Animator anim;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryGather();
        }
    }

    private void FixedUpdate()
    {
        rigid.linearVelocity = inputVec * speed;
    }

    private void LateUpdate()
    {
        anim.SetFloat("Speed", inputVec.magnitude);
        if (inputVec.x != 0)
            spriteRenderer.flipX = inputVec.x < 0;
    }

    private void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
    }

    private void TryGather()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, gatherRange);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Gatherable"))
            {
                Debug.Log($"채집됨: {hit.name}");
                Destroy(hit.gameObject);
                break;
            }
        }
    }
}
