using UnityEngine;

public class AnimalHealth : MonoBehaviour
{
    public int hp = 1;

    [Header("드랍 설정")]
    public GameObject dropPrefab;
    public int dropCount = 2;

    private bool isDead = false;

    public void Kill()
    {
        if (isDead) return;
        isDead = true;

        DropItems();

        Destroy(gameObject);
    }

    void DropItems()
    {
        if (dropPrefab == null) return;

        for (int i = 0; i < dropCount; i++)
        {
            Vector3 pos = transform.position +
                (Vector3)Random.insideUnitCircle * 0.3f;

            Instantiate(dropPrefab, pos, Quaternion.identity);
        }
    }
}