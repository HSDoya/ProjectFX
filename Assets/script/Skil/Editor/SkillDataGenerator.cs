using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// 스킬 DB가 완성되기 전까지 쓰는 테스트용 데이터 생성기.
// 여기 하드코딩된 목록이 나중에 CSV 기반(ItemDataBaker 패턴)으로 바뀔 자리다.
public class SkillDataGenerator : EditorWindow
{
    private class SkillDef
    {
        public string id;
        public string name;
        public string description;
        public SkillCategory category;
        public int x;
        public int y;
        public bool unlockedByDefault;

        public SkillDef(string id, string name, string description, SkillCategory category, int x, int y, bool unlockedByDefault = false)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.category = category;
            this.x = x;
            this.y = y;
            this.unlockedByDefault = unlockedByDefault;
        }
    }

    [MenuItem("Tools/스킬 테스트 데이터 생성(20개)")]
    public static void GenerateTestSkills()
    {
        const string parentFolder = "Assets/character/asset/skill";
        const string folderPath = parentFolder + "/SkillDataAssets";

        if (!AssetDatabase.IsValidFolder(parentFolder))
        {
            Debug.LogError($"[SkillDataGenerator] {parentFolder} 폴더를 찾을 수 없습니다.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, "SkillDataAssets");
        }

        // 마름모(등각) 배치 대신 정사각형 좌표 그대로 사용. 4개 파트를 2x2로 붙여서
        // 합쳤을 때 하나의 사각형(ㅁ) 블록이 되도록 4x6 영역에 조밀하게 채움(바깥 모서리 4칸만 비움).
        // 각 파트에서 중앙(전체 도형 중심)에 가장 가까운 칸이 시작점(unlockedByDefault=true).
        // 1사분면(우상단)=전투, 2사분면(좌상단)=경험치, 3사분면(좌하단)=농사, 4사분면(우하단)=발전
        //
        //   . X X .
        //   X X X X
        //   X X X X
        //   X X X X
        //   X X X X
        //   . X X .
        var defs = new List<SkillDef>
        {
            // 경험치 - 2사분면(좌상단)
            new SkillDef("Exp_01", "경험치 습득 증가 I",    "테스트용 경험치 스킬",       SkillCategory.Experience, 1, 0),
            new SkillDef("Exp_02", "경험치 습득 증가 II",   "테스트용 경험치 스킬",       SkillCategory.Experience, 0, 1),
            new SkillDef("Exp_03", "경험치 습득 증가 III",  "테스트용 경험치 스킬",       SkillCategory.Experience, 1, 1),
            new SkillDef("Exp_04", "경험치 습득 증가 IV",   "테스트용 경험치 스킬",       SkillCategory.Experience, 0, 2),
            new SkillDef("Exp_05", "경험치 마스터리",       "테스트용 경험치 시작 스킬",   SkillCategory.Experience, 1, 2, unlockedByDefault: true),

            // 전투 - 1사분면(우상단)
            new SkillDef("Combat_01", "공격력 증가 I",      "테스트용 전투 스킬",         SkillCategory.Combat, 2, 0),
            new SkillDef("Combat_02", "공격력 증가 II",     "테스트용 전투 스킬",         SkillCategory.Combat, 2, 1),
            new SkillDef("Combat_03", "치명타 확률 증가",    "테스트용 전투 스킬",         SkillCategory.Combat, 3, 1),
            new SkillDef("Combat_04", "방어 관통력 증가",    "테스트용 전투 스킬",         SkillCategory.Combat, 3, 2),
            new SkillDef("Combat_05", "전투 마스터리",      "테스트용 전투 시작 스킬",     SkillCategory.Combat, 2, 2, unlockedByDefault: true),

            // 농사 - 3사분면(좌하단)
            new SkillDef("Farm_01", "작물 성장 속도 증가",   "테스트용 농사 스킬",         SkillCategory.Farming, 0, 3),
            new SkillDef("Farm_02", "작물 수확량 증가",      "테스트용 농사 스킬",         SkillCategory.Farming, 0, 4),
            new SkillDef("Farm_03", "농기구 내구도 증가",    "테스트용 농사 스킬",         SkillCategory.Farming, 1, 4),
            new SkillDef("Farm_04", "물뿌리개 범위 증가",    "테스트용 농사 스킬",         SkillCategory.Farming, 1, 5),
            new SkillDef("Farm_05", "농사 마스터리",        "테스트용 농사 시작 스킬",     SkillCategory.Farming, 1, 3, unlockedByDefault: true),

            // 발전 - 4사분면(우하단)
            new SkillDef("Dev_01", "제작 슬롯 확장 I",      "테스트용 발전 스킬",         SkillCategory.Development, 3, 3),
            new SkillDef("Dev_02", "제작 슬롯 확장 II",     "테스트용 발전 스킬",         SkillCategory.Development, 2, 4),
            new SkillDef("Dev_03", "장비 강화 해금",         "테스트용 발전 스킬",         SkillCategory.Development, 3, 4),
            new SkillDef("Dev_04", "테크트리 해금 I",       "테스트용 발전 스킬",         SkillCategory.Development, 2, 5),
            new SkillDef("Dev_05", "발전 마스터리",         "테스트용 발전 시작 스킬",     SkillCategory.Development, 2, 3, unlockedByDefault: true),
        };

        List<SkillData> baked = new List<SkillData>();
        HashSet<string> seenIds = new HashSet<string>();

        foreach (var def in defs)
        {
            if (!seenIds.Add(def.id))
            {
                Debug.LogWarning($"[SkillDataGenerator] 중복된 skillID '{def.id}'를 건너뜁니다.");
                continue;
            }

            string assetPath = $"{folderPath}/{def.id}.asset";
            SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.skillID = def.id;
            data.displayName = def.name;
            data.description = def.description;
            data.category = def.category;
            data.gridX = def.x;
            data.gridY = def.y;
            data.unlockedByDefault = def.unlockedByDefault;

            EditorUtility.SetDirty(data);
            baked.Add(data);
        }

        string dbPath = $"{parentFolder}/SkillDatabase.asset";
        SkillDatabaseSO database = AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>(dbPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<SkillDatabaseSO>();
            AssetDatabase.CreateAsset(database, dbPath);
        }

        database.allSkills = baked;
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>성공!</color> 테스트 스킬 {baked.Count}개가 {folderPath}에 생성/갱신되었습니다. " +
                  $"{dbPath}를 SkillTreeManager의 Skill Database 필드에 연결해주세요.");
    }
}
