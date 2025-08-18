// Assets/Scripts/Save System/StorageSaver.cs
using PixelCrushers;
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class StorageSaver : Saver
{
    [SerializeField] private Inventory_Storage storage;

    private static Dictionary<string, ItemDataSO> itemLookup;
    private static readonly string[] LOOKUP_PATHS = { "Items", "Materials" };

    [Serializable] private class SavedItem { public string itemName; public int stackSize; public List<SavedItemModifier> modifiers; }
    [Serializable] private class SavedItemModifier { public StatType statType; public float value; }
    [Serializable] private class Blob { public List<SavedItem> items = new(); public List<SavedItem> materials = new(); }

    private void Awake()
    {
        if (storage == null) storage = FindFirstObjectByType<Inventory_Storage>(FindObjectsInactive.Include);
        BuildItemLookupIfNeeded();
    }

    public override string RecordData()
    {
        var b = new Blob();
        Pack(storage?.itemList, b.items);
        Pack(storage?.materialStash, b.materials);
        return SaveSystem.Serialize(b);
    }

    public override void ApplyData(string s)
    {
        BuildItemLookupIfNeeded();
        var b = SaveSystem.Deserialize<Blob>(s);
        if (b == null || storage == null) return;

        storage.itemList.Clear();
        storage.materialStash.Clear();

        Unpack(b.items, storage.itemList);
        Unpack(b.materials, storage.materialStash);

        storage.NotifyInventoryChanged();
    }

    private static void Pack(List<Inventory_Item> src, List<SavedItem> dst)
    {
        dst.Clear(); if (src == null) return;
        foreach (var it in src)
        {
            if (it?.itemData == null) continue;
            dst.Add(new SavedItem
            {
                itemName = it.itemData.name,
                stackSize = Mathf.Max(1, it.stackSize),
                modifiers = PackMods(it.GetInstanceModifiers())
            });
        }
    }

    private static void Unpack(List<SavedItem> src, List<Inventory_Item> dst)
    {
        dst.Clear(); if (src == null) return;
        foreach (var s in src)
        {
            if (string.IsNullOrEmpty(s.itemName)) continue;

            if (!itemLookup.TryGetValue(s.itemName, out var so) || so == null)
            {
                Debug.LogWarning($"[StorageSaver] Missing SO: {s.itemName}");
                continue;
            }

            var it = new Inventory_Item(so);
            if (s.modifiers != null && s.modifiers.Count > 0) it.SetInstanceModifiers(UnpackMods(s.modifiers));
            if (s.stackSize > 1) it.AddStack(s.stackSize - 1);
            dst.Add(it);
        }
    }

    private static List<SavedItemModifier> PackMods(ItemModifier[] mods)
    {
        var list = new List<SavedItemModifier>(); if (mods == null || mods.Length == 0) return list;
        foreach (var m in mods) list.Add(new SavedItemModifier { statType = m.statType, value = m.value });
        return list;
    }

    private static ItemModifier[] UnpackMods(List<SavedItemModifier> mods)
    {
        if (mods == null || mods.Count == 0) return Array.Empty<ItemModifier>();
        var arr = new ItemModifier[mods.Count];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = new ItemModifier { statType = mods[i].statType, value = mods[i].value };
        return arr;
    }

    private static void BuildItemLookupIfNeeded()
    {
        if (itemLookup != null) return;

        itemLookup = new Dictionary<string, ItemDataSO>();
        foreach (var path in LOOKUP_PATHS)
        {
            var sos = Resources.LoadAll<ItemDataSO>(path);
            foreach (var so in sos)
            {
                if (so == null) continue;
                if (!itemLookup.ContainsKey(so.name))
                    itemLookup.Add(so.name, so);
                else
                    Debug.LogWarning($"[StorageSaver] Duplicate ItemDataSO name: {so.name} (in {path}).");
            }
        }
    }
}
