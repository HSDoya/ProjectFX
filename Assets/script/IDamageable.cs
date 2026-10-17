// 플레이어 무기에 맞을 수 있는 모든 대상(나무/돌/몹)의 공통 창구.
//
// 예전엔 PlayerMove가 GetComponent<TreeHealth>() -> <StoneHealth>() -> <EnemyBaseAI>()를
// 줄줄이 시도하는 분기 덩어리를 들고 있었다. 대상이 늘어날 때마다 그 분기가 길어지고,
// "이 대상엔 어떤 도구가 필요한가"라는 규칙이 때리는 쪽에 흩어져 있었다.
// 이제 필요한 도구는 대상이 직접 알려주고, PlayerMove는 찾아서 때리기만 한다.
public interface IDamageable
{
    /// <summary>
    /// 이 대상을 때리려면 필요한 아이템 DB(CSV)의 'type' 값. (예: 나무는 "Axe", 돌은 "Pick")
    /// 비어 있으면 아무 무기로나 때릴 수 있다.
    /// </summary>
    string RequiredToolType { get; }

    void TakeDamage(int damage);
}
