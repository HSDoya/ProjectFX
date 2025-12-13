using UnityEngine;

public interface ItemSlot 
{
    /// <summary>
    /// 이 슬롯이 현재 들고 있는 아이템(없으면 null)
    /// </summary>
    Item CurrentItem { get; set; }

    /// <summary>
    /// 이 슬롯이 특정 아이템을 받을 수 있는지(슬롯 제한 규칙)
    /// </summary>
    bool CanReceive(Item item);

    /// <summary>
    /// CurrentItem 상태를 기반으로 내부 UI(아이콘/수량)를 갱신
    /// </summary>
    void Refresh();
}
