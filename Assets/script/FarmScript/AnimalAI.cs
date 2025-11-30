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
        // 🔒 Peck 중이면 절대 움직이지 않음
        if (peckLock || currentJob == AnimalJob.Peck)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsMoving", false);
            return;
        }

        if (currentJob != AnimalJob.Wander)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsMoving", false);
            return;
        }

        // Wander일 때만 이동
        Vector2 newPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;

        if (IsWalkable(newPos))
        {
            rb.MovePosition(newPos);
            sr.flipX = moveDir.x > 0;
            bool moving = moveDir.sqrMagnitude > 0.0001f;
            anim.SetBool("IsMoving", moving);
        }
        else
        {
            // 벽/물에 부딪히면 방향 재설정
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

//이전코드

// 코드 수정으로 인해 변경
/*void Start()
{
 StartCoroutine(MoveRoutine());

 if (audioSource != null && ambientClips != null && ambientClips.Count > 0)
    ambientCo = StartCoroutine(AmbientRoutine());
}
*/

/*
// 동물 죽음 및 아이템 드롭 코드 추가 (테스트)hs
public void KillAndDrop(Vector3? dropAt = null)
{
    // 드롭 → 오브젝트 삭제 순서
    Vector3 pos = dropAt ?? transform.position;
    SpawnDrops(pos);

    // 실제 제거
    Destroy(gameObject);
}

//아이템 드롭에서 추가 hs
private DropConfig GetDropConfigForSpecies()
{
    // 현재 this.species에 맞는 설정 찾기
    foreach (var cfg in dropTable)
        if (cfg.species == species) return cfg;
    return null;
}

// 아이템 드롭에서 추가 hs
private void SpawnDrops(Vector3 pos)
{
    var cfg = GetDropConfigForSpecies();
    if (cfg == null || cfg.prefabs == null || cfg.prefabs.Count == 0 || cfg.countRange.y <= 0)
        return;

    // 개수 보정
    int min = Mathf.Max(0, cfg.countRange.x);
    int max = Mathf.Max(min, cfg.countRange.y);

    int count = Random.Range(min, max + 1);
    for (int i = 0; i < count; i++)
    {
        var pick = cfg.prefabs[Random.Range(0, cfg.prefabs.Count)];
        if (pick != null) Instantiate(pick, pos, Quaternion.identity);
    }
}
// 코드 수정으로 인해 변경
//private void SpawnDrops(Vector3 pos)
//{
//    if (dropPrefabs == null || dropPrefabs.Count == 0 || dropCountRange.y <= 0)
//        return;

//    int count = Random.Range(dropCountRange.x, dropCountRange.y + 1);
//    for (int i = 0; i < count; i++)
//    {
//        var pick = dropPrefabs[Random.Range(0, dropPrefabs.Count)];
//        if (pick != null)
//            Instantiate(pick, pos, Quaternion.identity);
//    }
//}
// 아이템 드랍 및 죽음 코드 추가함에 따라 주석처리 
//void OnDestroy()
//{
//    // PlayerMove에서 Destroy될 때 자동 드롭함
//    if (dropPrefabs != null && dropPrefabs.Count > 0 && dropCountRange.y > 0)
//    {
//        int count = Random.Range(dropCountRange.x, dropCountRange.y + 1);
//        for (int i = 0; i < count; i++)
//        {
//            var pick = dropPrefabs[Random.Range(0, dropPrefabs.Count)];
//            if (pick != null)
//                Instantiate(pick, transform.position, Quaternion.identity);
//        }
//    }
//}

private void ApplySpeciesPreset()
{
    switch (species)
    {
        case Species.Chicken:
            moveSpeed = Mathf.Approximately(moveSpeed, 2f) ? 2.2f : moveSpeed;
            idleTimeMin = Mathf.Approximately(idleTimeMin, 1f) ? 0.6f : idleTimeMin;
            idleTimeMax = Mathf.Approximately(idleTimeMax, 3f) ? 1.2f : idleTimeMax;
            turnBias = (Mathf.Approximately(turnBias, 0.3f)) ? 0.7f : turnBias;
            avoidBias = (Mathf.Approximately(avoidBias, 0.5f)) ? 0.3f : avoidBias;
            bodyRadius = Mathf.Approximately(bodyRadius, 0.2f) ? 0.18f : bodyRadius;
            break;

        case Species.Cow:
            moveSpeed = Mathf.Approximately(moveSpeed, 2f) ? 1.4f : moveSpeed;
            idleTimeMin = Mathf.Approximately(idleTimeMin, 1f) ? 1.2f : idleTimeMin;
            idleTimeMax = Mathf.Approximately(idleTimeMax, 3f) ? 2.6f : idleTimeMax;
            turnBias = (Mathf.Approximately(turnBias, 0.3f)) ? 0.2f : turnBias;
            avoidBias = (Mathf.Approximately(avoidBias, 0.5f)) ? 0.7f : avoidBias;
            bodyRadius = Mathf.Approximately(bodyRadius, 0.2f) ? 0.28f : bodyRadius;
            break;
    }
}

// === 이동 가능 여부 공통 게이트 ===
private bool CanMoveNow()
{
    // 기존: isIdle/isPecking만 확인
    // return !isIdle && !isPecking;

    // 수정: 애니메이터의 "IsMoving" 파라미터까지 함께 검사하여
    //       "걷기" 상태일 때만 실제 이동 허용
    // 기존코드 주석처리 ↑
    return !isIdle && !isPecking && anim.GetBool("IsMoving");
}

IEnumerator MoveRoutine()
{
    while (true)
    {
        // --- 이동 단계 ---
        float moveTime = (species == Species.Cow)
            ? Random.Range(cowMoveDuration.x, cowMoveDuration.y)         // 소: 10~20초
            : Random.Range(chickenMoveDuration.x, chickenMoveDuration.y); // 닭: 5~10초

        ChooseNewDirection(force: true);
        isIdle = false;
        float t = 0f;
        while (t < moveTime)
        {
            if (!isPecking)
            {
                t += Time.deltaTime;

                // 이동 도중에도 회피/진로 보정
                if (avoidBias > 0.01f && Random.value < avoidBias * 0.05f)
                {
                    if (!IsWalkable(rb.position + moveDirection * moveSpeed * Time.deltaTime))
                        ChooseNewDirection(force: true);
                }
            }
            yield return null;
        }
        // --- 정지 단계 ---
        moveDirection = Vector2.zero;
        isIdle = true;

        float idleWait = Random.Range(idleTimeMin, idleTimeMax);
        float idleT = 0f;
        while (idleT < idleWait)
        {
            idleT += Time.deltaTime;
            yield return null;
        }
    }
}

void FixedUpdate()
{
    // Peck 중이거나 Idle이면 이동하지 않음
    if (isIdle || isPecking)
    {
        rb.linearVelocity = Vector2.zero; // 혹시 모를 관성 제거
        anim.SetBool("IsMoving", false);
        return;
    }

    // 이동 갱신
    Vector2 newPos = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
    if (IsWalkable(newPos))
    {
        rb.MovePosition(newPos);
        sr.flipX = moveDirection.x > 0;
        anim.SetBool("IsMoving", moveDirection.sqrMagnitude > 0.0001f);
    }
    else
    {
        moveDirection = Vector2.zero;
        isIdle = true;
        anim.SetBool("IsMoving", false);
    }
}


// 기존 FixedUpdate 주석 처리
// void FixedUpdate()
// {
//     if (isIdle)
//     {
//         anim.SetBool("IsMoving", false);
//         return;
//     }
//     if (avoidBias > 0.01f && Random.value < avoidBias * 0.15f)
//     {
//         if (!IsWalkable(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime))
//             ChooseNewDirection(force: true);
//     }
//     Vector2 newPos = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
//     if (IsWalkable(newPos))
//     {
//         rb.MovePosition(newPos);
//         sr.flipX = moveDirection.x > 0;
//         anim.SetBool("IsMoving", moveDirection.sqrMagnitude > 0.0001f);
//     }
//     else
//     {
//         moveDirection = Vector2.zero;
//         isIdle = true;
//         anim.SetBool("IsMoving", false);
//     }
// }
// Peck 중에는 확실히 멈추고, 끝나면 다시 걷도록(수정)
bool IsWalkable(Vector2 worldPos2D)
{
    // 타일 중앙 좌표화
    Vector3Int cell = groundTilemap.WorldToCell(worldPos2D);
    Vector3 worldCenter = groundTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

    bool hasGround = groundTilemap != null && groundTilemap.HasTile(cell);
    bool isWater = waterTilemap != null && waterTilemap.HasTile(cell);

    bool hasObstacle = false;
    if (obstacleMask.value != 0)
        hasObstacle = Physics2D.OverlapCircle(worldCenter, bodyRadius, obstacleMask);

    return hasGround && !isWater && !hasObstacle;
}
void ChooseNewDirection(bool force = false)
{
    // turnBias가 높을수록 방향을 더 자주/랜덤하게 바꿈
    if (!force && Random.value > (0.5f + turnBias * 0.5f))
        return;

    int r = Random.Range(0, 4);
    switch (r)
    {
        case 0: moveDirection = Vector2.up; break;
        case 1: moveDirection = Vector2.down; break;
        case 2: moveDirection = Vector2.left; break;
        case 3: moveDirection = Vector2.right; break;
    }
}
private IEnumerator AmbientRoutine()
{
    while (true)
    {
        float wait = Random.Range(ambientIntervalRange.x, ambientIntervalRange.y);
        yield return new WaitForSeconds(wait);

        if (audioSource != null && ambientClips != null && ambientClips.Count > 0)
        {
            var clip = ambientClips[Random.Range(0, ambientClips.Count)];
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
// 닭 모이쪼기: 시작~끝 동안 완전 정지 + Animator 하드락(IsPecking)
private IEnumerator ChickenPeckRoutine()
{
    while (true)
    {
        float wait = Random.Range(chickenPeckIntervalRange.x, chickenPeckIntervalRange.y);
        yield return new WaitForSeconds(wait);

        if (peckOnlyWhenIdle && !isIdle) continue;
        if (isPecking) continue;

        // 이동 중이면 쪼기 생략(강제 쪼기 원하면 아래 줄 주석)
        if (moveDirection.sqrMagnitude > 0.0001f) continue;

        isPecking = true;
        anim.SetBool("IsPecking", true);  // ✅ Animator 전이에서 Walk 금지 조건으로 사용

        // 완전 정지 + Walk 끄고 → Peck 트리거
        moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsMoving", false);
        anim.ResetTrigger("Peck");
        anim.SetTrigger("Peck");

        // 3~4초 랜덤 유지
        float peckDur = Random.Range(peckDurationRange.x, peckDurationRange.y);
        yield return new WaitForSeconds(peckDur);

        // 종료: 락 해제 후 자연 이동 재개(고정된 true 세팅은 피함)
        isPecking = false;
        anim.SetBool("IsPecking", false);
        isIdle = false;
        ChooseNewDirection(force: true);
        // IsMoving은 FixedUpdate/이동 로직이 자연스럽게 설정
    }
}

// (선택) 애니메이션 이벤트 사용 시: Peck 마지막 프레임에서 호출
public void OnPeckEnd()
{
    isPecking = false;
    anim.SetBool("IsPecking", false);
    isIdle = false;
    ChooseNewDirection(force: true);
}

private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player"))
    {
        Debug.Log("접촉했습니다.");
    }
}
}
*/