using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data Single")]
public class EnemyDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string itemID;
    public string enemyName;
    public int maxHealth;

    [Header("성향 및 AI 범위")]
    public bool isAggressive;     // TRUE = 선공 몹, FALSE = 비선공 동물(소, 닭 등)
    public float detectRange;     // 선공 몹의 추격 범위 (비선공이면 0 또는 무시)
    public float attackRange;     // 공격 사거리

    [Header("이동 및 전투 스탯")]
    public float moveSpeed;
    public float attackDamage;
    public float attackCooldown;
    public string animType;       // BlendTree 또는 SimpleAnimation
}