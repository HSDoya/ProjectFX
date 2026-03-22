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
        // (이전 단계에서 작성한 아이템 드랍 로직 그대로 유지)
        if (fieldItemPrefab == null || ItemDataManager.instance == null) return;

        foreach (var rule in dropRules)
        {
            if (Random.Range(0f, 100f) <= rule.dropChance)
            {
                int count = Random.Range(rule.minDrop, rule.maxDrop + 1);
                if (count <= 0) continue;

                ItemData data = ItemDataManager.instance.GetItemDataByID(rule.itemID);
                if (data != null)
                {
                    Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
                    GameObject droppedObj = Instantiate(fieldItemPrefab, dropPos, Quaternion.identity);

                    FieldItem fieldItem = droppedObj.GetComponent<FieldItem>();
                    if (fieldItem != null)
                    {
                        fieldItem.Setup(data, count);
                    }
                }
            }
        }
    }
}