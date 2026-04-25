using UnityEngine;

public class UnlockSign : MonoBehaviour
{
    private MapUnlockManager manager;
    private Vector2Int chunkCoord;
    private int cost;

    // 매니저가 표지판을 소환할 때 정보를 주입해주는 함수
    public void Setup(MapUnlockManager mgr, Vector2Int coord, int unlockCost)
    {
        manager = mgr;
        chunkCoord = coord;
        cost = unlockCost;
    }

    // 임시 테스트용: 표지판을 마우스로 클릭하면 해금되도록 설정
    void OnMouseDown()
    {
        // 실제 게임에서는 "돈이 충분한지" 검사하는 로직이 필요합니다.
        Debug.Log($"{chunkCoord} 구역 해금 시도! (비용: {cost})");

        // 매니저에게 해금 요청
        manager.UnlockChunk(chunkCoord);

        // 땅이 열렸으니 표지판 파괴
        Destroy(gameObject);
    }
}
