using UnityEngine;
using System.Collections.Generic;

public class AnimalHealth : MonoBehaviour
{
    public int hp = 1;

    [Header("공용 필드 아이템 프리팹")]
    // FieldItem.cs가 붙어있는 빈 프리팹을 연결해줍니다.
    public GameObject fieldItemPrefab;

    // 인스펙터에서 드랍할 아이템 목록을 자유롭게 설정할 수 있는 클래스
    [System.Serializable]
    public class DropRule
    {
        public string itemID;      // ItemDB에 있는 아이템 ID (예: "meat")
        public int minDrop = 1;    // 최소 드랍 개수
        public int maxDrop = 2;    // 최대 드랍 개수
        [Range(0f, 100f)]
        public float dropChance = 100f; // 드랍 확률 (0~100%)
    }

    [Header("드랍 아이템 설정")]
    public List<DropRule> dropRules = new List<DropRule>();

    private bool isDead = false;

    public void Kill()
    {
        if (isDead) return;
        isDead = true;

        DropItems();
        Destroy(gameObject);
    }

    void DropItems()
    {
        if (fieldItemPrefab == null || ItemDataManager.instance == null) return;

        foreach (var rule in dropRules)
        {
            // 1. 확률 체크
            if (Random.Range(0f, 100f) <= rule.dropChance)
            {
                // 2. 개수 결정
                int count = Random.Range(rule.minDrop, rule.maxDrop + 1);
                if (count <= 0) continue;

                // 3. DB에서 아이템 정보 가져오기
                ItemData data = ItemDataManager.instance.GetItemDataByID(rule.itemID);
                if (data != null)
                {
                    // 4. 아이템 스폰 및 흩뿌리기
                    Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
                    GameObject droppedObj = Instantiate(fieldItemPrefab, dropPos, Quaternion.identity);

                    // FieldItem 스크립트에 데이터 주입
                    FieldItem fieldItem = droppedObj.GetComponent<FieldItem>();
                    if (fieldItem != null)
                    {
                        fieldItem.Setup(data, count);
                    }
                }
                else
                {
                    Debug.LogWarning($"드랍 실패: DB에서 '{rule.itemID}' 아이템을 찾을 수 없습니다.");
                }
            }
        }
    }
}