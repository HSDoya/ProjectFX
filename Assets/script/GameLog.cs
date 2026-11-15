using System.Diagnostics;
using UnityDebug = UnityEngine.Debug;

/// <summary>
/// 개발용 로그 래퍼.
///
/// [Conditional] 이 붙은 메서드는 해당 심볼이 정의되지 않은 빌드에서 "호출 자체가" 사라진다.
/// 인자로 넘긴 문자열 결합($"...")까지 같이 제거되므로, Debug.Log를 그냥 두는 것보다
/// 문자열 할당 비용도 함께 아낄 수 있다.
///
/// 심볼 기준:
///   UNITY_EDITOR      - 에디터 플레이 모드
///   DEVELOPMENT_BUILD - Build Settings에서 Development Build를 켠 빌드
///
/// 즉 릴리즈 빌드에서는 Log/LogWarning이 완전히 빠진다.
/// 치명적 오류는 릴리즈에서도 보여야 하므로 Debug.LogError는 감싸지 않고 그대로 쓴다.
/// </summary>
public static class GameLog
{
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message) => UnityDebug.Log(message);

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, UnityEngine.Object context) => UnityDebug.Log(message, context);

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message) => UnityDebug.LogWarning(message);

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message, UnityEngine.Object context) => UnityDebug.LogWarning(message, context);
}
