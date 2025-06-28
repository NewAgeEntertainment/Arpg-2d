using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Material item", fileName = "Material data - ")]
[Serializable]
public class ItemDataSO : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType;
    public int maxStackSize = 1;

    [Header("Item effects")]
    public ItemEffect_DataSO itemEffect;

    [Header("Item stat modifiers")]
    public List<ItemStatModifier> itemModifiers; // ✅ Add this!
}


// Removed duplicate [Serializable] attribute  

public class ItemStatModifier
{
    public StatType statType;
    public float value;
}
