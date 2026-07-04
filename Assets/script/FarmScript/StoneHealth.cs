using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class StoneHealth : MonoBehaviour
{
    public int hp = 20; // 돌 체력 (곡괭이 데미지에 맞춰 유니티 인스펙터에서 조절하세요)

    [Header("드랍 설정")]
    public GameObject fieldItemPrefab;

    [System.Serializable]
    public class DropRule
    {
        public string itemID;
        public int minDrop = 1;
        public int maxDrop = 3;
        [Range(0f, 100f)]
        public float dropChance = 100f;
    }

    public List<DropRule> dropRules = new List<DropRule>();

    private bool isDestroyed = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        hp -= damage;
        Debug.Log($"돌 타격! 데미지: {damage}, 남은 HP: {hp}");

        if (spriteRenderer != null)
        {
            StartCoroutine(HitEffectCoroutine());
        }

        if (hp <= 0)
        {
            BreakStone();
        }
    }

    private IEnumerator HitEffectCoroutine()
    {
        Color originalColor = spriteRenderer.color;
        // 돌은 맞았을 때 나무와 다르게 밝은 회색/흰색 느낌으로 깜빡이게 연출
        spriteRenderer.color = new Color(0.8f, 0.8f, 0.8f);
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    private void BreakStone()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        DropItems();
        Destroy(gameObject); // 돌은 파괴되면 완전히 사라집니다.
    }

    private void DropItems()
    {
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
                    // 돌 주변으로 아이템 흩뿌리기
                    Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.8f;
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