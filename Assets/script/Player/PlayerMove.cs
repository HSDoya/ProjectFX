using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;

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
    private GameObject collidedObject = null;
    private Vector3Int lastCollidedTile = Vector3Int.zero;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }
    private void Update()
    {
        Quickslot(); // 슬롯(호미,괭이 등)
    }
    //플레이어 상하좌우 움직임 코드
    private void LateUpdate()//움직임 관련(Input시스템)
    {
        anim.SetFloat("Speed", inputVec.magnitude);
        if (inputVec.x != 0)
        {
            spriteRenderer.flipX = inputVec.x < 0;
        }
    }
    private void OnMove(InputValue value)//움직임 관련(Input시스템)
    {
        inputVec = value.Get<Vector2>();
        anim.SetBool("Fishing", false);
    }
    private void FixedUpdate()//움직임 관련(Input시스템)
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
    private void Quickslot()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentEquipment = "Hoe";
            Debug.Log("괭이 장착됨 ✅");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentEquipment = "Seeds";
            Debug.Log("씨앗 장착됨 ✅");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentEquipment = "Water";
            Debug.Log("물뿌리개 장착됨 ✅");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentEquipment = "Harvest";
            Debug.Log("수확 도구 장착됨 ✅");
        }
        event_time = false;
    }
    // 낚시 관련 코드
    public void OnInteraction()
    {
        if (collidedObject != null && collidedObject.CompareTag("sea"))
        {
            HandleFishingAction();
        }
    }
    private void HandleFishingAction()
    {
        Debug.Log("🎣 낚시 이벤트 실행!");
        anim.SetBool("Fishing", true);
    }

    public void ShowFishingUI()
    {
        Debug.Log("낚시 이벤트 UI 실행");
        event_time = true;
        StartCoroutine(FishingProcess());
    }

    private IEnumerator FishingProcess()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("낚시 완료!");
        event_time = false;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("충돌한 객체: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("sea"))
        {
            collidedObject = collision.gameObject;
        }
        if (collision.gameObject.CompareTag("farmTile"))
        {
            collidedObject = collision.gameObject;
        }
    }
}
