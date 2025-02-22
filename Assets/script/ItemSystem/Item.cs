using UnityEngine;

[System.Serializable]
public class Item
{
    public string item_name;
    //public Sprite icon; icon �̹��� �߰��� �߰�
    public bool isDefaultItem = false;

    public virtual void Use()
    {
        Debug.Log("Using" + item_name);
    }
}
