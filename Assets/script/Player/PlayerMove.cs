using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;
using Kinnly;

public class PlayerMove : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed = 5f;
    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer;
    public Tilemap farmTilemap;
    public Tilemap waterTilemap;
    public landtiles landTileManager;
    private string currentEquipment = "";

    public bool event_time;
    Animator anim;
    private GameObject collidedObject = null;

    [SerializeField] private Inventory inventory;
    [SerializeField] private Tile_Fishing fishingTile;
    [SerializeField] private ObjectSpawner objectSpawner; // 드래그 연결 필요(2025-07-27)

    // [추가] G키로 띄울 "세로 막대 미니게임" 프리팹 & 실행중 인스턴스 
    [Header("MiniGame (Vertical Bar)")]
    [SerializeField] private GameObject fishingMiniGamePrefab; // 프리팹: VerticalFishingMiniGameView 포함
    private GameObject currentFishingGame;                     // 현재 실행 중 미니게임
    // 테스트 이후 삭세 


    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        event_time = false;
    }

    // Update()에서 클릭 감지(2025.06.28 수정)
    private void Update() 
    {
        Quickslot();
        // 마우스 클릭 시 농사 행동 처리
        if (Mouse.current.leftButton.wasPressedThisFrame && !event_time)
        {
            OnMouseClick();
        }
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryDestroyNearestSpawnedObject();
        }

        //  G 키로 미니게임 시작 (테스트용)
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            StartFishingMiniGame();
        }

    }

    private void LateUpdate()
    {
        anim.SetFloat("Speed", inputVec.magnitude);
        if (inputVec.x != 0)
        {
            spriteRenderer.flipX = inputVec.x < 0;
        }
    }

    private void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
        if (!event_time)
        {
            anim.SetBool("Fishing", false);
        }
    }

    private void FixedUpdate()
    {
        if (!event_time)
            rigid.linearVelocity = inputVec * speed;
        else
            rigid.linearVelocity = Vector2.zero;
    }
    /*
private void OnMouseClick()
{
    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    Vector3Int tilePos = farmTilemap.WorldToCell(mouseWorldPos);
    tilePos.z = 0;

    if (Vector3.Distance(transform.position, farmTilemap.CellToWorld(tilePos)) <= 1.5f)
    {
        HandleFarmAction(tilePos);
    }
}
*/
    // 🔥 수정된 마우스 클릭 함수
    private void OnMouseClick()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0;

        // 동물 제거 시도
        if (currentEquipment == "Knife")
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f);
            if (hit.collider != null)
            {
                AnimalAI animal = hit.collider.GetComponent<AnimalAI>();
                if (animal != null)
                {
                    float dist = Vector2.Distance(transform.position, animal.transform.position);
                    if (dist <= 1.5f)
                    {
                        //Destroy(animal.gameObject);
                        animal.KillAndDrop(animal.transform.position);
                        Debug.Log("가축이 도살되었습니다.");
                        return;
                    }
                }
            }
        }
        // 기존 농사 처리
        Vector3Int tilePos = farmTilemap.WorldToCell(mouseWorldPos);
        tilePos.z = 0;

        if (Vector3.Distance(transform.position, farmTilemap.CellToWorld(tilePos)) <= 1.5f)
        {
            HandleFarmAction(tilePos);
        }
    }
        private void HandleFarmAction(Vector3Int tilePosition)
    {
        if (currentEquipment == "Hoe")
        {
            landTileManager.PlowSoil(tilePosition);
        }
        else if (currentEquipment == "Seeds")
        {
            landTileManager.PlantSeed(tilePosition);
        }
        else if (currentEquipment == "Water")
        {
            landTileManager.WaterTile(tilePosition);
        }
        else if (currentEquipment == "Harvest")
        {
            landTileManager.HarvestCrop(tilePosition);
        }
    }
    private void HandleFishingAction()
    {
        Debug.Log("낚시 이벤트 실행!");
        anim.SetBool("Fishing", true);
        Tile_Fishing fishingComponent = fishingTile.GetComponent<Tile_Fishing>();
        if (fishingComponent != null)
        {
            fishingComponent.AdvanceStage();
        }
    }
    private void OnInventory()
    {
        inventory.inventory_bool = !inventory.inventory_bool;
        Debug.Log("인벤토리: " + inventory.inventory_bool);
    }

    private void Quickslot()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentEquipment = "Hoe";
            event_time = false;
            Debug.Log("괭이 장착됨 ✅");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentEquipment = "Seeds";
            event_time = false;
            Debug.Log("씨앗 장착됨 ✅");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentEquipment = "Water";
            event_time = false;
            Debug.Log("물뿌리개 장착됨 ✅");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentEquipment = "Harvest";
            event_time = false;
            Debug.Log("수확 도구 장착됨 ✅");
        }// 추가: 칼 장착
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            currentEquipment = "Knife";
            event_time = false;
            Debug.Log("칼 장착됨 🗡️");
        }
}
    public void OnInteraction()
    {
        if (collidedObject != null && collidedObject.CompareTag("sea"))
        {
            HandleFishingAction();
        }
    }
    private void TryDestroyNearestSpawnedObject()
    {
        if (objectSpawner == null || objectSpawner.spawnedObjects.Count == 0) return;

        GameObject closest = null;
        float minDist = 1.5f;

        for (int i = objectSpawner.spawnedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = objectSpawner.spawnedObjects[i];

            // ★ 핵심: 이미 파괴되었는지(null인지) 먼저 확인
            if (obj == null)
            {
                objectSpawner.spawnedObjects.RemoveAt(i); // 리스트 청소
                continue;
            }

            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < minDist)
            {
                closest = obj;
                minDist = dist;
            }
        }

        // ★ 실행 직전에 한 번 더 체크
        if (closest != null)
        {
            objectSpawner.spawnedObjects.Remove(closest);
            Destroy(closest);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("sea") || collision.gameObject.CompareTag("farmTile"))
        {
            collidedObject = collision.gameObject;
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject == collidedObject)
        {
            collidedObject = null;
            Debug.Log("충돌 객체 해제됨");
        }
    }
    public void StopFishingAnimation()
    {
        anim.SetBool("Fishing", false);
    }


    // 낚시 게임 
    private void StartFishingMiniGame()
    {
        if (currentFishingGame != null) return; // 이미 실행 중이면 무시

        if (fishingMiniGamePrefab == null)
        {
            Debug.LogError("[Fishing] fishingMiniGamePrefab이 비어 있습니다. 프리팹을 연결하세요.");
            return;
        }

        Debug.Log("🎣 G 키 입력 → 세로 막대 낚시 미니게임 시작");

        // 플레이어 조작 잠깐 정지
        event_time = true;
        rigid.linearVelocity = Vector2.zero;

        // Canvas 밑에 인스턴스
        Canvas canvas = FindFirstObjectByType<Canvas>();
        Transform parent = canvas != null ? canvas.transform : null;
        currentFishingGame = Instantiate(fishingMiniGamePrefab, parent);

        // 프리팹에서 세로 뷰 컴포넌트 찾기
        var view = currentFishingGame.GetComponentInChildren<VerticalFishingMiniGameView>(true);
        if (view == null)
        {
            Debug.LogError("[Fishing] 프리팹에 VerticalFishingMiniGameView가 없습니다.");
            return;
        }

        // 열기 & 콜백
        view.Open(
            onFinished: (success) =>
            {
                if (success)
                {
                    Debug.Log("낚시 성공! (여기서 보상 지급)");
                    // TODO: Inventory.instance.Add("fish", 1);
                }
                else
                {
                    Debug.Log("낚시 실패");
                }
            },
            onClosed: () =>
            {
                if (currentFishingGame != null)
                {
                    Destroy(currentFishingGame);
                    currentFishingGame = null;
                }
                event_time = false; // 플레이어 조작 복구
                Debug.Log("낚시 미니게임 종료");
            }
        );
    }



}
