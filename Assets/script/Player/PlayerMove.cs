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
    
    private void OnInventory()
    {
        if (inventory != null)
        {
            inventory.ToggleUI();
            Debug.Log($"인벤토리 상태: {inventory.isInventoryOpen}");
        }
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
            //HandleFishingAction();
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
    

}
