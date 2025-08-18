// Assets/Scripts/Save System/InventorySaver.cs
using PixelCrushers;
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class InventorySaver : Saver
{
    private Inventory_Player inventory;

    private static Dictionary<string, ItemDataSO> itemLookup;
    private static readonly string[] LOOKUP_PATHS = { "Items", "Materials" };

    [System.Serializable]
    public class InventorySaveData
    {
        public List<SavedItem> items = new List<SavedItem>();
        public int gold;
    }

    [System.Serializable]
    public class SavedItem
    {
        public string itemName; // Uses itemData.name (asset filename without path)
        public int stackSize;
        public List<SavedItemModifier> modifiers; // per-instance modifiers
    }

    [System.Serializable]
    public class SavedItemModifier
    {
        public StatType statType;
        public float value;
    }

    private void Awake()
    {
        inventory = GetComponent<Inventory_Player>();
        BuildItemLookupIfNeeded();
    }

    private static void BuildItemLookupIfNeeded()
    {
        if (itemLookup != null) return;

        itemLookup = new Dictionary<string, ItemDataSO>();
        foreach (var path in LOOKUP_PATHS)
        {
            var allItems = Resources.LoadAll<ItemDataSO>(path);
            foreach (var so in allItems)
            {
                if (so == null) continue;
                if (!itemLookup.ContainsKey(so.name))
                    itemLookup.Add(so.name, so);
                else
                    Debug.LogWarning($"[InventorySaver] Duplicate ItemDataSO name: {so.name} (in {path}). Keep names unique.");
            }
        }
    }

    public override string RecordData()
    {
        if (inventory == null) return string.Empty;

        var data = new InventorySaveData { gold = inventory.gold };

        foreach (var item in inventory.itemList)
        {
            if (item?.itemData == null) continue;

            var saved = new SavedItem
            {
                itemName = item.itemData.name,
                stackSize = Mathf.Max(1, item.stackSize),
                modifiers = new List<SavedItemModifier>()
            };

            var instMods = item.GetInstanceModifiers();
            if (instMods != null && instMods.Length > 0)
            {
                foreach (var m in instMods)
                    saved.modifiers.Add(new SavedItemModifier { statType = m.statType, value = m.value });
            }

            data.items.Add(saved);
        }

        return SaveSystem.Serialize(data);
    }

    public override void ApplyData(string s)
    {
        BuildItemLookupIfNeeded();

        var data = SaveSystem.Deserialize<InventorySaveData>(s);
        if (data == null || inventory == null) return;

        inventory.itemList.Clear();

        foreach (var saved in data.items)
        {
            if (string.IsNullOrEmpty(saved.itemName)) continue;

            if (!itemLookup.TryGetValue(saved.itemName, out var itemData) || itemData == null)
            {
                Debug.LogWarning($"[InventorySaver] ItemData not found: {saved.itemName}. Ensure it’s under Resources/Items or Resources/Materials.");
                continue;
            }

            var newItem = new Inventory_Item(itemData);

            if (saved.modifiers != null && saved.modifiers.Count > 0)
            {
                var rebuilt = new ItemModifier[saved.modifiers.Count];
                for (int i = 0; i < rebuilt.Length; i++)
                    rebuilt[i] = new ItemModifier { statType = saved.modifiers[i].statType, value = saved.modifiers[i].value };
                newItem.SetInstanceModifiers(rebuilt);
            }

            if (saved.stackSize > 1)
                newItem.AddStack(saved.stackSize - 1);

            inventory.itemList.Add(newItem);
        }

        inventory.gold = data.gold;
        inventory.NotifyInventoryChanged();
    }
}

