using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Enemy/Enemy Database")]
public class EnemyDatabaseSO : ScriptableObject
{
    // 베이커가 자동으로 채워줄 전체 데이터 리스트
    public List<EnemyDataSO> allEnemies = new List<EnemyDataSO>();

    // 런타임 빠른 검색용 딕셔너리
    private Dictionary<string, EnemyDataSO> enemyDict = new Dictionary<string, EnemyDataSO>();

    public void Initialize()
    {
        enemyDict.Clear();
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && !enemyDict.ContainsKey(enemy.itemID))
            {
                enemyDict.Add(enemy.itemID, enemy);
            }
        }
    }

    public EnemyDataSO GetEnemyByID(string id)
    {
        if (enemyDict.Count == 0) Initialize(); // 초기화 보장
        enemyDict.TryGetValue(id, out var enemy);
        return enemy;
    }
}