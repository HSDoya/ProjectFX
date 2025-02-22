using UnityEngine;

[System.Serializable]
public class Item
{
    public string item_name;
    //public Sprite icon; icon 이미지 추가시 추가
    public bool isDefaultItem = false;

    public virtual void Use()
    {
        Debug.Log("Using" + item_name);
    }
}
