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

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // SpriteRenderer 컴포넌트 가져오기
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
        changeColor();
        Debug.Log("마우스 클릭!");
        if (targetTile != null)
        {
            targetTile.AdvanceStage(); // landtiles의 AdvanceStage 메서드 호출
            Debug.Log("타일의 색상이 변경되었습니다.");
        }
        else
        {
            Debug.LogWarning("targetTile이 설정되지 않았습니다.");
        }
    }
    private void changeColor()
    {
        // 랜덤한 색상을 생성하여 적용
        Color randomColor = new Color(Random.value, Random.value, Random.value);
        spriteRenderer.color = randomColor;
    }
}
