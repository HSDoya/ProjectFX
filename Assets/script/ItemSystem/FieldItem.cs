using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class FieldItem : MonoBehaviour
{
    public Item item; // 이 필드 아이템이 가지고 있는 실제 아이템 정보

    private SpriteRenderer spriteRenderer;

    // 동물이 죽을 때 이 메서드를 호출하여 아이템 정보를 세팅해 줍니다.
    public void Setup(ItemData data, int amount)
    {
        item = new Item(data, amount);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && data != null && data.icon != null)
        {
            spriteRenderer.sprite = data.icon;
        }

        // 아이콘 크기에 맞춰 콜라이더 크기 조정 (선택 사항)
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true; // 플레이어가 밟고 지나갈 수 있게 Trigger로 설정
        }
    }

    // 플레이어가 아이템에 닿았을 때 인벤토리로 획득
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // ★ 추가: AddItem에 들어가면 quantity가 0이 되므로, 들어가기 전에 미리 개수를 기억해 둡니다!
            int originalQuantity = item.quantity;

            // 인벤토리에 추가 시도
            if (Inventory.instance.AddItem(item))
            {
                // ★ 변경: item.quantity 대신 기억해둔 originalQuantity를 출력
                Debug.Log($"{item.data.displayName} {originalQuantity}개 획득!");
                Destroy(gameObject); // 필드에서 아이템 삭제
            }
            else
            {
                // 인벤토리가 꽉 차서 못 먹었을 때는 일부만 먹었을 수도 있으니 남은 개수 표시
                Debug.Log($"인벤토리가 가득 차서 {item.data.displayName}을(를) {item.quantity}개 남겼습니다.");
            }
        }
    }
}