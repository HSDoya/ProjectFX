using UnityEngine;

public class Testcode : MonoBehaviour
{
    /*
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed;

    private Rigidbody2D rigid;
    private Animator animator; // �߰��� �ڵ�: �ִϸ��̼� ��Ʈ�ѷ� �߰�

    private string currentEquipment = ""; // ���� ������ ���

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // �߰��� �ڵ�: Animator ��������
    }

    private void Update()
    {
        Quickslot();
    }

    private void FixedUpdate()
    {
        Vector2 nextVec = inputVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);

        UpdateAnimation(); // �߰��� �ڵ�: �ִϸ��̼� ������Ʈ
    }

    private void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
    }

    private void UpdateAnimation() // �߰��� �ڵ�: �ִϸ��̼� ����
    {
        animator.SetFloat("MoveX", inputVec.x);
        animator.SetFloat("MoveY", inputVec.y);
        animator.SetBool("IsMoving", inputVec.magnitude > 0);
    }

    public void OnFire()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag("Farm"))
        {
            ClickFarm(hit.point);
        }
        else
        {
            Debug.Log("��ȣ�ۿ��� �� ���� ��ü�Դϴ�.");
        }
    }

    private void ClickFarm(Vector3 worldPosition)
    {
        switch (currentEquipment)
        {
            case "Hoe":
                Debug.Log("���̸� ����Ͽ� ���� ��.");
                break;
            case "Seeds":
                Debug.Log("������ �ɾ����ϴ�!");
                break;
            case "Water":
                Debug.Log("���� �ѷȽ��ϴ�! ���� ����!");
                break;
            case "Harvest":
                Debug.Log("�۹��� ��Ȯ�߽��ϴ�!");
                break;
        }
    }

    private void Quickslot()
    {
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
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentEquipment = "Harvest";
            Debug.Log("��Ȯ ���� ������");
        }
    }
}


using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance; // �̱��� ���� ����

    public enum WeatherType { Sunny, Rainy, Cloudy }
    public WeatherType currentWeather;

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject); // �ߺ� ���� ����
        }
    }

    private void Start()
    {
        StartCoroutine(ChangeWeather());
    }

    private IEnumerator ChangeWeather()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f); // 10�ʸ��� ���� ����
            int randomWeather = Random.Range(0, 3);
            currentWeather = (WeatherType)randomWeather;
            Debug.Log($"���� ����: {currentWeather}");
        }
    }
}

using UnityEngine;
using UnityEngine.Tilemaps;

public class Farm : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase farmableTile; 
    public TileBase wetSoilTile;
    public TileBase seedTile;
    public TileBase sproutTile;
    public TileBase grownPlantTile;
    public TileBase harvestableTile;

    public void PlowSoil(Vector3 worldPosition)
    {
        Vector3Int tilePosition = tilemap.WorldToCell(worldPosition);
        tilemap.SetTile(tilePosition, farmableTile);
    }

    public void WaterSoil(Vector3 worldPosition)
    {
        Vector3Int tilePosition = tilemap.WorldToCell(worldPosition);
        if (tilemap.GetTile(tilePosition) == farmableTile)
        {
            tilemap.SetTile(tilePosition, wetSoilTile);
        }
    }

    public void PlantSeed(Vector3 worldPosition)
    {
        Vector3Int tilePosition = tilemap.WorldToCell(worldPosition);
        if (tilemap.GetTile(tilePosition) == wetSoilTile)
        {
            tilemap.SetTile(tilePosition, seedTile);
        }
    }

    public void HarvestPlant(Vector3 worldPosition)
    {
        Vector3Int tilePosition = tilemap.WorldToCell(worldPosition);
        if (tilemap.GetTile(tilePosition) == harvestableTile)
        {
            tilemap.SetTile(tilePosition, farmableTile);
        }
    }
}

using UnityEngine;
using UnityEngine.Tilemaps;

public class Tile_Fishing : MonoBehaviour
{
    public Tilemap waterTilemap;
    public TileBase fishTile;

    private Vector3Int fishPosition;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnFish), 5f, Random.Range(5f, 15f)); // ����� ���� ����
    }

    private void SpawnFish()
    {
        fishPosition = GetRandomWaterTile();
        if (fishPosition != Vector3Int.zero)
        {
            waterTilemap.SetTile(fishPosition, fishTile);
        }
    }

    public bool TryCatchFish(Vector3 worldPosition)
    {
        Vector3Int tilePosition = waterTilemap.WorldToCell(worldPosition);
        if (tilePosition == fishPosition)
        {
            waterTilemap.SetTile(tilePosition, null); // ����� ���
            Debug.Log("����⸦ ��ҽ��ϴ�!");
            return true;
        }
        return false;
    }

    private Vector3Int GetRandomWaterTile()
    {
        // TODO: ������ �� Ÿ�� ��ġ ��������
        return new Vector3Int(0, 0, 0);
    }
}
     */


}
