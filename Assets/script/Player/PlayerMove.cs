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

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // SpriteRenderer 컴포넌트 가져오기
    }

    private void Update()
    {
        Quickslote();
    }
    private void FixedUpdate()
    {
        Vector2 nextVec = inputVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
    }

    private void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
    }
    public void OnFire()
    {
        //changeColor();
       
        Debug.Log("마우스 클릭!");
        /*
        if (targetTile != null)
        {
            targetTile.AdvanceStage(); // landtiles의 AdvanceStage 메서드 호출
            Debug.Log("타일의 색상이 변경되었습니다.");
        }
        else
        {
            Debug.LogWarning("targetTile이 설정되지 않았습니다.");
        }
        */
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
    private void Click_farm(GameObject farmTile) // 테스팅 단계에 임시 변수명 이후 변수명 변경해야함
    {
        if (!equipment_01)
        {
            Debug.Log("괭이를 착용하세요");
            return;
        }

        // 농사 타일과 상호작용
        landtiles landTileComponent = farmTile.GetComponent<landtiles>();
        if (landTileComponent != null)
        {
            landTileComponent.AdvanceStage();
            Debug.Log("농사 타일과 상호작용 완료");
        }
        else
        {
            Debug.LogWarning("해당 객체에 landtiles 컴포넌트가 없습니다.");
        }
    }
    private void Click_fishing(GameObject fishingTile)
    {
        if (!equipment_02)
        {
            Debug.Log("낚시대를 착용하세요");
            return;
        }

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
    private void Quickslote()
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
    }
}
