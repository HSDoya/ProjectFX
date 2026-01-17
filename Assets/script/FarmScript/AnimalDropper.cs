using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class AnimalDropper : MonoBehaviour
{
    [System.Serializable]
    public class DropItem
    {
        public GameObject prefab;
        public Vector2Int countRange = new Vector2Int(1, 1);
    }

    public List<DropItem> drops = new List<DropItem>();

    public void Drop()
    {
        foreach (var d in drops)
        {
            if (d.prefab == null) continue;

            int count = Random.Range(d.countRange.x, d.countRange.y + 1);
            for (int i = 0; i < count; i++)
            {
                Instantiate(d.prefab, transform.position, Quaternion.identity);
            }
        }
    }
}