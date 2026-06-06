#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class EnemyDatabaseBaker : EditorWindow
{
    [MenuItem("Tools/Bake Enemy Database")]
    public static void BakeDatabase()
    {
        // 1. CSV 파일 로드
        TextAsset csvFile = Resources.Load<TextAsset>("EnemyDatabase");
        if (csvFile == null)
        {
            Debug.LogError("Assets/Resources/EnemyDatabase.csv 파일을 찾을 수 없습니다! 위치를 확인하세요.");
            return;
        }

        // 2. 통합 DB 에셋 로드 또는 생성 경로 설정
        string dbFolder = "Assets/Resources/Databases";
        if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);

        string dbPath = $"{dbFolder}/EnemyDatabase.asset";
        EnemyDatabaseSO enemyDB = AssetDatabase.LoadAssetAtPath<EnemyDatabaseSO>(dbPath);

        if (enemyDB == null)
        {
            enemyDB = ScriptableObject.CreateInstance<EnemyDatabaseSO>();
            AssetDatabase.CreateAsset(enemyDB, dbPath);
        }

        enemyDB.allEnemies.Clear(); // 베이킹 시작 전 초기화

        // 3. 개별 적 데이터 에셋 저장 폴더 생성
        string enemyFolder = "Assets/Resources/Enemies";
        if (!Directory.Exists(enemyFolder)) Directory.CreateDirectory(enemyFolder);

        // 4. CSV 파싱 개시
        string[] lines = csvFile.text.Split('\n');

        // i = 1부터 시작 (0번째 헤더 줄 건너뜀)
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] data = lines[i].Split(',');

            try
            {
                string id = data[0].Trim();
                string name = data[1].Trim();

                // 기존 생성된 에셋이 있는지 검사, 없으면 인스턴스 새로 생성
                string assetPath = $"{enemyFolder}/{id}_{name}.asset";
                EnemyDataSO singleData = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(assetPath);

                if (singleData == null)
                {
                    singleData = ScriptableObject.CreateInstance<EnemyDataSO>();
                    AssetDatabase.CreateAsset(singleData, assetPath);
                }

                // CSV 칼럼 데이터 자동 바인딩
                singleData.itemID = id;
                singleData.enemyName = name;
                singleData.maxHealth = int.Parse(data[2].Trim());

                // bool.Parse를 사용하여 TRUE/FALSE 판별
                singleData.isAggressive = bool.Parse(data[3].Trim().ToUpper());

                singleData.detectRange = float.Parse(data[4].Trim());
                singleData.attackRange = float.Parse(data[5].Trim());
                singleData.moveSpeed = float.Parse(data[6].Trim());
                singleData.attackDamage = float.Parse(data[7].Trim());
                singleData.attackCooldown = float.Parse(data[8].Trim());
                singleData.animType = data[9].Trim();

                // 수정 사항 에디터 기록 및 메인 DB 리스트 등록
                EditorUtility.SetDirty(singleData);
                enemyDB.allEnemies.Add(singleData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Baker Error] {i}번째 줄 파싱 실패. CSV 열 순서나 데이터 타입을 확인하세요: {e.Message}");
            }
        }

        // 5. 에셋 저장 및 유니티 리프레시
        EditorUtility.SetDirty(enemyDB);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=cyan>[Enemy Database Baker]</color> 에셋 자동 추출 및 데이터베이스 베이킹이 완벽하게 완료되었습니다!");
    }
}
#endif