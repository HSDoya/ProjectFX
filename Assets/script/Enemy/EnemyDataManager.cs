using UnityEngine;
using System.Collections.Generic;

// 엑셀의 한 줄(행) 데이터를 담을 클래스
[System.Serializable]
public class EnemyRawData
{
    public string itemID;
    public string enemyName;
    public int maxHealth;
    public float detectRange;
    public float attackRange;
    public float moveSpeed;
    public float attackDamage;
    public float attackCooldown;
}

public class EnemyDataManager : MonoBehaviour
{
    public static EnemyDataManager Instance;

    // itemID를 키(Key)로 사용하여 빠르게 적 데이터를 찾을 수 있는 딕셔너리
    private Dictionary<string, EnemyRawData> enemyTable = new Dictionary<string, EnemyRawData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadEnemyCSV(); // 게임 시작 시 CSV 로드
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadEnemyCSV()
    {
        // Assets/Resources/EnemyDatabase.csv 파일을 읽어옴 (.csv 확장자는 생략)
        TextAsset csvFile = Resources.Load<TextAsset>("EnemyDatabase");
        if (csvFile == null)
        {
            Debug.LogError("EnemyDatabase.csv 파일을 찾을 수 없습니다! Resources 폴더를 확인하세요.");
            return;
        }

        // 엔터(\n) 기준으로 줄바꿈 파싱
        string[] lines = csvFile.text.Split('\n');

        // i = 1인 이유: 0번째 줄은 헤더(itemID, enemyName...)이므로 건너뜁니다.
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // 콤마(,) 기준으로 데이터 분리
            string[] data = lines[i].Split(',');

            EnemyRawData rawData = new EnemyRawData();
            rawData.itemID = data[0].Trim();
            rawData.enemyName = data[1].Trim();
            rawData.maxHealth = int.Parse(data[2].Trim());
            rawData.detectRange = float.Parse(data[3].Trim());
            rawData.attackRange = float.Parse(data[4].Trim());
            rawData.moveSpeed = float.Parse(data[5].Trim());
            rawData.attackDamage = float.Parse(data[6].Trim());
            rawData.attackCooldown = float.Parse(data[7].Trim());

            // 딕셔너리에 추가
            if (!enemyTable.ContainsKey(rawData.itemID))
            {
                enemyTable.Add(rawData.itemID, rawData);
                enemyTable[rawData.itemID] = rawData;
            }
        }
        Debug.Log($"[EnemyDataManager] 총 {enemyTable.Count}종의 적 데이터 로드 완료.");
    }

    // 외부(AI 스크립트)에서 몬스터 정보를 요청할 때 쓸 함수
    public EnemyRawData GetEnemyData(string id)
    {
        if (enemyTable.TryGetValue(id, out EnemyRawData data))
        {
            return data;
        }
        //Debug.LogError($"[EnemyDataManager] {id}에 해당하는 데이터가 없습니다!");
        return null;
    }
}