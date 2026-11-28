using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 세이브 값을 씬의 필드에 되돌리면서, 실제로 달라진 것만 이름과 함께 기록한다.
///
/// 로그에 "무엇이" 달라졌는지 남기는 것이 목적이다. 생성 설정은 숫자 하나가 바뀌어도
/// 지형이 전부 달라지므로, 조용히 덮어쓰면 나중에 원인을 찾을 수 없다.
/// </summary>
public static class SaveDiff
{
    public static void Set(ref int field, int value, string name, List<string> changes)
    {
        if (field == value) return;

        changes?.Add($"{name} {field} → {value}");
        field = value;
    }

    public static void Set(ref float field, float value, string name, List<string> changes)
    {
        if (Mathf.Approximately(field, value)) return;

        changes?.Add($"{name} {field} → {value}");
        field = value;
    }
}
