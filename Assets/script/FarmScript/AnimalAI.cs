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

    [Header("Movement")]
    [Tooltip("기본 이동 속도")]
    public float moveSpeed = 2f;
    [Tooltip("정지(아이들) 최소/최대 시간")]
    public float idleTimeMin = 1f;
    public float idleTimeMax = 3f;
    [Tooltip("방향을 자주 바꾸는 경향(0=거의 안바꿈, 1=매우 자주)")]
    [Range(0f, 1f)] public float turnBias = 0.3f;
    [Tooltip("장애물/물 타일 회피 강도(값이 클수록 회피 성향↑)")]
    [Range(0f, 1f)] public float avoidBias = 0.5f;

    [Header("Collision/Physics")]
    [Tooltip("몸통 판정 반경(장애물 탐지 반경 기준)")]
    public float bodyRadius = 0.2f;
    [Tooltip("장애물 레이어 지정(없으면 비워두세요 → 무시)")]
    public LayerMask obstacleMask;

    [Header("Ambient (Optional)")]
    public AudioSource audioSource;               // (선택) 붙이면 주기적으로 울음소리 추가
    public List<AudioClip> ambientClips;          // 동물 울음소리 추가
    public Vector2 ambientIntervalRange = new Vector2(6f, 12f);

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
    private Coroutine ambientCo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

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
    }

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
        // 이미 인스펙터에서 수동으로 바꾼 값은 그대로 쓰고 싶다면 아래 주석 처리 가능
        switch (species)
        {
            case Species.Chicken:
                moveSpeed = Mathf.Approximately(moveSpeed, 2f) ? 2.2f : moveSpeed; // 기본값일 때만 살짝 조정함
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
    IEnumerator MoveRoutine()
    {
        while (true)
        {
            // 이동
            ChooseNewDirection();
            isIdle = false;
            yield return new WaitForSeconds(Random.Range(2f, 4f));

            // 정지
            moveDirection = Vector2.zero;
            isIdle = true;
            yield return new WaitForSeconds(Random.Range(idleTimeMin, idleTimeMax));
        }
    }
    void FixedUpdate()
    {
        if (isIdle)
        {
            anim.SetBool("IsMoving", false);
            return;
        }
        // 회피(물/장애물) 성향이 높을수록 방향 보정 시도하면 됨
        if (avoidBias > 0.01f && Random.value < avoidBias * 0.15f)
        {
            if (!IsWalkable(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime))
                ChooseNewDirection(force: true);
        }
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
    private void OnTriggerEnter2D(Collider2D other)  // 추후 상호작용 확장
    {
        if (other.CompareTag("Player"))
        {
            // 터치 로그 정도만 유지(원 코드 호환)
            Debug.Log("접촉했습니다.");
        }
    }

}
