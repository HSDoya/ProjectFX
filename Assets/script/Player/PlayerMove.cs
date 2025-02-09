using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerMove : MonoBehaviour
{
    
    public Vector2 inputVec;
    public float speed;

    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer; //색상변경
    public landtiles targetTile; //landtiles 객체 참조를 위한 public 변수로 추후 변경예정
    public Tile_Fishing tile_fishing; // 테스팅 코드 이후 변경예정 
    //장비 리스트는 이후 리스트 형태 or 타 코드로 옮겨 질 수 있음 
    private bool equipment_01 = false;
    private bool equipment_02 = false;
    private string currentEquipment = ""; // 현재 장착된 장비 ("Hoe", "Seeds", "Water")
    public bool event_time = false; // 이벤트 발생시 움직임을 막을 코드 이후 수정 필요 

    // 애니메이션 코드
    Animator anim;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // SpriteRenderer 컴포넌트 가져오기
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        Quickslote(); // 숫자 키 입력 감지
    }
    private void FixedUpdate()
    {
        Vector2 nextVec = inputVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
    }
    private void LateUpdate()
    {
        // 2024.03.27 플레이어 애니메이션 추가 코드 수정 
        // Player 애니메이션중 이동 애니메이션만 추가 이후 죽었을때 애니메이션도 추가 예정
        anim.SetFloat("Speed", inputVec.magnitude);

        if (inputVec.x != 0)
        {
            spriteRenderer.flipX = inputVec.x < 0;
        }
    }
    private void OnMove(InputValue value)
    {
        if(!event_time)
            inputVec = value.Get<Vector2>();
    }
    public void OnFire()
    {
       
        Debug.Log("마우스 클릭!");
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider != null)
        {
            GameObject clickedObject = hit.collider.gameObject;

            if (clickedObject.CompareTag("Farm"))
            {
                Click_farm(clickedObject);
            }
            else if (clickedObject.CompareTag("Fishing"))
            {
                Click_fishing(clickedObject);
            }
            else
            {
                Debug.Log("상호작용할 수 없는 객체입니다.");
            }
        }
        else
        {
            Debug.Log("클릭된 객체가 없습니다.");
        }
    }
    private void changeColor()
    {
        // 랜덤한 색상을 생성하여 적용
        Color randomColor = new Color(Random.value, Random.value, Random.value);
        spriteRenderer.color = randomColor;
    }
    private void Click_farm(GameObject farmTile)//(2025-02-05 최근 수정 박기보)
    {
        landtiles landTileComponent = farmTile.GetComponent<landtiles>();
        if (landTileComponent == null) return;

        switch (currentEquipment)
        {
            case "Hoe":
                landTileComponent.ChangeTileColor(Color.gray, 3f);
                Debug.Log("괭이를 사용하여 땅을 고름.");
                break;
            case "Seeds":
                landTileComponent.PlantSeed();
                Debug.Log("씨앗을 심었습니다!");
                break;
            case "Water":
                landTileComponent.WaterTile();
                Debug.Log("물을 뿌렸습니다! 성장 시작!");
                break;
        }
    }
    private void Click_fishing(GameObject fishingTile)
    {
        // 낚시 타일과 상호작용
        Tile_Fishing fishingComponent = fishingTile.GetComponent<Tile_Fishing>();
        if (fishingComponent != null)
        {
            fishingComponent.AdvanceStage();
            Debug.Log("낚시 타일과 상호작용 완료");
        }
        else
        {
            Debug.LogWarning("해당 객체에 Tile_Fishing 컴포넌트가 없습니다.");
        }
    }
    private void Quickslote() //(2025-02-05 박기보)장착 아이템 수정
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
             {
            equipment_01 = true;
            equipment_02 = false;
            Debug.Log(equipment_01);
            }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
            equipment_01 = false;
            equipment_02 = true;
            Debug.Log(equipment_02);
            }
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
    }
}

