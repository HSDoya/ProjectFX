using UnityEngine;

public class TestCode : MonoBehaviour
{
    //이전코드(AnimalAI)

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
}
