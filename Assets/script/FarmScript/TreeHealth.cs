using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TreeHealth : MonoBehaviour, IDamageable
{
    public int hp = 20; // 나무 체력

    [Header("드랍 설정")]
    public GameObject fieldItemPrefab;

    // DropRule은 공용 타입(DropTable.cs)으로 옮겼다. 필드 이름이 그대로라 기존 프리팹 데이터는 유지된다.
    public List<DropRule> dropRules = new List<DropRule>();

    // 나무는 도끼로만 벨 수 있다는 규칙을 때리는 쪽(PlayerMove)이 아니라 여기서 알려준다.
    public string RequiredToolType => "Axe";

    private bool isDestroyed = false;
    private SpriteRenderer spriteRenderer;
    private Color baseColor; // 항상 여기로 복구한다 - 연타 중 색이 바뀐 채로 캡처되는 걸 방지
    private Coroutine hitEffectCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        hp -= damage;
        Debug.Log($"나무 타격! 데미지: {damage}, 남은 HP: {hp}");

        if (spriteRenderer != null)
        {
            // 연타로 코루틴이 겹치면 색이 안 돌아오는 문제가 있어서, 새로 시작하기 전에 이전 걸 끊는다.
            if (hitEffectCoroutine != null) StopCoroutine(hitEffectCoroutine);
            // 나무는 맞았을 때 살짝 어두운 빨간색/갈색 느낌으로 깜빡이게 연출
            hitEffectCoroutine = StartCoroutine(
                SpriteFlash.Flash(spriteRenderer, new Color(1f, 0.6f, 0.6f), 0.1f, baseColor));
        }

        if (hp <= 0)
        {
            ChopDown();
        }
    }

    private void ChopDown()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // 나무 주변으로 아이템 흩뿌리기 (실제 로직은 공용 DropTable이 담당)
        DropTable.SpawnDrops(dropRules, fieldItemPrefab, transform.position, 0.8f, name);
        Destroy(gameObject); // 나중에 여기를 '밑동(Stump)으로 이미지 변경' 등으로 업그레이드할 수 있습니다.
    }
}