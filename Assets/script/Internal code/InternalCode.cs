using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 스타듀밸리 스타일의 건물 출입(씬 워프) + 실내 그리드 가구 배치 통합 매니저
/// </summary>
public class InternalCode : MonoBehaviour
{
    public static InternalCode instance;

    // 씬 전환 시 다음 씬에서 플레이어가 등장할 출입구 ID ([B7] 단일 소유)
    private static string pendingSpawnID = "";

    [Header("1.List of entry/exit points (warps) in the current scene")]
    [Tooltip("이 씬에 존재하는 모든 문/출구 및 도착 지점을 등록합니다.")]
    public List<WarpPoint> warpPoints = new List<WarpPoint>();

    [Header("2.Tilemap for interior furniture layout ([B1] layer separated)")]
    [Tooltip("가구 배치가 가능한 실내 바닥 타일맵 (야외 씬에서는 비워두세요)")]
    public Tilemap floorTilemap;
    [Tooltip("벽, 카운터, 현관 앞 등 가구 배치가 금지되는 타일맵")]
    public Tilemap blockedTilemap;

    [Header("3.Style Placement Rules")]
    public Transform playerTransform;
    [Tooltip("플레이어로부터 가구를 설치/회수할 수 있는 최대 거리")]
    public float interactRange = 2.2f;
    [Tooltip("플레이어나 기존 오브젝트와 겹침을 검사할 레이어")]
    public LayerMask obstacleLayer;
    [Tooltip("출입구(워프) 주변 몇 칸 이내에는 가구 설치를 금지할지 설정 (갇힘 방지)")]
    public float doorSafeRadius = 1.2f;

    [Header("4.Placement Preview (Ghost) Settings")]
    public SpriteRenderer previewRenderer;
    public Color validColor = new Color(0.2f, 1f, 0.2f, 0.55f);
    public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.55f);

    // 내부 데이터 구조체: 출입구 겸 도착 스폰 포인트
    [System.Serializable]
    public class WarpPoint
    {
        [Tooltip("이 지점의 고유 이름 (예: Farm_HouseDoor, House_ExitMat)")]
        public string pointID;
        [Tooltip("플레이어가 도착할 위치 및 출입 트리거 기준 Transform")]
        public Transform pointTransform;
        [Tooltip("이동할 대상 씬 이름 (단순 도착 전용 지점이면 비워둠)")]
        public string targetSceneName;
        [Tooltip("이동할 대상 씬에서 도착할 pointID")]
        public string targetPointID;
        [Tooltip("트리거 반경 (플레이어가 이 거리 안으로 들어오면 감지)")]
        public float triggerRadius = 0.7f;
        [Tooltip("체크하면 밟자마자 즉시 이동(실내 출구 매트용), 해제하면 E키로 이동(야외 문용)")]
        public bool warpOnStep = false;
    }

    // 내부 데이터 구조체: 설치된 가구 추적 데이터
    private class PlacedItemRecord
    {
        public string itemID;
        public GameObject instanceObj;
        public int rotationStep; // 0, 1, 2, 3 (90도 단위)
    }

    // 현재 손에 든 배치 가능 아이템 정보
    private bool isPlacementMode = false;
    private string currentItemID = "";
    private GameObject currentPrefab = null;
    private int currentRotationStep = 0;

    // 셀 좌표별 배치된 가구 딕셔너리 (단일 소유권)
    private Dictionary<Vector3Int, PlacedItemRecord> placedFurnitures = new Dictionary<Vector3Int, PlacedItemRecord>();
    private Camera mainCam;

    private void Awake()
    {
        // 씬마다 각자의 타일맵과 워프 포인트를 가지므로 현재 씬 매니저로 갱신 ([T5] 방어)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        mainCam = Camera.main;

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (previewRenderer != null)
            previewRenderer.gameObject.SetActive(false);

        // 다른 씬에서 문을 통해 넘어왔다면 해당 도착 지점으로 플레이어 즉시 배치
        ApplyPendingSpawnPosition();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. 건물 출입구(워프) 감지 및 이동 처리
        HandleWarpPoints();

        // 2. 실내 바닥 타일맵이 연결된 경우에만 하우징(설치/회수) 모드 동작
        if (floorTilemap != null)
        {
            HandleInteriorHousing();
        }
    }

    // [파트 A] 출입구(워프) 시스템

    private void ApplyPendingSpawnPosition()
    {
        if (string.IsNullOrEmpty(pendingSpawnID) || playerTransform == null) return;

        foreach (var wp in warpPoints)
        {
            if (wp.pointID == pendingSpawnID && wp.pointTransform != null)
            {
                playerTransform.position = wp.pointTransform.position;
                break;
            }
        }

        pendingSpawnID = "";
    }

    private void HandleWarpPoints()
    {
        if (warpPoints == null || warpPoints.Count == 0) return;

        bool interactPressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);

        foreach (var wp in warpPoints)
        {
            if (wp.pointTransform == null || string.IsNullOrEmpty(wp.targetSceneName)) continue;

            float dist = Vector2.Distance(playerTransform.position, wp.pointTransform.position);
            if (dist <= wp.triggerRadius)
            {
                if (wp.warpOnStep || interactPressed)
                {
                    ExecuteSceneWarp(wp.targetSceneName, wp.targetPointID);
                    return;
                }
            }
        }
    }

    public void ExecuteSceneWarp(string sceneName, string destinationPointID)
    {
        pendingSpawnID = destinationPointID;
        SceneManager.LoadScene(sceneName);
    }

    // [파트 B] 실내 그리드 가구 배치 및 회수 시스템

    private void HandleInteriorHousing()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || Mouse.current == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector3Int targetCell = floorTilemap.WorldToCell(mouseWorldPos);
        Vector3 cellCenter = floorTilemap.GetCellCenterWorld(targetCell);
        cellCenter.z = 0f;

        // 배치 모드일 때: 회전(R키) + 고스트 미리보기 + 좌클릭 설치
        if (isPlacementMode && currentPrefab != null)
        {
            // R키로 가구 90도 회전 (스타듀밸리 우클릭/R키 회전 감각)
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                currentRotationStep = (currentRotationStep + 1) % 4;
            }

            bool canPlace = CanPlaceAt(targetCell, cellCenter);

            if (previewRenderer != null)
            {
                previewRenderer.gameObject.SetActive(true);
                previewRenderer.transform.position = cellCenter;
                previewRenderer.transform.rotation = Quaternion.Euler(0f, 0f, currentRotationStep * -90f);
                previewRenderer.color = canPlace ? validColor : invalidColor;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame && canPlace)
            {
                PlaceItemAt(targetCell, cellCenter);
            }
        }
        else
        {
            if (previewRenderer != null && previewRenderer.gameObject.activeSelf)
                previewRenderer.gameObject.SetActive(false);
        }

        // 우클릭 시 커서 위치의 설치된 가구 회수 ([S6] 일원화된 회수 경로)
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (Vector2.Distance(playerTransform.position, cellCenter) <= interactRange)
            {
                TryRemoveFurnitureAt(targetCell);
            }
        }
    }

    /// <summary>
    /// 퀵슬롯에서 설치 가능한 가구 아이템을 선택했을 때 외부(PlayerQuickSlot 등)에서 호출
    /// </summary>
    public void EnterPlacementMode(string itemID, GameObject prefab, Sprite previewSprite)
    {
        currentItemID = itemID;
        currentPrefab = prefab;
        currentRotationStep = 0;
        isPlacementMode = (prefab != null);

        if (previewRenderer != null && previewSprite != null)
        {
            previewRenderer.sprite = previewSprite;
        }
    }

    public void ExitPlacementMode()
    {
        isPlacementMode = false;
        currentItemID = "";
        currentPrefab = null;
        currentRotationStep = 0;

        if (previewRenderer != null)
            previewRenderer.gameObject.SetActive(false);
    }

    public bool CanPlaceAt(Vector3Int cell, Vector2 worldPos)
    {
        // 1. 실내 바닥 타일이 존재하는지 확인
        if (floorTilemap == null || !floorTilemap.HasTile(cell)) return false;

        // 2. 벽/배치 금지 타일맵에 걸리는지 확인
        if (blockedTilemap != null && blockedTilemap.HasTile(cell)) return false;

        // 3. 이미 다른 가구가 배치된 칸인지 확인
        if (placedFurnitures.ContainsKey(cell)) return false;

        // 4. 플레이어 상호작용 거리 이내인지 확인
        if (Vector2.Distance(playerTransform.position, worldPos) > interactRange) return false;

        // 5. 출입구(현관 매트) 앞을 막아서 플레이어가 갇히지 않도록 안전 반경 보호
        foreach (var wp in warpPoints)
        {
            if (wp.pointTransform != null && Vector2.Distance(wp.pointTransform.position, worldPos) < doorSafeRadius)
                return false;
        }

        // 6. 플레이어 몸체나 다른 물리 충돌체와 겹치는지 확인
        Collider2D hit = Physics2D.OverlapBox(worldPos, new Vector2(0.7f, 0.7f), 0f, obstacleLayer);
        if (hit != null && !hit.isTrigger) return false;

        return true;
    }

    private void PlaceItemAt(Vector3Int cell, Vector3 worldPos)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, currentRotationStep * -90f);
        GameObject obj = Instantiate(currentPrefab, worldPos, rot);

        placedFurnitures[cell] = new PlacedItemRecord
        {
            itemID = currentItemID,
            instanceObj = obj,
            rotationStep = currentRotationStep
        };

        // TODO: 인벤토리/퀵슬롯에서 currentItemID 1개 차감 처리 연결
        // 예: Inventory.instance.RemoveItem(currentItemID, 1);
    }

    public bool TryRemoveFurnitureAt(Vector3Int cell)
    {
        if (placedFurnitures.TryGetValue(cell, out PlacedItemRecord record))
        {
            if (record.instanceObj != null)
                Destroy(record.instanceObj);

            placedFurnitures.Remove(cell);

            // TODO: 회수한 가구(record.itemID) 1개를 인벤토리로 돌려주는 처리 연결
            // 예: Inventory.instance.AddItem(ItemDataManager.instance.GetItemDataByID(record.itemID));
            return true;
        }
        return false;
    }

    // 에디터 씬 뷰에서 출입구 위치와 반경을 시각적으로 확인하기 위한 기즈모
    private void OnDrawGizmos()
    {
        if (warpPoints == null) return;

        foreach (var wp in warpPoints)
        {
            if (wp.pointTransform == null) continue;
            Gizmos.color = wp.warpOnStep ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(wp.pointTransform.position, wp.triggerRadius);
        }
    }
}