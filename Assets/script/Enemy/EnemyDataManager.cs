using UnityEngine;
using System.Collections.Generic;

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
    public string animType; // 애니메이션 타입도 CSV에서 결정하도록 확장
}

public class EnemyDataManager : MonoBehaviour
{
    public static EnemyDataManager Instance;
    private Dictionary<string, EnemyRawData> enemyTable = new Dictionary<string, EnemyRawData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadEnemyCSV();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadEnemyCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("EnemyDatabase");
        if (csvFile == null)
        {
            Debug.LogError("EnemyDatabase.csv 파일을 Assets/Resources/ 폴더에서 찾을 수 없습니다!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');

        // i = 1부터 시작 (0번째 헤더 줄 건너뛰기)
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] data = lines[i].Split(',');

            try
            {
                EnemyRawData rawData = new EnemyRawData
                {
                    itemID = data[0].Trim(),
                    enemyName = data[1].Trim(),
                    maxHealth = int.Parse(data[2].Trim()),
                    detectRange = float.Parse(data[3].Trim()),
                    attackRange = float.Parse(data[4].Trim()),
                    moveSpeed = float.Parse(data[5].Trim()),
                    attackDamage = float.Parse(data[6].Trim()),
                    attackCooldown = float.Parse(data[7].Trim()),
                    animType = data[8].Trim()
                };

                if (!enemyTable.ContainsKey(rawData.itemID))
                {
                    enemyTable.Add(rawData.itemID, rawData);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{i}번째 줄 데이터 파싱 중 에러 발생: {e.Message}");
            }
        }
        Debug.Log($"[EnemyDataManager] 총 {enemyTable.Count}종의 적 데이터 로드 완료.");
    }

    public EnemyRawData GetEnemyData(string id)
    {
        if (enemyTable.TryGetValue(id, out EnemyRawData data))
        {
            return data;
        }
        return null;
    }
}