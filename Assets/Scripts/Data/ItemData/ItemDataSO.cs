using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Material item", fileName = "Material data - ")]
[Serializable]
public class ItemDataSO : ScriptableObject
{
    [Header("Unique Save ID")]
    [SerializeField] private string guid;                 // Auto-assigned for save lookups
    public string Guid => guid;

    [Header("Currency (Optional)")]
    public int goldValue = 0;

    [Header("Merchant details")]
    [Range(0, 10000)] public int itemPtice = 100;         // (Keeping your original name)
    public int minStackSizeAtShop = 1;
    public int maxStackSizeAtShop = 1;

    [Header("Drop details")]
    [Range(0, 1000)] public int itemRarity = 100;         // Lower is rarer, higher is more common (depends on your design)
    [Range(0, 100)] public float dropChance;              // Computed from rarity
    [Range(0, 100)] public float maxDropChance = 65f;

    [Header("Craft details")]
    public Inventory_Item[] craftRecipe;

    [Header("Item details")]
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType;
    public int maxStackSize = 1;

    [Header("Item effects")]
    public ItemEffect_DataSO itemEffect;

    [Header("Item stat modifiers (SO-level defaults)")]
    public List<ItemModifier> itemModifiers;              // For non-instance uses (tooltips, defaults)

    // Convenience flags
    public bool isUsable => itemEffect != null;
    public bool isGold => itemType == ItemType.Material && goldValue > 0;

    public float GetDropChance()
    {
        // Example: invert rarity into chance, then clamp by maxDropChance.
        // Adjust formula to your game’s balance.
        float maxRarity = 1000f;
        float chance = (maxRarity - itemRarity + 1) / maxRarity * 100f;
        return Mathf.Min(chance, maxDropChance);
    }

    // ✅ Back-compat shim so old calls to GetDropCance() don’t break:
    public float GetDropCance() => GetDropChance();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-generate a GUID once
        if (string.IsNullOrEmpty(guid))
        {
            guid = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        // Keep dropChance synced in the editor for easy viewing/tuning
        dropChance = GetDropChance();
    }
#endif
}
