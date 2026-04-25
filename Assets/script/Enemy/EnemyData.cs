using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName;      // 이름
    public int maxHealth;         // 최대 체력
    public float detectRange;     // 플레이어 감지 범위
    public float attackRange;     // 공격 사거리

    [Header("이동 설정")]
    public float moveSpeed;       // 이동 속도
    public float rotationSpeed;   // 회전 속도 (필요 시)

    [Header("공격 설정")]
    public float attackDamage;    // 공격력
    public float attackCooldown;  // 공격 쿨타임
}