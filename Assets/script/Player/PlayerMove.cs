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

    // 클릭한 위치의 타일에서 농사 수행(2025.06.28 수정)
    private void OnMouseClick()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int tilePos = farmTilemap.WorldToCell(mouseWorldPos);
        tilePos.z = 0;

        // 클릭한 타일이 플레이어 근처인지 확인 (선택 사항)
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
        float minDist = 1.5f; // 거리 제한

        foreach (GameObject obj in objectSpawner.spawnedObjects)
        {
            if (obj == null) continue;
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < minDist)
            {
                closest = obj;
                minDist = dist;
            }
        }

        if (closest != null)
        {
            objectSpawner.spawnedObjects.Remove(closest);
            Destroy(closest);
            Debug.Log("ObjectSpawner에서 생성된 오브젝트를 제거했습니다.");
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
}
