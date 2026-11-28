#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// 개발용 치트 버튼. 화면 왼쪽 아래에 뜬다.
///
/// 릴리즈 빌드에는 파일 전체가 컴파일되지 않는다(#if). 에디터 플레이와
/// Development Build에서만 존재한다.
///
/// 씬에 오브젝트를 두지 않는다. RuntimeInitializeOnLoadMethod로 스스로 생겨나므로
/// 씬 파일도 프리팹도 건드리지 않고, UI 생성기를 다시 돌릴 필요도 없다.
/// (씬에 넣어두면 그것도 "집 씬에 중복된 매니저" 목록에 하나 더 추가되는 꼴이 된다.)
/// </summary>
public class DevCheatPanel : MonoBehaviour
{
    private const string ObjectName = "[Dev] CheatPanel";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindFirstObjectByType<DevCheatPanel>() != null) return;

        var go = new GameObject(ObjectName);
        go.AddComponent<DevCheatPanel>();
        DontDestroyOnLoad(go);
    }

    // 접어두는 상태. 테스트 중 화면을 가리면 거슬리므로 한 번 눌러 접을 수 있게 한다.
    private bool expanded = true;

    private PlayerMove player;
    private PlayerStats Stats => PlayerStats.Instance;

    private PlayerMove Player
    {
        get
        {
            if (player == null) player = FindFirstObjectByType<PlayerMove>();
            return player;
        }
    }

    private void OnGUI()
    {
        const float width = 190f;
        float x = 12f;
        float y = Screen.height - (expanded ? 150f : 40f) - 12f;

        GUILayout.BeginArea(new Rect(x, y, width, expanded ? 150f : 40f), GUI.skin.box);

        if (GUILayout.Button(expanded ? "▼ 개발자 도구" : "▶ 개발자 도구", GUILayout.Height(24)))
            expanded = !expanded;

        if (expanded)
        {
            var p = Player;
            var s = Stats;

            GUILayout.Label(p != null
                ? $"Lv.{p.level}   HP {p.currentHealth:0}/{p.MaxHealth:0}"
                : "플레이어 없음");

            GUILayout.Label(s != null
                ? $"남은 포인트  스탯 {s.availableStatPoints} / 스킬 {s.availableSkillPoints}"
                : "PlayerStats 없음");

            GUI.enabled = p != null;

            // 레벨을 직접 올리지 않고 경험치를 부어서 올린다. 그래야 실제 경로
            // (AddExp -> GrantLevelUpPoints -> OnLevelUp -> HUD 연출)가 그대로 돌아간다.
            if (GUILayout.Button("레벨 +1", GUILayout.Height(26)))
                LevelUpOnce();

            // 저장/불러오기 테스트에는 "최대치와 다른 현재 체력"이 필요하다.
            // 체력이 가득 찬 상태로 저장하면 복원 순서가 틀려도 증상이 안 드러난다.
            if (GUILayout.Button("체력 -10", GUILayout.Height(22)))
                DamageSelf(10f);

            GUI.enabled = true;
        }

        GUILayout.EndArea();
    }

    /// <summary>정확히 한 레벨만 오르도록 모자란 경험치만큼만 넣는다.</summary>
    private void LevelUpOnce()
    {
        var p = Player;
        if (p == null) return;

        int need = Mathf.CeilToInt(p.ExpToNextLevel - p.currentExp);
        p.AddExp(Mathf.Max(1, need));
    }

    /// <summary>
    /// 피격 처리를 거치지 않고 현재 체력만 깎는다.
    /// 무적시간·넉백·사망 처리에 얽히면 테스트가 지저분해진다.
    /// </summary>
    private void DamageSelf(float amount)
    {
        var p = Player;
        if (p == null) return;

        p.currentHealth = Mathf.Clamp(p.currentHealth - amount, 1f, p.MaxHealth);
    }
}
#endif
