using UnityEngine;


[CreateAssetMenu(menuName = "RPG Setup/Item Data/Material item", fileName = "Material data - ")]
public class ItemDataSO : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType;
    public int maxStackSize = 1; // Maximum stack size for the item

    [Header("Item effects")]
    public ItemEffect_DataSO itemEffect; // Array of item effects

}
