using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// 세이브 파일의 읽기/쓰기와, 각 시스템에서 값을 모으고 되돌리는 일을 담당한다.
///
/// 정적 클래스인 이유: 씬에 오브젝트를 하나 더 두면 그것도 중복 관리 대상이 된다.
/// 필요한 대상은 전부 기존 싱글톤(Inventory.instance 등)으로 찾을 수 있으므로 씬 배선이 없다.
///
/// 저장 순서는 중요하지 않지만 <b>불러오는 순서는 중요하다</b>. ApplyTo 주석 참고.
/// </summary>
public static class SaveManager
{
    public const int CurrentVersion = 3;

    private const string FilePrefix = "save_";
    private const string FileExt = ".json";

    /// <summary>슬롯 번호에 해당하는 파일 경로. 지금은 0번만 쓰지만 번호는 열어둔다.</summary>
    public static string PathFor(int slot = 0)
        => Path.Combine(Application.persistentDataPath, $"{FilePrefix}{slot}{FileExt}");

    public static bool Exists(int slot = 0) => File.Exists(PathFor(slot));

    /// <summary>쓸 수 있는 슬롯 수. 저장/불러오기 UI가 이 수만큼 줄을 만든다.</summary>
    public const int SlotCount = 3;

    private const string LastSlotKey = "save.lastSlot";

    /// <summary>
    /// 마지막으로 저장하거나 불러온 슬롯.
    ///
    /// MapUnlockManager가 시작할 때 "어느 슬롯의 시드로 세계를 만들지" 정하는 데 쓴다.
    /// 이게 없으면 항상 0번만 보기 때문에, 1번 슬롯을 불러올 때마다 지형을 전부 부수고
    /// 다시 만들게 된다.
    ///
    /// 게임 진행이 아니라 "어디까지 보던 중이었나"에 해당하므로 세이브 파일이 아니라
    /// PlayerPrefs에 둔다. 세이브를 지워도 남아 있어야 하는 값이다.
    /// </summary>
    public static int LastUsedSlot
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(LastSlotKey, 0), 0, SlotCount - 1);
        private set
        {
            PlayerPrefs.SetInt(LastSlotKey, Mathf.Clamp(value, 0, SlotCount - 1));
            PlayerPrefs.Save();
        }
    }

    /// <summary>슬롯 목록에 보여줄 요약. 불러오지 않고 파일만 들여다본 결과다.</summary>
    public struct SaveSlotInfo
    {
        public bool exists;
        public bool broken;   // 파일은 있는데 읽을 수 없다
        public int version;
        public bool hasTime;
        public DateTime savedAtLocal;
        public int level;
    }

    /// <summary>
    /// 슬롯 하나의 요약을 읽는다. 아무것도 적용하지 않으므로 목록을 그리는 중에 불러도 안전하다.
    /// </summary>
    public static SaveSlotInfo PeekInfo(int slot)
    {
        var info = new SaveSlotInfo();
        if (!Exists(slot)) return info;

        info.exists = true;

        SaveData data = ReadFile(slot);
        if (data == null)
        {
            // 파일이 있는데 못 읽는 상태. 목록에서 "Damaged file"로 보여주고 불러오기를 막는다.
            info.broken = true;
            return info;
        }

        info.version = data.version;
        info.level = data.player != null ? data.player.level : 1;

        if (!string.IsNullOrEmpty(data.savedAtUtc) &&
            DateTime.TryParse(data.savedAtUtc, CultureInfo.InvariantCulture,
                              DateTimeStyles.RoundtripKind, out DateTime parsed))
        {
            info.hasTime = true;
            info.savedAtLocal = parsed.ToLocalTime();
        }

        return info;
    }

    /// <summary>
    /// 파일을 읽어 SaveData로 바꾼다. 없거나 깨졌으면 null. 아무것도 적용하지 않는다.
    /// 읽는 입구가 네 곳(Load, PeekWorld, PeekInfo, CarryOverMissing)이라 한 곳으로 모았다.
    /// </summary>
    private static SaveData ReadFile(int slot)
    {
        string path = PathFor(slot);
        if (!File.Exists(path)) return null;

        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            GameLog.LogWarning($"[Save] 파일을 읽을 수 없습니다 (슬롯 {slot}): {e.Message}");
            return null;
        }
    }

    // ───────────────────── 월드 생성 (시드 + 생성 설정) ─────────────────────

    /// <summary>
    /// 세이브 파일에서 월드 생성 정보만 읽어본다. 아무것도 적용하지 않는다.
    ///
    /// MapUnlockManager가 세계를 만들기 <b>전에</b> 호출한다. 처음부터 맞는 시드와
    /// 설정으로 만들면 나중에 불러올 때 지형이 이미 같으므로, 부수고 다시 만드는 일이
    /// 생기지 않는다. (이 함수가 없으면 매 실행마다 무작위 시드로 한 번 만든 뒤 전부 버린다.)
    ///
    /// 실패하면 false를 돌려주고 게임은 무작위 시드로 그냥 시작한다. 시드를 못 읽었다고
    /// 게임이 안 열리는 것이 더 나쁘다.
    /// </summary>
    public static bool PeekWorld(int slot, out WorldGenSave world)
    {
        world = null;

        SaveData data = ReadFile(slot);
        if (data == null) return false;

        // Load가 거부할 파일이면 시드도 받아들이지 않는다. 읽지 못할 세이브의
        // 지형만 반쯤 적용된 상태가 가장 설명하기 어렵다.
        if (data.version > CurrentVersion) return false;

        MigrateIfNeeded(data);

        if (data.world == null || !data.world.hasSeed) return false;

        world = data.world;
        return true;
    }

    /// <summary>
    /// 세이브의 시드와 생성 설정을 씬에 되돌린다.
    ///
    /// <b>세이브가 권위를 가진다</b> — 저장한 세계는 저장한 모습으로 열려야 한다.
    /// 생성 파라미터를 조정하는 중이라면 MapGenerator의 ignoreSavedWorldSettings를 켠다.
    /// 그때도 시드는 적용된다. 시드까지 무시하면 아예 다른 세계가 되기 때문이다.
    /// </summary>
    /// <returns>지형을 다시 만들어야 하는지. 세계를 결정하는 값이 하나라도 달라졌으면 true.</returns>
    public static bool ApplyWorld(WorldGenSave world)
    {
        if (world == null) return false;

        var mapGen = UnityEngine.Object.FindFirstObjectByType<MapGenerator>();
        if (mapGen == null) return false;

        if (world.genVersion != 0 && world.genVersion != MapGenerator.GenVersion)
        {
            GameLog.LogWarning(
                $"[Save] 생성 규칙 버전이 다릅니다 (세이브 {world.genVersion} / 현재 {MapGenerator.GenVersion}). " +
                "같은 시드여도 지형이나 오브젝트 배치가 저장 당시와 다를 수 있습니다.");
        }

        var changes = new List<string>();
        mapGen.ApplySeed(world, changes);

        if (mapGen.ignoreSavedWorldSettings)
        {
            GameLog.LogWarning("[Save] ignoreSavedWorldSettings가 켜져 있어 생성 설정을 적용하지 않습니다. " +
                               "현재 인스펙터 값으로 지형을 만듭니다.");
        }
        else
        {
            mapGen.ApplySettings(world, changes);

            var spawner = FindSpawner(mapGen);
            if (spawner != null) spawner.ApplySettings(world, changes);

            var unlock = UnityEngine.Object.FindFirstObjectByType<MapUnlockManager>();
            if (unlock != null) unlock.ApplySettings(world, changes);
        }

        if (changes.Count == 0) return false;

        GameLog.Log($"[Save] 생성 설정을 세이브 값으로 되돌립니다 ({changes.Count}곳)\n  "
                    + string.Join("\n  ", changes));
        return true;
    }

    private static void CaptureWorld(WorldGenSave world)
    {
        var mapGen = UnityEngine.Object.FindFirstObjectByType<MapGenerator>();
        if (mapGen != null) mapGen.CaptureSettings(world);

        var spawner = FindSpawner(mapGen);
        if (spawner != null) spawner.CaptureSettings(world);

        var unlock = UnityEngine.Object.FindFirstObjectByType<MapUnlockManager>();
        if (unlock != null) unlock.CaptureSettings(world);
    }

    private static ObjectSpawner FindSpawner(MapGenerator mapGen)
    {
        if (mapGen != null && mapGen.objectSpawner != null) return mapGen.objectSpawner;
        return UnityEngine.Object.FindFirstObjectByType<ObjectSpawner>();
    }

    /// <summary>
    /// 구버전 세이브를 현재 모양으로 올린다.
    ///
    /// 버전 2까지 시드는 ProgressSave 안에 있었다. 필드 이름을 바꾸면 JsonUtility가
    /// 옛 파일의 값을 못 찾으므로 옛 필드는 남겨두고, 읽을 때 여기서 옮긴다.
    ///
    /// 두 번 불려도 안전해야 한다. PeekWorld와 Load가 각각 부른다.
    /// </summary>
    private static void MigrateIfNeeded(SaveData data)
    {
        if (data == null || data.version >= 3) return;

        if (data.world == null) data.world = new WorldGenSave();

        if (!data.world.hasSeed && data.progress != null && data.progress.hasSeed)
        {
            data.world.hasSeed = true;
            data.world.seed = data.progress.mapSeed;

            // 버전 2는 나무/돌을 전역 Random으로 뿌렸다. 배치는 재현되지 않는다.
            data.world.genVersion = 1;

            // 설정은 담겨 있지 않다. hasSettings가 false여야 chunkSize 0 같은 값이
            // 적용되는 사고를 막는다.
            data.world.hasSettings = false;
        }

        data.version = CurrentVersion;
    }

    // ───────────────────────── 저장 ─────────────────────────

    public static bool Save(int slot = 0)
    {
        try
        {
            SaveData data = Capture(slot);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            string path = PathFor(slot);

            // 쓰는 도중에 게임이 죽으면 반쪽짜리 파일이 남는다. 임시 파일에 다 쓴 뒤 교체한다.
            string temp = path + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);

            LastUsedSlot = slot;

            GameLog.Log($"[Save] 저장 완료 (슬롯 {slot}) - {path}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] 저장 실패 (슬롯 {slot}): {e.Message}");
            return false;
        }
    }

    // ───────────────────────── 불러오기 ─────────────────────────

    public static bool Load(int slot = 0)
    {
        if (!Exists(slot))
        {
            GameLog.LogWarning($"[Save] 세이브 파일이 없습니다 (슬롯 {slot}).");
            return false;
        }

        SaveData data = ReadFile(slot);
        if (data == null)
        {
            Debug.LogError($"[Save] 세이브를 읽지 못했습니다 (슬롯 {slot}).");
            return false;
        }

        if (data.version > CurrentVersion)
        {
            // 미래 버전 파일을 억지로 읽으면 조용히 값이 빠진 채 진행된다. 차라리 멈춘다.
            Debug.LogError($"[Save] 더 새로운 버전의 세이브입니다 (파일 {data.version} > 현재 {CurrentVersion}).");
            return false;
        }

        int fileVersion = data.version;
        MigrateIfNeeded(data);

        ApplyTo(data);
        LastUsedSlot = slot;

        GameLog.Log($"[Save] 불러오기 완료 (슬롯 {slot}, 버전 {fileVersion})");
        return true;
    }

    public static bool Delete(int slot = 0)
    {
        try
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            GameLog.Log($"[Save] 삭제 완료 (슬롯 {slot})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] 삭제 실패 (슬롯 {slot}): {e.Message}");
            return false;
        }
    }

    // ───────────────────────── 모으기 ─────────────────────────

    private static SaveData Capture(int slot)
    {
        var data = new SaveData
        {
            version = CurrentVersion,
            savedAtUtc = DateTime.UtcNow.ToString("o")
        };

        var player = FindPlayer();
        if (player != null)
        {
            data.player.level = player.level;
            data.player.currentExp = player.currentExp;
            data.player.currentHealth = player.currentHealth;
            data.player.currentStamina = player.currentStamina;

            Vector3 p = player.transform.position;
            data.player.posX = p.x;
            data.player.posY = p.y;
            data.player.posZ = p.z;
        }

        var stats = PlayerStats.Instance;
        if (stats != null)
        {
            data.stats.allocated = stats.CaptureAllocated();
            data.stats.availableStatPoints = stats.availableStatPoints;
            data.stats.availableSkillPoints = stats.availableSkillPoints;
        }

        var inv = Inventory.instance;
        if (inv != null)
        {
            data.inventory.items = ToSaveArray(inv.items);
            data.inventory.quickSlots = ToSaveArray(inv.quickSlots);
        }

        data.inventory.equipment = CaptureEquipment();

        CaptureProgress(data.progress);
        CaptureWorld(data.world);
        CarryOverMissing(data, slot);

        return data;
    }

    /// <summary>
    /// 이 씬에서 모으지 못한 블록은 이전 세이브의 값을 그대로 가져온다.
    ///
    /// 집 안 씬(player house 1)에는 MapGenerator도 MapUnlockManager도 SkillTreeManager도
    /// 없다. 그대로 저장하면 시드와 구역 개방, 스킬 해금이 빈 값으로 덮여 <b>월드가 사라진다</b>.
    /// 게임 안 저장 UI가 생기면서 집에서 저장 버튼을 누르는 일이 실제로 가능해졌다.
    ///
    /// "비어 있으면 가져온다"가 아니라 "관리자가 이 씬에 없으면 가져온다"로 판단한다.
    /// 스킬을 하나도 안 찍은 상태를 빈 값과 구별할 수 없기 때문이다.
    /// </summary>
    private static void CarryOverMissing(SaveData data, int slot)
    {
        bool noMapGen = UnityEngine.Object.FindFirstObjectByType<MapGenerator>() == null;
        bool noUnlock = UnityEngine.Object.FindFirstObjectByType<MapUnlockManager>() == null;
        bool noTree = UnityEngine.Object.FindFirstObjectByType<SkillTreeManager>() == null;

        if (!noMapGen && !noUnlock && !noTree) return;

        SaveData old = ReadFile(slot);
        if (old == null) return;

        MigrateIfNeeded(old);

        if (noMapGen && old.world != null && old.world.hasSeed)
            data.world = old.world;

        if (old.progress != null)
        {
            if (noUnlock) data.progress.chunks = old.progress.chunks;
            if (noTree) data.progress.unlockedSkillIDs = old.progress.unlockedSkillIDs;
        }

        GameLog.Log("[Save] 이 씬에 없는 정보(지형/구역/스킬)는 이전 세이브의 값을 유지합니다.");
    }

    private static void CaptureProgress(ProgressSave progress)
    {
        var tree = UnityEngine.Object.FindFirstObjectByType<SkillTreeManager>();
        if (tree != null) progress.unlockedSkillIDs = tree.CaptureUnlocked();

        // 시드는 버전 3에서 WorldGenSave로 옮겼다. 여기서는 쓰지 않는다.

        var unlock = UnityEngine.Object.FindFirstObjectByType<MapUnlockManager>();
        if (unlock != null) progress.chunks = unlock.CaptureChunks();
    }

    private static ItemSave[] ToSaveArray(Item[] source)
    {
        if (source == null) return new ItemSave[0];

        var result = new ItemSave[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            Item item = source[i];
            result[i] = (item != null && item.data != null)
                ? new ItemSave { itemID = item.data.itemID, quantity = item.quantity }
                : ItemSave.Empty;
        }
        return result;
    }

    private static EquipSave[] CaptureEquipment()
    {
        var equip = EquipmentManager.instance;
        if (equip == null) return new EquipSave[0];

        var list = new List<EquipSave>();
        foreach (var slot in EquipmentManager.OwnedSlots)
        {
            Item item = equip.GetEquipped(slot);
            if (item == null || item.data == null) continue;

            list.Add(new EquipSave
            {
                slotType = (int)slot,
                itemID = item.data.itemID,
                quantity = item.quantity
            });
        }
        return list.ToArray();
    }

    // ───────────────────────── 되돌리기 ─────────────────────────

    /// <summary>
    /// 불러온 값을 각 시스템에 밀어넣는다.
    ///
    /// 순서가 중요하다.
    ///   1) 월드 생성(시드/설정)과 진행도 먼저. 스킬 해금이 스탯 보너스를 바꾸므로
    ///      최대 체력에 영향을 준다. 지형을 다시 만드는 경우에도 플레이어 위치보다
    ///      먼저 끝나 있어야 한다 (GenerateChunk가 (0,0)에서 플레이어를 원점으로 옮긴다).
    ///   2) 그 다음 스탯. 최대 체력/스태미너가 스탯에서 계산되므로, 스탯이 늦게 들어오면
    ///      PlayerMove가 "최대치가 늘었다"고 보고 현재 체력을 올려버린다.
    ///   3) 그 다음 현재 체력/스태미너. 저장된 값이 그대로 들어가야 한다.
    ///   4) 인벤토리/장비는 서로 독립이라 순서가 자유롭다.
    /// </summary>
    private static void ApplyTo(SaveData data)
    {
        bool rebuildWorld = ApplyWorld(data.world);
        ApplyProgress(data.progress, rebuildWorld);

        var stats = PlayerStats.Instance;
        if (stats != null && data.stats != null)
        {
            stats.LoadFrom(data.stats.allocated,
                           data.stats.availableStatPoints,
                           data.stats.availableSkillPoints);
        }

        var player = FindPlayer();
        if (player != null && data.player != null)
        {
            player.level = Mathf.Max(1, data.player.level);
            player.currentExp = Mathf.Max(0f, data.player.currentExp);
            player.transform.position = new Vector3(data.player.posX, data.player.posY, data.player.posZ);

            // 스탯이 반영된 뒤에 불러야 최대치 기준이 맞는다.
            player.RestoreVitals(data.player.currentHealth, data.player.currentStamina);
        }

        var inv = Inventory.instance;
        if (inv != null && data.inventory != null)
        {
            inv.ReplaceAll(ToItemArray(data.inventory.items, Inventory.INVENTORY_SIZE),
                           ToItemArray(data.inventory.quickSlots, Inventory.QUICK_SLOT_SIZE));
        }

        ApplyEquipment(data.inventory?.equipment);
    }

    /// <summary>저장된 ID를 실제 ItemData로 되살린다. 못 찾은 ID는 빈 칸으로 두고 경고만 남긴다.</summary>
    private static Item[] ToItemArray(ItemSave[] source, int size)
    {
        var result = new Item[size];
        if (source == null) return result;

        int count = Mathf.Min(source.Length, size);
        for (int i = 0; i < count; i++)
        {
            ItemSave slot = source[i];
            if (slot == null || slot.IsEmpty) continue;

            ItemData found = ResolveItem(slot.itemID);
            if (found == null) continue;

            result[i] = new Item(found, slot.quantity);
        }
        return result;
    }

    private static ItemData ResolveItem(string itemID)
    {
        var manager = ItemDataManager.instance;
        if (manager == null)
        {
            Debug.LogError("[Save] ItemDataManager가 없어 아이템을 되살릴 수 없습니다.");
            return null;
        }

        ItemData found = manager.GetItemDataByID(itemID);
        if (found == null)
        {
            // 데이터베이스에서 사라진 아이템. 세이브를 통째로 버리는 것보다 그 칸만 비우는 게 낫다.
            GameLog.LogWarning($"[Save] 알 수 없는 아이템 ID '{itemID}' - 해당 칸을 비웁니다.");
        }
        return found;
    }

    private static void ApplyEquipment(EquipSave[] saved)
    {
        var equip = EquipmentManager.instance;
        if (equip == null) return;

        // 먼저 전부 벗긴다. 저장 당시 비어 있던 슬롯이 그대로 남아 있으면 안 된다.
        foreach (var slot in EquipmentManager.OwnedSlots)
            equip.Unequip(slot);

        if (saved == null) return;

        foreach (var entry in saved)
        {
            if (entry == null || entry.IsEmpty) continue;

            ItemData found = ResolveItem(entry.itemID);
            if (found == null) continue;

            // Equip이 슬롯 타입과 소유 여부를 다시 검사하므로 잘못된 조합은 알아서 걸러진다.
            equip.Equip((EquipmentSlotType)entry.slotType, new Item(found, entry.quantity));
        }
    }

    /// <summary>
    /// 스킬 해금과 구역 개방을 되돌린다.
    ///
    /// 시드와 생성 설정은 ApplyWorld가 먼저 처리한다. rebuildWorld가 true면 세계를
    /// 결정하는 값이 하나라도 달라졌다는 뜻이고, 그때만 지형을 다시 만든다.
    ///
    /// MapUnlockManager가 시작할 때 세이브의 값으로 세계를 만들기 때문에, 보통은 이미
    /// 같아서 타일맵도 나무도 건드리지 않는다. 다시 만드는 경우는 다른 세계의 세이브를
    /// 불러올 때처럼 값이 정말 다를 때뿐이다.
    /// </summary>
    private static void ApplyProgress(ProgressSave progress, bool rebuildWorld)
    {
        var tree = UnityEngine.Object.FindFirstObjectByType<SkillTreeManager>();
        if (tree != null && progress != null) tree.LoadUnlocked(progress.unlockedSkillIDs);

        // progress가 비어도(버전 1 세이브) 지형을 다시 만들어야 하면 호출해야 한다.
        var unlock = UnityEngine.Object.FindFirstObjectByType<MapUnlockManager>();
        if (unlock != null) unlock.LoadChunks(progress == null ? null : progress.chunks, rebuildWorld);
    }

    private static PlayerMove FindPlayer()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
        if (player == null) GameLog.LogWarning("[Save] PlayerMove를 찾지 못했습니다.");
        return player;
    }
}
