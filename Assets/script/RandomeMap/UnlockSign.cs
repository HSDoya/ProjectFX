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
        GameLog.Log($"{chunkCoord} 구역 해금 시도! (비용: {cost})");

        // 매니저에게 해금 요청. 표지판 파괴도 매니저가 맡는다.
        // (여기서 스스로 파괴하면, 세이브를 불러와 코드로 개방할 때는 클릭이 없으니
        //  이미 열린 땅 위에 표지판이 남는다.)
        manager.UnlockChunk(chunkCoord);
    }
}
