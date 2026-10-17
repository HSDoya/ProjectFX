using System.Collections;
using UnityEngine;

/// <summary>
/// 맞았을 때 스프라이트를 잠깐 다른 색으로 바꾸는 연출. 몹/나무/돌이 공용으로 쓴다.
/// </summary>
/// 예전엔 세 클래스가 거의 같은 코루틴을 각자 들고 있었는데, 미묘하게 달랐다 -
/// 나무/돌은 원래 색(baseColor)으로 되돌리고 몹은 Color.white로 하드코딩해서,
/// 원래 색이 흰색이 아닌 몹은 맞을 때마다 색이 틀어졌다. 복구할 색을 받게 해서 하나로 합쳤다.
public static class SpriteFlash
{
    public static IEnumerator Flash(SpriteRenderer renderer, Color flashColor, float duration, Color restoreColor)
    {
        if (renderer == null) yield break;

        renderer.color = flashColor;
        yield return new WaitForSeconds(duration);

        // 연타로 코루틴이 겹칠 수 있으므로, 호출하는 쪽에서 이전 코루틴을 StopCoroutine으로 끊고
        // 새로 시작해야 한다(그래야 색이 중간 상태로 눌러붙지 않는다).
        if (renderer != null) renderer.color = restoreColor;
    }
}
