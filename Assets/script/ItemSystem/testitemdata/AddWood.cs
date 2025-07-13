using UnityEngine;

public class AddWood : MonoBehaviour
{
    [SerializeField] private ItemData woodItem;

    void Start()
    {
        for (int i = 0; i < 99; i++)
        {
            Inventory.instance.Add(woodItem);
        }

        Debug.Log("시작 시 나무 99개 지급 완료!");
    }

}
