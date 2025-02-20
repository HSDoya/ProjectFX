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
    private Animator animator; // 추가된 코드: 애니메이션 컨트롤러 추가

    private string currentEquipment = ""; // 현재 장착된 장비

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // 추가된 코드: Animator 가져오기
    }

    private void Update()
    {
        Quickslot();
    }

    private void FixedUpdate()
    {
        Vector2 nextVec = inputVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);

        UpdateAnimation(); // 추가된 코드: 애니메이션 업데이트
    }

    private void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
    }

    private void UpdateAnimation() // 추가된 코드: 애니메이션 적용
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
            Debug.Log("상호작용할 수 없는 객체입니다.");
        }
    }

    private void ClickFarm(Vector3 worldPosition)
    {
        switch (currentEquipment)
        {
            case "Hoe":
                Debug.Log("괭이를 사용하여 땅을 고름.");
                break;
            case "Seeds":
                Debug.Log("씨앗을 심었습니다!");
                break;
            case "Water":
                Debug.Log("물을 뿌렸습니다! 성장 시작!");
                break;
            case "Harvest":
                Debug.Log("작물을 수확했습니다!");
                break;
        }
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


using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance; // 싱글톤 패턴 적용

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
            Destroy(gameObject); // 중복 생성 방지
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
            yield return new WaitForSeconds(10f); // 10초마다 날씨 변경
            int randomWeather = Random.Range(0, 3);
            currentWeather = (WeatherType)randomWeather;
            Debug.Log($"현재 날씨: {currentWeather}");
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
        InvokeRepeating(nameof(SpawnFish), 5f, Random.Range(5f, 15f)); // 물고기 랜덤 생성
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
            waterTilemap.SetTile(tilePosition, null); // 물고기 잡기
            Debug.Log("물고기를 잡았습니다!");
            return true;
        }
        return false;
    }

    private Vector3Int GetRandomWaterTile()
    {
        // TODO: 랜덤한 물 타일 위치 가져오기
        return new Vector3Int(0, 0, 0);
    }
}
     */


}
