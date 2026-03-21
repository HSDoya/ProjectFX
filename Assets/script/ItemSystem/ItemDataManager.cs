
using UnityEngine;

public class ItemDataManager : MonoBehaviour
{
    public static ItemDataManager instance;

    // 인스펙터에서 ItemDatabaseSO 에셋을 연결해줍니다.
    [SerializeField] private ItemDatabaseSO database;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 게임 시작 시 딕셔너리 세팅
        if (database != null) database.Initialize();
    }

    public ItemData GetItemDataByID(string itemID)
    {
        return database != null ? database.GetItemByID(itemID) : null;
    }
}