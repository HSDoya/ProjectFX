
using UnityEngine;

public class ItemDataManager : MonoBehaviour
{
    public static ItemDataManager instance;

    // 인스펙터에서 ItemDatabaseSO 에셋을 연결해줍니다.
    [SerializeField] private ItemDatabaseSO database;

    void Awake()
    {
        // 중복 인스턴스는 여기서 끝낸다. 예전엔 Destroy 후에도 아래 Initialize까지 실행됐다.
        if (instance != null)
        {
            Debug.LogWarning("More than one ItemDataManager found!");
            Destroy(gameObject);
            return;
        }
        instance = this;

        // 게임 시작 시 딕셔너리 세팅
        if (database != null) database.Initialize();
    }

    public ItemData GetItemDataByID(string itemID)
    {
        return database != null ? database.GetItemByID(itemID) : null;
    }
}