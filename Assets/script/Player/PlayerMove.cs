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
    public Tile_Fishing tile_fishing; // �׽��� �ڵ� ���� ���濹�� 
    //��� ����Ʈ�� ���� ����Ʈ ���� or Ÿ �ڵ�� �Ű� �� �� ���� 
    private bool equipment_01 = false;
    private bool equipment_02 = false;
    private string currentEquipment = ""; // ���� ������ ��� ("Hoe", "Seeds", "Water")


    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // SpriteRenderer ������Ʈ ��������
    }

    private void Update()
    {
        Quickslote(); // ���� Ű �Է� ����
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
       
        Debug.Log("���콺 Ŭ��!");
     
        
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
                Debug.Log("��ȣ�ۿ��� �� ���� ��ü�Դϴ�.");
            }
        }
        else
        {
            Debug.Log("Ŭ���� ��ü�� �����ϴ�.");
        }
    }
    private void changeColor()
    {
        // ������ ������ �����Ͽ� ����
        Color randomColor = new Color(Random.value, Random.value, Random.value);
        spriteRenderer.color = randomColor;
    }
    private void Click_farm(GameObject farmTile) // �׽��� ���� ������ ����(�ֱ� ���� �ڱ⺸)
    {
        landtiles landTileComponent = farmTile.GetComponent<landtiles>();
        if (landTileComponent == null)
        {
            Debug.LogWarning("�ش� ��ü�� landtiles ������Ʈ�� �����ϴ�.");
            return;
        }

        switch (currentEquipment)
        {
            case "Hoe": // ����
                landTileComponent.ChangeTileColor(Color.gray, 3f); // ����(ȸ��)���� ����, 3�� �� ����
                Debug.Log("���̸� ����Ͽ� ��ȣ�ۿ��߽��ϴ�.");
                break;

            case "Seeds": // ����
                landTileComponent.ChangeTileColor(Color.yellow, 3f); // ��������� ����
                Debug.Log("������ ����Ͽ� ��ȣ�ۿ��߽��ϴ�.");
                break;

            case "Water": // ���Ѹ���
                landTileComponent.ChangeTileColor(Color.blue, 3f); // �Ķ������� ����
                Debug.Log("���� ����Ͽ� ��ȣ�ۿ��߽��ϴ�.");
                break;

            default:
                Debug.Log("��� �������� �ʾҽ��ϴ�.");
                break;
        }
    }
    private void Click_fishing(GameObject fishingTile)
    {
     

        // ���� Ÿ�ϰ� ��ȣ�ۿ�
        Tile_Fishing fishingComponent = fishingTile.GetComponent<Tile_Fishing>();
        if (fishingComponent != null)
        {
            fishingComponent.AdvanceStage();
            Debug.Log("���� Ÿ�ϰ� ��ȣ�ۿ� �Ϸ�");
        }
        else
        {
            Debug.LogWarning("�ش� ��ü�� Tile_Fishing ������Ʈ�� �����ϴ�.");
        }
    }
    private void Quickslote() //���� ������ ����
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
            Debug.Log("���� ������");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentEquipment = "Seeds";
            Debug.Log("���� ������");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentEquipment = "Water";
            Debug.Log("���Ѹ��� ������");
        }
    }
}

