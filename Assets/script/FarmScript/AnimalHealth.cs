using UnityEngine;
using System.Collections.Generic;
using System.Collections; // 코루틴을 위해 추가

public class AnimalHealth : MonoBehaviour
{
    // 테스트를 위해 체력을 넉넉하게(예: 50) 늘려주시면 좋습니다.
    public int hp = 20;

    [Header("공용 필드 아이템 프리팹")]
    public GameObject fieldItemPrefab;

    [System.Serializable]
    public class DropRule
    {
        public string itemID;
        public int minDrop = 1;
        public int maxDrop = 2;
        [Range(0f, 100f)]
        public float dropChance = 100f;
    }

    [Header("드랍 아이템 설정")]
    public List<DropRule> dropRules = new List<DropRule>();

    private bool isDead = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // ★ 추가: 데미지를 입는 메서드
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        hp -= damage;
        Debug.Log($"{gameObject.name} 피격! 데미지: {damage}, 남은 HP: {hp}");

        // 피격 연출 (빨갛게 깜빡임)
        if (spriteRenderer != null)
        {
            StartCoroutine(HitEffectCoroutine());
        }

        // 체력이 0 이하가 되면 사망 처리
        if (hp <= 0)
        {
            Kill();
        }
    }

    // 타격감을 위한 간단한 깜빡임 효과
    private IEnumerator HitEffectCoroutine()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    public void Kill()
    {
        if (isDead) return;
        isDead = true;

        DropItems();
        Destroy(gameObject);
    }

    void DropItems()
    {
        // 1. 프리팹이 안 들어가 있는지 체크
        if (fieldItemPrefab == null)
        {
            Debug.LogError("[드랍 실패] fieldItemPrefab이 비어있습니다! 동물 프리팹의 인스펙터에 FieldItem 프리팹을 넣어주세요.");
            return;
        }

        // 2. 매니저가 없는지 체크
        if (ItemDataManager.instance == null)
        {
            Debug.LogError("[드랍 실패] 씬에 ItemDataManager가 없습니다.");
            return;
        }

        // 3. 드랍 아이템 목록이 비어있는지 체크
        if (dropRules.Count == 0)
        {
            Debug.LogWarning("[드랍 경고] 동물 인스펙터의 Drop Rules(드랍 목록)가 비어있습니다. 설정해 주세요!");
            return;
        }

        foreach (var rule in dropRules)
        {
            if (Random.Range(0f, 100f) <= rule.dropChance)
            {
                int count = Random.Range(rule.minDrop, rule.maxDrop + 1);
                if (count <= 0) continue;

                // 4. DB에서 아이템 찾기
                ItemData data = ItemDataManager.instance.GetItemDataByID(rule.itemID);
                if (data != null)
                {
                    Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
                    GameObject droppedObj = Instantiate(fieldItemPrefab, dropPos, Quaternion.identity);

                    FieldItem fieldItem = droppedObj.GetComponent<FieldItem>();
                    if (fieldItem != null)
                    {
                        fieldItem.Setup(data, count);
                        Debug.Log($"아이템 드랍 성공: {data.displayName} {count}개");
                    }
                    else
                    {
                        Debug.LogError(" [드랍 실패] 스폰된 fieldItemPrefab에 FieldItem 스크립트가 안 붙어있습니다!");
                    }
                }
                else
                {
                    // 5. 스펠링 대소문자가 틀렸을 경우
                    Debug.LogError($"[드랍 실패] ItemDB에서 '{rule.itemID}'(을)를 찾을 수 없습니다. 대소문자나 띄어쓰기를 확인하세요!");
                }
            }
        }
    }
}