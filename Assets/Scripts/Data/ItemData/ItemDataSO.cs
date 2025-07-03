using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Material item", fileName = "Material data - ")]
[Serializable]
public class ItemDataSO : ScriptableObject
{
    [Header("Merchant details")]
    [Range(0, 10000)]
    public int itemPtice = 100; // Price of the item in the merchant
    public int minStackSizeAtShop = 1; // Minimum stack size of the item in the merchant
    public int maxStackSizeAtShop = 1; // Maximum stack size of the item in the merchant

    [Header("Drop details")]
    [Range(0, 1000)]
    public int itemRarity = 100;
    [Range(0, 100)]
    public float dropChance;
    [Range(0, 100)]
    public float maxDropChance = 65f;

    [Header("Craft details")]
    public Inventory_Item[] craftRecipe;

    [Header("Item details")]
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType;
    public int maxStackSize = 1;

    [Header("Item effects")]
    public ItemEffect_DataSO itemEffect;

    [Header("Item stat modifiers")]
    public List<ItemStatModifier> itemModifiers; // ✅ Add this!

    private void OnValidate()
    {
        dropChance = GetDropCance();
    }

    public float GetDropCance()
    {
        float maxRarity = 1000;
        float chance = (maxRarity - itemRarity + 1) / maxRarity * 100f;

        return Mathf.Min(chance, maxDropChance);
    }
    
}


// Removed duplicate [Serializable] attribute  


public class ItemStatModifier
{
    public StatType statType;
    public float value;
}
