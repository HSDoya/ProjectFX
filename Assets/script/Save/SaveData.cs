using System;

// 세이브 파일의 데이터 구조.
//
// JsonUtility로 직렬화하므로 지켜야 하는 제약이 있다.
//   - Dictionary는 직렬화되지 않는다. 배열로 펴서 담는다.
//   - ScriptableObject 참조(ItemData, CropData, SkillData)는 저장할 수 없다.
//     ID 문자열로 바꿔 저장하고, 불러올 때 ItemDataManager 등으로 되살린다.
//   - 배열 안의 null은 빈 객체로 읽혀 나온다. 그래서 "빈 칸"은 null이 아니라
//     itemID가 빈 문자열인 것으로 표현한다. 읽는 쪽은 IsEmpty만 보면 된다.
//
// 이 파일에는 로직을 두지 않는다. 담는 모양만 정의하고, 채우고 되돌리는 일은 SaveManager가 한다.

[Serializable]
public class SaveData
{
    // 항목이 늘어나도 구버전 파일을 읽을 수 있게 처음부터 버전을 넣어둔다.
    // 나중에 붙이려고 하면 이미 버전 없는 파일이 돌아다니는 상태라 늦는다.
    // 버전 1: 플레이어 / 스탯 / 인벤토리
    // 버전 2: + 진행도(스킬 해금, 지형 시드, 구역 개방)
    // 버전 3: + 월드 생성(시드가 world로 이동, 생성 설정, 생성기 버전)
    public int version = SaveManager.CurrentVersion;

    // 표시용. 세이브 슬롯 목록에 "언제 저장했는지"를 보여줄 때 쓴다. (ISO 8601, UTC)
    public string savedAtUtc = string.Empty;

    public PlayerSave player = new PlayerSave();
    public StatsSave stats = new StatsSave();
    public InventorySave inventory = new InventorySave();
    public ProgressSave progress = new ProgressSave();
    public WorldGenSave world = new WorldGenSave();
}

/// <summary>
/// 세계를 결정하는 값 전부. 시드 하나로는 부족하다.
///
/// 지형은 시드 + 생성 파라미터의 조합으로 결정된다. 시드만 저장하면 인스펙터에서
/// noiseScale을 0.1에서 0.09로 바꾸는 순간 옛 세이브의 지형이 말없이 달라진다.
/// 그래서 파라미터도 같이 담고, 불러올 때 세이브 값으로 되돌린다.
///
/// genVersion은 세이브 포맷 버전(SaveData.version)과 다른 것이다. 포맷은 그대로인데
/// 생성 규칙만 바뀌는 경우가 더 흔하다. 그때는 같은 시드라도 다른 세계가 나온다.
/// </summary>
[Serializable]
public class WorldGenSave
{
    // 생성 규칙 버전. 알고리즘을 바꾸면 올린다. 0이면 기록이 없는 구버전 세이브다.
    public int genVersion;

    public bool hasSeed;
    public int seed;

    // false면 설정이 담기지 않은 세이브다(구버전에서 올라온 경우). 그때 설정을 적용하면
    // chunkSize 0 같은 값이 들어가 세계가 아예 만들어지지 않는다.
    public bool hasSettings;

    // ── 지형 (MapGenerator)
    public int chunkSize;
    public float noiseScale;
    public float waterThreshold;
    public float startIslandRadius;
    public float unlockedIslandRadius;
    public float shapeWobbleAmount;
    public float islandEdgePenalty;

    // ── 배치 (ObjectSpawner)
    public float spawnCheckRadius;
    public float minDistanceBetweenObjects;
    public int maxPositionSearchTries;
    public int[] ruleSpawnCounts = new int[0];

    // 프리팹 목록과 순서는 에셋 참조라 세이브에 담아도 되돌릴 수 없다.
    // 대신 "바뀌었다"는 사실만 감지한다. 순서만 바뀌어도 다른 세계가 된다
    // (프리팹을 인덱스로 고르기 때문이다).
    public int rulesFingerprint;

    // ── 구조 (MapUnlockManager)
    public int maxGridRadius;
}

/// <summary>
/// 진행도. 스킬 해금, 지형 시드, 맵 구역 개방 상태.
///
/// 지형 타일은 저장하지 않는다. MapGenerator가 시드에서 결정적으로 만들어내므로
/// 정수 하나면 지형 전체가 복원된다.
/// </summary>
[Serializable]
public class ProgressSave
{
    // 해금된 스킬의 skillID 목록. 좌표가 아니라 ID로 담는 이유: 트리 배치를 바꿔도
    // 예전 세이브가 그대로 읽힌다.
    public string[] unlockedSkillIDs = new string[0];

    // ── 구버전(버전 2) 읽기 전용 필드.
    // 시드는 버전 3에서 WorldGenSave로 옮겨갔다. 이름을 바꾸면 JsonUtility가
    // 옛 파일의 값을 못 찾으므로 그대로 남겨둔다. 새 세이브는 여기에 쓰지 않고,
    // SaveManager.MigrateIfNeeded가 옛 파일을 읽을 때만 쓴다.
    public bool hasSeed;
    public int mapSeed;

    public ChunkSave[] chunks = new ChunkSave[0];
}

/// <summary>맵 구역 한 칸. state는 ChunkState를 int로 담은 것이다.</summary>
[Serializable]
public class ChunkSave
{
    public int x;
    public int y;
    public int state;
}

/// <summary>레벨·경험치·현재 체력/스태미너·위치. 최대치는 스탯에서 계산되므로 저장하지 않는다.</summary>
[Serializable]
public class PlayerSave
{
    public int level = 1;
    public float currentExp;
    public float currentHealth;
    public float currentStamina;

    // Vector3는 JsonUtility가 다루긴 하지만, 나중에 손으로 파일을 열어볼 때
    // 축이 분명한 편이 낫다.
    public float posX;
    public float posY;
    public float posZ;
}

/// <summary>
/// 찍은 스탯과 남은 포인트.
///
/// skillBonus는 저장하지 않는다. 해금된 스킬 목록에서 다시 계산되는 값이라
/// 저장해두면 두 벌이 되어 서로 어긋날 수 있다. (Phase 3에서 스킬 해금 목록을 저장한다.)
/// </summary>
[Serializable]
public class StatsSave
{
    public int[] allocated = new int[0];
    public int availableStatPoints;
    public int availableSkillPoints;
}

/// <summary>칸 하나. itemID가 비어 있으면 빈 칸이다.</summary>
[Serializable]
public class ItemSave
{
    public string itemID = string.Empty;
    public int quantity;

    public bool IsEmpty => string.IsNullOrEmpty(itemID) || quantity <= 0;

    public static ItemSave Empty => new ItemSave();
}

/// <summary>장착 슬롯 하나. slotType은 EquipmentSlotType을 int로 담은 것이다.</summary>
[Serializable]
public class EquipSave
{
    public int slotType;
    public string itemID = string.Empty;
    public int quantity;

    public bool IsEmpty => string.IsNullOrEmpty(itemID) || quantity <= 0;
}

/// <summary>인벤토리 70칸 + 퀵슬롯 14칸 + 장착 슬롯.</summary>
[Serializable]
public class InventorySave
{
    public ItemSave[] items = new ItemSave[0];
    public ItemSave[] quickSlots = new ItemSave[0];
    public EquipSave[] equipment = new EquipSave[0];
}
