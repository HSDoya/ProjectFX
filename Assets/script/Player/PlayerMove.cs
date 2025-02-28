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
    public landtiles landTileManager;
    private string currentEquipment = "";
    public bool event_time = false;
    Animator anim;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (!event_time)
        {
            rigid.linearVelocity = inputVec * speed;
        }
        else
        {
            rigid.linearVelocity = Vector2.zero;
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
        anim.SetBool("Fishing", false);
    }

    public void OnFire()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int tilePosition = farmTilemap.WorldToCell(mousePosition);
        TileBase clickedFarmTile = farmTilemap.GetTile(tilePosition);
        TileBase clickedWaterTile = waterTilemap.GetTile(tilePosition);

        if (clickedFarmTile != null)
        {
            Debug.Log("농사 타일 클릭됨: " + tilePosition);
            HandleFarmAction(tilePosition);
        }
        else if (clickedWaterTile != null)
        {
            Debug.Log("바다 타일 클릭됨: " + tilePosition);
            HandleFishingAction();
        }
        else
        {
            Debug.Log("타일이 감지되지 않음!");
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
    }

    private void Quickslot()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentEquipment = "Hoe";
            Debug.Log("괭이 장착됨");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentEquipment = "Seeds";
            Debug.Log("씨앗 장착됨");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentEquipment = "Water";
            Debug.Log("물뿌리개 장착됨");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentEquipment = "Harvest";
            Debug.Log("수확 도구 장착됨");
        }
    }
}
