using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class AnimalAI : MonoBehaviour
{
    public enum Species { Chicken, Cow }
    private enum AnimalJob { None, Idle, Wander, Peck }

    [Header("Basic")]
    [SerializeField] private Species species = Species.Chicken;

    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;

    [Header("Movement Common")]
    public float moveSpeed = 2f;
    public float idleTimeMin = 1f;
    public float idleTimeMax = 3f;
    [Range(0f, 1f)] public float turnBias = 0.3f;

    [Header("Move Duration By Species")]
    public Vector2 chickenMoveDuration = new Vector2(5f, 10f);
    public Vector2 cowMoveDuration = new Vector2(10f, 20f);

    [Header("Collision / Obstacle")]
    public float bodyRadius = 0.2f;
    public LayerMask obstacleMask;

    [Header("Peck (Chicken Only)")]
    public bool enablePecking = true;
    public Vector2 peckDurationRange = new Vector2(3f, 4f);      // 쪼기 유지 3~4초
    public Vector2 peckThinkInterval = new Vector2(6f, 12f);     // 다음 쪼기까지 대기
    [System.Serializable]
    public class DropConfig
    {
        public Species species;
        public List<GameObject> prefabs;
        public Vector2Int countRange = new Vector2Int(1, 1);
    }
    [Header("Drops By Species")]
    public List<DropConfig> dropTable = new List<DropConfig>();

    // ===== RUNTIME =====
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Vector2 moveDir = Vector2.zero;
    private AnimalJob currentJob = AnimalJob.Idle;
    private float jobTimer = 0f;
    // 🔒 Peck 중 하드락 (이거 하나로만 이동을 막음)
    private bool peckLock = false;
    private float nextPeckAllowedTime = 0f;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        ApplySpeciesPreset();

        // 시작은 Idle
        currentJob = AnimalJob.Idle;
        jobTimer = Random.Range(idleTimeMin, idleTimeMax);
        moveDir = Vector2.zero;
        peckLock = false;

        anim.ResetTrigger("Peck");
        anim.SetBool("IsMoving", false);

        if (species == Species.Chicken)
        {
            nextPeckAllowedTime = Time.time + Random.Range(peckThinkInterval.x, peckThinkInterval.y);
        }
    }
    void Start()
    {
        // RimWorld 느낌의 간단 Think 루프
        StartCoroutine(ThinkLoop());
    }

    // ─────────────────────────────────────
    // Player에서 도살 시 호출
    // ─────────────────────────────────────
    public void KillAndDrop(Vector3? dropAt = null)
    {
        Vector3 pos = dropAt ?? transform.position;
        DropConfig cfg = dropTable.Find(c => c.species == species);

        if (cfg != null && cfg.prefabs != null && cfg.prefabs.Count > 0 && cfg.countRange.y > 0)
        {
            int min = Mathf.Max(0, cfg.countRange.x);
            int max = Mathf.Max(min, cfg.countRange.y);
            int count = Random.Range(min, max + 1);
            for (int i = 0; i < count; i++)
            {
                GameObject p = cfg.prefabs[Random.Range(0, cfg.prefabs.Count)];
                if (p) Instantiate(p, pos, Quaternion.identity);
            }
        }

        Destroy(gameObject);
    }

    // ─────────────────────────────────────
    // Think 루프 (0.5초 단위로 Job 판단)
    // ─────────────────────────────────────
    private System.Collections.IEnumerator ThinkLoop()
    {
        while (true)
        {
            if (jobTimer <= 0f)
            {
                DecideNextJob();
            }

            jobTimer -= 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void DecideNextJob()
    {
        // Peck Job 끝났으면 락 해제 (안전용)
        if (currentJob == AnimalJob.Peck)
        {
            EndPeckJob();
        }

        float r = Random.value;

        if (species == Species.Chicken)
        {
            bool canPeck = enablePecking && Time.time >= nextPeckAllowedTime;

            // 대충 Idle / Wander / Peck 비율 3:4:3
            if (canPeck && r < 0.3f)
                StartPeckJob();
            else if (r < 0.7f)
                StartWanderJob(true);
            else
                StartIdleJob();
        }
        else
        {
            // 소: Idle / Wander 비율 4:6
            if (r < 0.4f)
                StartIdleJob();
            else
                StartWanderJob(false);
        }
    }

    private void StartIdleJob()
    {
        currentJob = AnimalJob.Idle;
        jobTimer = Random.Range(idleTimeMin, idleTimeMax);
        moveDir = Vector2.zero;

        // Idle이니 이동 끄기
        anim.SetBool("IsMoving", false);
    }

    private void StartWanderJob(bool isChicken)
    {
        currentJob = AnimalJob.Wander;
        moveDir = GetRandomDir();

        anim.SetFloat("MoveY", moveDir.y);
        anim.SetBool("IsMoving", true);

        jobTimer = isChicken
            ? Random.Range(chickenMoveDuration.x, chickenMoveDuration.y)
            : Random.Range(cowMoveDuration.x, cowMoveDuration.y);
    }

    private void StartPeckJob()
    {
        if (species != Species.Chicken || !enablePecking)
        {
            StartIdleJob();
            return;
        }

        currentJob = AnimalJob.Peck;
        jobTimer = Random.Range(peckDurationRange.x, peckDurationRange.y);

        // 🔒 Peck 락 ON
        peckLock = true;

        // 이동 즉시 정지
        moveDir = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsMoving", false);

        anim.ResetTrigger("Peck");
        anim.SetTrigger("Peck");

        anim.SetFloat("MoveX", 0);
        anim.SetFloat("MoveY", 0);

        // 다음 Peck까지 대기 시간 설정
        nextPeckAllowedTime = Time.time + Random.Range(peckThinkInterval.x, peckThinkInterval.y);
    }

    private void EndPeckJob()
    {
        peckLock = false;           // 🔓 Peck 락 OFF
        currentJob = AnimalJob.Idle;  // 자연스럽게 Idle로 돌아가게
        moveDir = Vector2.zero;
        anim.SetBool("IsMoving", false);
    }

    // 애니메이션 이벤트(선택사항) - Peck 끝 프레임에서 호출 가능
    public void OnPeckEnd()
    {
        // 애니메이션 길이가 바뀌어도 이벤트로 정확히 맞출 수 있음
        if (currentJob == AnimalJob.Peck)
        {
            jobTimer = 0f;    // 다음 Think에서 EndPeckJob 호출됨
        }
    }

    // ─────────────────────────────────────
    // 이동 처리 (여기서만 IsMoving을 ON/OFF)
    // ─────────────────────────────────────
    void FixedUpdate()
    {
        if (currentJob == AnimalJob.Peck || peckLock)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsMoving", false);
            anim.SetFloat("MoveX", 0);
            anim.SetFloat("MoveY", 0);
            return;
        }

        if (currentJob != AnimalJob.Wander)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsMoving", false);
            return;
        }

        Vector2 newPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;

        if (IsWalkable(newPos))
        {
            rb.MovePosition(newPos);

            anim.SetBool("IsMoving", true);

            sr.flipX = moveDir.x > 0;
        }
        else
        {
            moveDir = GetRandomDir();
        }
    }

    private bool IsWalkable(Vector2 worldPos2D)
    {
        Vector3Int cell = groundTilemap.WorldToCell(worldPos2D);
        Vector3 worldCenter = groundTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

        bool hasGround = groundTilemap && groundTilemap.HasTile(cell);
        bool isWater = waterTilemap && waterTilemap.HasTile(cell);

        bool hasObstacle = false;
        if (obstacleMask.value != 0)
            hasObstacle = Physics2D.OverlapCircle(worldCenter, bodyRadius, obstacleMask);

        return hasGround && !isWater && !hasObstacle;
    }

    private Vector2 GetRandomDir()
    {
        switch (Random.Range(0, 4))
        {
            case 0: return Vector2.up;
            case 1: return Vector2.down;
            case 2: return Vector2.left;
            case 3: return Vector2.right;
        }
        return Vector2.right;
    }

    private void ApplySpeciesPreset()
    {
        switch (species)
        {
            case Species.Chicken:
                moveSpeed = Mathf.Approximately(moveSpeed, 2f) ? 2.2f : moveSpeed;
                idleTimeMin = Mathf.Approximately(idleTimeMin, 1f) ? 0.6f : idleTimeMin;
                idleTimeMax = Mathf.Approximately(idleTimeMax, 3f) ? 1.2f : idleTimeMax;
                break;
            case Species.Cow:
                moveSpeed = Mathf.Approximately(moveSpeed, 2f) ? 1.4f : moveSpeed;
                idleTimeMin = Mathf.Approximately(idleTimeMin, 1f) ? 1.2f : idleTimeMin;
                idleTimeMax = Mathf.Approximately(idleTimeMax, 3f) ? 2.6f : idleTimeMax;
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Debug.Log("동물이 플레이어와 접촉했습니다.");
    }
}

