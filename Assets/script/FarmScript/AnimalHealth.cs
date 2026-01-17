using UnityEngine;

[DisallowMultipleComponent]
public class AnimalHealth : MonoBehaviour
{
    public int maxHP = 1;
    private int currentHP;

    private AnimalDropper dropper;

    void Awake()
    {
        currentHP = maxHP;
        dropper = GetComponent<AnimalDropper>();
    }

    // kill 안에 고기, 가죽 아이템 드롭 방식으로 변경
    public void Kill()
    {
        Die();
    }

    public void TakeDamage(int dmg)
    {
        currentHP -= dmg;
        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        if (dropper != null)
            dropper.Drop();

        Destroy(gameObject);
    }
}