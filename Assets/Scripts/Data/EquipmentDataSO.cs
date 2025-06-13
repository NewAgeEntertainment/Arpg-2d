using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Equipment item", fileName = "Equptment data - ")]
public class EquipmentDataSO : ItemDataSO
{
    [Header("Item modifiers")]
    public ItemModifier[] modifiers; // Array of modifiers that can be applied to the item
}

[Serializable]
public class ItemModifier
{
    public StatType statType;
    public float value;
}
