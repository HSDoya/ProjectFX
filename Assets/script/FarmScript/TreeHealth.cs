using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TreeHealth : MonoBehaviour
{
    public int hp = 20; // 나무 체력

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
    private Color baseColor; // 항상 여기로 복구한다 - 연타 중 색이 바뀐 채로 캡처되는 걸 방지
    private Coroutine hitEffectCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        hp -= damage;
        Debug.Log($"나무 타격! 데미지: {damage}, 남은 HP: {hp}");

        if (spriteRenderer != null)
        {
            // 연타로 코루틴이 겹치면 색이 안 돌아오는 문제가 있어서, 새로 시작하기 전에 이전 걸 끊는다.
            if (hitEffectCoroutine != null) StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = StartCoroutine(HitEffectCoroutine());
        }

        if (hp <= 0)
        {
            ChopDown();
        }
    }

    private IEnumerator HitEffectCoroutine()
    {
        // 나무는 맞았을 때 살짝 어두운 빨간색/갈색 느낌으로 깜빡이게 연출
        spriteRenderer.color = new Color(1f, 0.6f, 0.6f);
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = baseColor;
        hitEffectCoroutine = null;
    }

    private void ChopDown()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        DropItems();
        Destroy(gameObject); // 나중에 여기를 '밑동(Stump)으로 이미지 변경' 등으로 업그레이드할 수 있습니다.
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
                    // 나무 주변으로 아이템 흩뿌리기
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