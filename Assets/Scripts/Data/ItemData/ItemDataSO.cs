// Assets/Scripts/Data/Item Data/ItemDataSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Material item", fileName = "Material data - ")]
[Serializable]
public class ItemDataSO : ScriptableObject
{
    [Header("Unique Save ID")]
    [SerializeField] private string guid;
    public string Guid => guid;

    [Header("Currency (Optional)")]
    public int goldValue = 0;

    [Header("Merchant details")]
    [Range(0, 10000)] public int itemPtice = 100;
    public int minStackSizeAtShop = 1;
    public int maxStackSizeAtShop = 1;

    [Header("Drop details")]
    [Range(0, 1000)] public int itemRarity = 100;
    [Range(0, 100)] public float dropChance;
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
    public List<ItemModifier> itemModifiers;

    // Convenience flags
    public bool isUsable => itemEffect != null;
    public bool isGold => itemType == ItemType.Material && goldValue > 0;
    public bool IsConsumable => itemEffect != null && itemType == ItemType.Consumable;

    // ---- Consumable convenience helpers (no IConsumableEffect needed) ----
    public bool AffectsHealth => itemEffect != null && itemEffect.AffectsHealth;
    public bool AffectsMana => itemEffect != null && itemEffect.AffectsMana;
    public float EffectDurationSeconds => itemEffect != null ? Mathf.Max(0f, itemEffect.DurationSeconds) : 0f;
    public string EffectKey => itemEffect != null
        ? (string.IsNullOrEmpty(itemEffect.EffectKey) ? itemEffect.name : itemEffect.EffectKey)
        : string.Empty;
    public bool BlocksAtFullResources => AffectsHealth || AffectsMana;

    public float GetDropChance()
    {
        float maxRarity = 1000f;
        float chance = (maxRarity - itemRarity + 1) / maxRarity * 100f;
        return Mathf.Min(chance, maxDropChance);
    }

    // Back-compat with older callers
    public float GetDropCance() => GetDropChance();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(guid))
        {
            guid = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
        dropChance = GetDropChance();
    }
#endif
}
