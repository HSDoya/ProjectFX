using UnityEngine;

// 월드에 설치된 가구 본체. 설치 판정에 필요한 규칙은 ItemData/CSV가 아니라
// 이 컴포넌트(= 프리팹)가 들고 있다. 아이템 쪽에는 프리팹 참조 하나만 두고,
// 크기나 통행 차단 여부 같은 설치 전용 정보는 여기서만 관리한다.
public class PlaceableFurniture : MonoBehaviour
{
    [Header("설치 판정 설정")]
    [Tooltip("설치에 필요한 공간(월드 유닛). 이 범위 안에 다른 충돌체가 있으면 설치할 수 없다.")]
    public Vector2 footprintSize = new Vector2(2f, 2f);

    // 회수했을 때 인벤토리로 돌려줄 아이템. 설치하는 쪽에서 런타임에 채워 넣는다.
    // 프리팹에 미리 저장해두면 ItemData.placedPrefab과 서로를 가리키는 이중 관리가 되므로
    // 직렬화하지 않는다.
    [System.NonSerialized] public ItemData sourceItem;
}
