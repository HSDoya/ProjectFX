using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class AnimalAI : MonoBehaviour
{
    public enum Species { Chicken, Cow }

    [Header("Basic")]
    [SerializeField] private Species species = Species.Chicken;

    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap waterTilemap;

    [Header("Movement (Common)")]
    //기본 이동 속도(프리셋으로 종별 튜닝됨)
    public float moveSpeed = 2f;

    //정지 최소/최대 시간
    public float idleTimeMin = 1f;
    public float idleTimeMax = 3f;

    //방향을 자주 바꾸는 경향(0=거의 안바꿈, 1=매우 자주)
    [Range(0f, 1f)] public float turnBias = 0.3f;

    //장애물/물 타일 회피 강도, 값이 클수록 회피 성향
    [Range(0f, 1f)] public float avoidBias = 0.5f;

    [Header("Movement Duration By Species")]
    //닭: 한 번 이동할 때 지속 시간 범위(초)
    public Vector2 chickenMoveDuration = new Vector2(3f, 7f);

    //소: 한 번 이동할 때 지속 시간 범위(초)
    public Vector2 cowMoveDuration = new Vector2(10f, 20f);

    [Header("Collision/Physics")]
    //몸통 판정 반경(장애물 탐지 반경 기준
    public float bodyRadius = 0.2f;
    //장애물 레이어 지정(없으면 비워주삼→ 무시)
    public LayerMask obstacleMask;

    [Header("Ambient (Optional)")]
    public AudioSource audioSource;
    public List<AudioClip> ambientClips;
    public Vector2 ambientIntervalRange = new Vector2(6f, 12f);

    [Header("Chicken Peck (Optional)")]
    //종이 Chicken일 때만 사용. 랜덤 간격으로 'Peck' 트리거 발동
    public bool enablePecking = true;

    //모이쪼기 시도 간격(초)
    public Vector2 chickenPeckIntervalRange = new Vector2(6f, 12f);

    //Idle일 때만 쪼도록 제한
    public bool peckOnlyWhenIdle = true;

    //Peck 유지시간 범위(초) - 매번 랜덤
    public Vector2 peckDurationRange = new Vector2(3f, 4f);

    // 1) 종별 드롭 설정 구조
    [System.Serializable]
    public class DropConfig
    {
        public Species species;                  // Chicken, Cow, ...(확장)
        public List<GameObject> prefabs;         // 이 종이 떨굴 프리팹들(랜덤)
        public Vector2Int countRange = new Vector2Int(1, 1); // 개수 범위
    }
    [Header("Drops By Species")]
    public List<DropConfig> dropTable = new List<DropConfig>();

    //이후 확장성을 위해 Item 드롭에 대해 다른 방식으로 접근하는 코드 추가
    //[Header("Drops (Optional)")]
    //[Tooltip("파괴 시 월드에 떨어뜨릴 프리팹들(랜덤)")]
    //public List<GameObject> dropPrefabs;
    //[Tooltip("드롭 개수 범위(min, max 포함)")]
    //public Vector2Int dropCountRange = new Vector2Int(0, 2);

    // Runtime
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Vector2 moveDirection;
    private bool isIdle = false;
    private bool isPecking = false;
    private Coroutine ambientCo;
    private Coroutine peckCo; // 닭 모이쪼기 루틴 핸들

    // 코드 수정으로 인해 변경
    // private Coroutine ambientCo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // 시작 시 절대 움직이지 않도록 강제 초기화
        moveDirection = Vector2.zero;
        isIdle = true;
        isPecking = false;
        anim.ResetTrigger("Peck");
        anim.SetBool("IsPecking", false); //Animator에 동일 bool 파라미터 추가 필요
        anim.SetBool("IsMoving", false);

        // 콜라이더는 트리거로(플레이어 Knife 클릭 판정과 충돌 방지)
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;

        // 2D 세팅 추천값(필수는 아님)
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        ApplySpeciesPreset(); // 종별 기본값 적용(이미 인스펙터에서 수동 조정했다면 그대로 두면됨)
    }

    void Start()
    {
        StartCoroutine(MoveRoutine());

        if (audioSource != null && ambientClips != null && ambientClips.Count > 0)
            ambientCo = StartCoroutine(AmbientRoutine());

        // 닭 전용 모이쪼기 루틴 시작
        if (species == Species.Chicken && enablePecking)
            peckCo = StartCoroutine(ChickenPeckRoutine());
    }
    private void OnDisable()
    {
        if (peckCo != null) StopCoroutine(peckCo);
        if (ambientCo != null) StopCoroutine(ambientCo);
    }

    // 코드 수정으로 인해 변경
    /*void Start()
    {
     StartCoroutine(MoveRoutine());

     if (audioSource != null && ambientClips != null && ambientClips.Count > 0)
        ambientCo = StartCoroutine(AmbientRoutine());
    }
    */

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