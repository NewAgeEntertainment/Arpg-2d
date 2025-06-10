using UnityEngine;


[CreateAssetMenu(menuName = "RPG Setup/Item Data/Material item", fileName = "Material data - ")]
public class ItemDataSO : ScriptableObject
{
    public string ItemName;
    public Sprite ItemIcon;
    public ItemType itemType;
}
