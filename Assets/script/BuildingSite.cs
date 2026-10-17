using System.Collections.Generic;
using UnityEngine;

// 설계도를 땅에 설치하면 생기는 건설 현장. 재료를 다 받으면 완공된다.
//
// 필요 재료와 완성 외형은 CSV가 아니라 이 컴포넌트(= 프리팹)가 들고 있다.
// PlaceableFurniture.footprintSize와 같은 방침이다 - 설치/건설 전용 정보는 프리팹이 소유한다.
// (건물 종류가 많아지면 그때 CSV + 베이커로 옮기면 된다.)
public class BuildingSite : MonoBehaviour
{
    [System.Serializable]
    public class Requirement
    {
        public string itemID;
        public int amount = 1;
        [System.NonSerialized] public int deposited;
    }

    [Header("건물 정보")]
    public string buildingName = "움집";

    [Header("필요 재료")]
    public List<Requirement> requirements = new List<Requirement>();

    [Header("완성 외형")]
    [Tooltip("비어 있으면 코드로 만든 플레이스홀더를 쓴다. 실제 아트가 생기면 여기에 넣으면 된다.")]
    public Sprite completedSprite;
    [Tooltip("건설 중 외형의 불투명도")]
    [Range(0f, 1f)] public float underConstructionAlpha = 0.45f;

    [Header("철거 환급")]
    [Range(0f, 1f)] public float refundRate = 1f;

    public bool IsComplete { get; private set; }

    // 아직 현장에 남아 있는 투입 재료가 있는지. 회수할 때 이게 true면 현장을 없애면 안 된다.
    public bool HasDeposited
    {
        get
        {
            foreach (var req in requirements)
                if (req != null && req.deposited > 0) return true;
            return false;
        }
    }

    // UI 제목에 쓸 이름. TMP 기본 폰트에 한글 글리프가 없어서(IngredientRowUI 주석 참고)
    // buildingName("움집")을 그대로 띄우면 글자가 깨진다. 그래서 설치에 쓴 아이템의 영문 itemID를
    // 우선 쓰고, 한글 폰트가 들어오면 여기만 buildingName으로 바꾸면 된다.
    public string UiLabel
    {
        get
        {
            var placeable = GetComponent<PlaceableFurniture>();
            if (placeable != null && placeable.sourceItem != null) return placeable.sourceItem.itemID;
            return buildingName;
        }
    }

    // ───────────────────────── 현장 목록 ─────────────────────────

    // 살아 있는 건설 현장 전부. "가장 가까운 현장"을 R키(재료 투입)와 UI가 각자 찾으면 서로 다른
    // 현장을 가리킬 수 있으므로, 목록과 탐색을 여기 한 곳에서만 소유한다.
    // 매 프레임 Physics2D로 훑지 않아도 되니 UI가 상시 갱신해도 부담이 없다.
    private static readonly List<BuildingSite> activeSites = new List<BuildingSite>();

    private void OnEnable()
    {
        if (!activeSites.Contains(this)) activeSites.Add(this);
    }

    private void OnDisable()
    {
        activeSites.Remove(this);
    }

    /// <summary>
    /// from에서 range 안에 있는 가장 가까운 건설 현장. 없으면 null.
    /// </summary>
    public static BuildingSite FindNearest(Vector2 from, float range, bool excludeComplete = true)
    {
        BuildingSite closest = null;
        float minDist = range;

        foreach (var site in activeSites)
        {
            if (site == null) continue;
            if (excludeComplete && site.IsComplete) continue;

            float dist = Vector2.Distance(from, site.transform.position);
            if (dist <= minDist)
            {
                closest = site;
                minDist = dist;
            }
        }

        return closest;
    }

    private SpriteRenderer spriteRenderer;
    private Sprite placeholderSite;
    private Sprite placeholderDone;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 아트가 아직 없어서 스프라이트가 비어 있으면 코드로 만들어 쓴다
        // (프로젝트에 이미 PlayerMove가 커서 스프라이트를 이렇게 만들고 있다).
        if (spriteRenderer != null)
        {
            if (spriteRenderer.sprite == null)
            {
                placeholderSite = BuildPlaceholder(new Color(0.75f, 0.68f, 0.45f), true);
                spriteRenderer.sprite = placeholderSite;
            }
            ApplyConstructionLook();
        }
    }

    // ───────────────────────── 재료 투입 ─────────────────────────

    /// <summary>
    /// 인벤토리에서 아직 모자란 재료를 있는 만큼 가져온다. 실제로 하나라도 넣었으면 true.
    /// </summary>
    public bool DepositFromInventory()
    {
        if (IsComplete || Inventory.instance == null) return false;

        bool any = false;
        foreach (var req in requirements)
        {
            if (req == null || string.IsNullOrEmpty(req.itemID)) continue;

            int need = req.amount - req.deposited;
            if (need <= 0) continue;

            int have = Inventory.instance.GetItemCount(req.itemID);
            int take = Mathf.Min(need, have);
            if (take <= 0) continue;

            // 실제로 빠졌을 때만 적립한다. 먼저 적립하면 소모에 실패했을 때 재료가 공짜로 들어간다.
            if (Inventory.instance.TryConsumeItems(req.itemID, take))
            {
                req.deposited += take;
                any = true;
            }
        }

        if (any)
        {
            Debug.Log($"[{buildingName}] {ProgressText()}");
            if (IsSatisfied()) Complete();
        }
        else if (!IsSatisfied())
        {
            Debug.Log($"[{buildingName}] 넣을 재료가 없습니다. {ProgressText()}");
        }

        return any;
    }

    public bool IsSatisfied()
    {
        foreach (var req in requirements)
            if (req != null && req.deposited < req.amount) return false;
        return true;
    }

    public string ProgressText()
    {
        var parts = new List<string>();
        foreach (var req in requirements)
        {
            if (req == null || string.IsNullOrEmpty(req.itemID)) continue;
            parts.Add($"{req.itemID} {req.deposited}/{req.amount}");
        }
        return parts.Count > 0 ? string.Join(" · ", parts) : "재료 없음";
    }

    // ───────────────────────── 완공 ─────────────────────────

    private void Complete()
    {
        if (IsComplete) return;
        IsComplete = true;

        if (spriteRenderer != null)
        {
            if (completedSprite != null)
            {
                spriteRenderer.sprite = completedSprite;
            }
            else
            {
                placeholderDone = BuildPlaceholder(new Color(0.55f, 0.38f, 0.24f), false);
                spriteRenderer.sprite = placeholderDone;
            }
            spriteRenderer.color = Color.white;
        }

        Debug.Log($"[{buildingName}] 완공되었습니다!");
    }

    private void ApplyConstructionLook()
    {
        Color c = spriteRenderer.color;
        c.a = underConstructionAlpha;
        spriteRenderer.color = c;
    }

    // ───────────────────────── 철거 ─────────────────────────

    /// <summary>
    /// 투입된 재료를 환급한다. 인벤토리에 못 넣으면 그만큼은 현장에 남겨둔다(증발 방지).
    /// </summary>
    public void RefundDeposited()
    {
        if (Inventory.instance == null || ItemDataManager.instance == null) return;

        foreach (var req in requirements)
        {
            if (req == null || req.deposited <= 0) continue;

            int give = Mathf.FloorToInt(req.deposited * refundRate);
            if (give <= 0) { req.deposited = 0; continue; }

            ItemData data = ItemDataManager.instance.GetItemDataByID(req.itemID.Trim());
            if (data == null) continue;

            // 전부 들어갈 수 있을 때만 넣는다 - AddItem은 일부만 넣고 false를 반환할 수 있다.
            if (!Inventory.instance.CanAcceptItem(data, give))
            {
                Debug.Log($"[{buildingName}] 인벤토리가 가득 차서 {data.displayName} {give}개를 환급하지 못했습니다.");
                continue;
            }

            Inventory.instance.AddItem(new Item(data, give));
            req.deposited = 0;
        }
    }

    // ───────────────────────── 플레이스홀더 외형 ─────────────────────────

    // 실제 아트가 없는 동안 쓸 임시 스프라이트. 건설 중이면 테두리만, 완공이면 꽉 채운 모양.
    private Sprite BuildPlaceholder(Color color, bool outlineOnly)
    {
        const int size = 32;
        const int border = 3;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < border || x >= size - border || y < border || y >= size - border;
                Color c = color;
                if (outlineOnly && !isBorder) c.a = 0.25f;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        // 1 유닛 = 32픽셀. 스프라이트 크기는 아래에서 footprint에 맞춰 늘린다.
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // 설치하는 쪽에서 footprint를 알려주면 외형과 콜라이더를 그 크기에 맞춘다.
    public void FitTo(Vector2 footprint)
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // 플레이스홀더는 1x1 유닛이므로 스케일로 늘린다. 실제 아트를 쓸 때는 건드리지 않는다.
            if (completedSprite == null || spriteRenderer.sprite == placeholderSite)
                transform.localScale = new Vector3(footprint.x, footprint.y, 1f);
        }

        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            // 콜라이더는 로컬 크기이므로 스케일을 되돌려 적용한다.
            Vector3 s = transform.localScale;
            box.size = new Vector2(
                footprint.x / Mathf.Max(0.0001f, s.x),
                footprint.y / Mathf.Max(0.0001f, s.y));
        }
    }
}
