using UnityEngine;
using System.Collections.Generic;

//이후 동물 DB추가시 동물 아이템 드랍도 여기에 추가할 예정

[System.Serializable]
public class DropRule
{
    public string itemID;          // 아이템 ID
    public float dropChance = 100f; // 드랍 확률
    public int minDrop = 1;        // 최소 드랍 개수
    public int maxDrop = 1;        // 최대 드랍 개수
}

public class ItemDropper : MonoBehaviour
{
    [Header("Drop Settings")]
    public GameObject fieldItemPrefab; // FieldItem 스크립트가 붙은 프리팹
    public List<DropRule> dropRules = new List<DropRule>();

    // 외부(적, 동물, 상자 등)에서 파괴될 때 이 함수만 호출하면 됨
    public void DropItems()
    {
        if (fieldItemPrefab == null || ItemDataManager.instance == null || dropRules.Count == 0) return;

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
                        Debug.Log($"[{gameObject.name}] 드랍 성공: {data.displayName} {count}개");
                    }
                }
                else
                {
                    Debug.LogError($"[ItemDropper] ItemDB에서 '{rule.itemID}'를 찾을 수 없습니다.");
                }
            }
        }
    }
}