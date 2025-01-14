using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerMove : MonoBehaviour
{
    
    public Vector2 inputVec;
    public float speed;

    private Rigidbody2D rigid;
    private SpriteRenderer spriteRenderer; //���󺯰�
    public landtiles targetTile; //landtiles ��ü ������ ���� public ������ ���� ���濹��
    //��� ����Ʈ�� ���� ����Ʈ ���� or Ÿ �ڵ�� �Ű� �� �� ���� 
    private bool equipment_01 = false;
    private bool equipment_02 = false;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // SpriteRenderer ������Ʈ ��������
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
        changeColor();
        Debug.Log("���콺 Ŭ��!");
        if (targetTile != null)
        {
            targetTile.AdvanceStage(); // landtiles�� AdvanceStage �޼��� ȣ��
            Debug.Log("Ÿ���� ������ ����Ǿ����ϴ�.");
        }
        else
        {
            Debug.LogWarning("targetTile�� �������� �ʾҽ��ϴ�.");
        }
    }
    private void changeColor()
    {
        // ������ ������ �����Ͽ� ����
        Color randomColor = new Color(Random.value, Random.value, Random.value);
        spriteRenderer.color = randomColor;
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
