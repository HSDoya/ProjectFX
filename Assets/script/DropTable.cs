using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 죽거나 파괴될 때 뱉을 아이템 한 줄. 몹/나무/돌이 공용으로 쓴다.
/// </summary>
/// 예전엔 EnemyBaseAI, TreeHealth, StoneHealth가 각자 똑같은 중첩 클래스를 들고 있어서,
/// 드랍 규칙을 바꾸려면 세 곳을 고쳐야 했다. 필드 이름(itemID/minDrop/maxDrop/dropChance)은
/// 그대로 유지했으므로 프리팹에 이미 저장된 드랍 데이터는 그대로 읽힌다.
[System.Serializable]
public class DropRule
{
    public string itemID;
    public int minDrop = 1;
    public int maxDrop = 2;
    [Range(0f, 100f)]
    public float dropChance = 100f;
}

/// <summary>
/// 드랍 규칙을 실제 월드 아이템으로 바꿔 뿌리는 공용 로직.
/// </summary>
public static class DropTable
{
    /// <param name="spread">아이템이 흩뿌려지는 반경 (몹은 좁게, 나무/돌은 넓게)</param>
    public static void SpawnDrops(List<DropRule> rules, GameObject fieldItemPrefab, Vector3 origin,
        float spread, string ownerName)
    {
        if (fieldItemPrefab == null)
        {
            Debug.LogError($"[드랍 실패] {ownerName}의 fieldItemPrefab이 비어있습니다! 인스펙터에 FieldItem 프리팹을 넣어주세요.");
            return;
        }

        if (ItemDataManager.instance == null)
        {
            Debug.LogError("[드랍 실패] 씬에 ItemDataManager 싱글턴 인스턴스가 없습니다.");
            return;
        }

        if (rules == null || rules.Count == 0) return;

        foreach (var rule in rules)
        {
            if (rule == null || string.IsNullOrEmpty(rule.itemID)) continue;
            if (Random.Range(0f, 100f) > rule.dropChance) continue;

            int count = Random.Range(rule.minDrop, rule.maxDrop + 1);
            if (count <= 0) continue;

            // CSV의 itemID에 공백이 섞여 있는 데이터가 있어서(예: 'CutRock ') 조회 전에 다듬는다.
            ItemData data = ItemDataManager.instance.GetItemDataByID(rule.itemID.Trim());
            if (data == null)
            {
                Debug.LogError($"[드랍 실패] ItemDB에서 '{rule.itemID}'를 찾을 수 없습니다. 대소문자나 오타를 확인하세요!");
                continue;
            }

            Vector3 dropPos = origin + (Vector3)Random.insideUnitCircle * spread;
            GameObject droppedObj = Object.Instantiate(fieldItemPrefab, dropPos, Quaternion.identity);

            FieldItem fieldItem = droppedObj.GetComponent<FieldItem>();
            if (fieldItem == null)
            {
                Debug.LogError("[드랍 실패] 스폰된 프리팹에 FieldItem 스크립트 컴포넌트가 누락되었습니다!");
                continue;
            }

            fieldItem.Setup(data, count);
        }
    }
}
