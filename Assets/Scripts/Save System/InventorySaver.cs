//// InventorySaver.cs
//using PixelCrushers;
//using UnityEngine;
//using System.Collections.Generic;

///// <summary>
///// Saves & loads the player's backpack inventory and gold.
///// Works with any subfolder structure under Resources/Items/**.
///// Also preserves per-instance item modifiers.
///// </summary>
//[DisallowMultipleComponent]
//public class InventorySaver : Saver
//{
//    private Inventory_Player inventory;

//    // Cache of all ItemDataSO found under Resources/Items (any subfolder).
//    // Key = asset name (itemData.name), Value = ItemDataSO
//    private static Dictionary<string, ItemDataSO> itemLookup;

//    [System.Serializable]
//    public class InventorySaveData
//    {
//        public List<SavedItem> items = new List<SavedItem>();
//        public int gold;
//    }

//    [System.Serializable]
//    public class SavedItem
//    {
//        public string itemName; // Uses itemData.name (asset filename without path)
//        public int stackSize;
//        public List<SavedItemModifier> modifiers; // per-instance modifiers
//    }

//    [System.Serializable]
//    public class SavedItemModifier
//    {
//        public StatType statType;
//        public float value;
//    }

//    private void Awake()
//    {
//        inventory = GetComponent<Inventory_Player>();
//        BuildItemLookupIfNeeded();
//    }

//    private static void BuildItemLookupIfNeeded()
//    {
//        if (itemLookup != null) return;

//        itemLookup = new Dictionary<string, ItemDataSO>();

//        // This loads every ItemDataSO anywhere under Resources/Items/** (recursively).
//        var allItems = Resources.LoadAll<ItemDataSO>("Items");

//        foreach (var so in allItems)
//        {
//            if (so == null) continue;

//            // NOTE: Key is the asset's .name (the file name without extension).
//            // Keep these unique across all subfolders, or last one wins.
//            if (!itemLookup.ContainsKey(so.name))
//            {
//                itemLookup.Add(so.name, so);
//            }
//            else
//            {
//                Debug.LogWarning($"[InventorySaver] Duplicate ItemDataSO name detected: {so.name}. " +
//                                 "Ensure unique asset names across your Items folders.");
//            }
//        }
//    }

//    public override string RecordData()
//    {
//        if (inventory == null) return string.Empty;

//        var data = new InventorySaveData
//        {
//            gold = inventory.gold
//        };

//        foreach (var item in inventory.itemList)
//        {
//            if (item?.itemData == null) continue;

//            var saved = new SavedItem
//            {
//                itemName = item.itemData.name,      // ← asset file name
//                stackSize = item.stackSize,
//                modifiers = new List<SavedItemModifier>()
//            };

//            // Save per-instance modifiers (if any)
//            var instMods = item.GetInstanceModifiers();
//            if (instMods != null)
//            {
//                foreach (var m in instMods)
//                {
//                    saved.modifiers.Add(new SavedItemModifier
//                    {
//                        statType = m.statType,
//                        value = m.value
//                    });
//                }
//            }

//            data.items.Add(saved);
//        }

//        return SaveSystem.Serialize(data);
//    }

//    public override void ApplyData(string s)
//    {
//        BuildItemLookupIfNeeded();

//        var data = SaveSystem.Deserialize<InventorySaveData>(s);
//        if (data == null || inventory == null) return;

//        inventory.itemList.Clear();

//        foreach (var saved in data.items)
//        {
//            if (string.IsNullOrEmpty(saved.itemName))
//                continue;

//            if (!itemLookup.TryGetValue(saved.itemName, out var itemData) || itemData == null)
//            {
//                Debug.LogWarning($"[InventorySaver] ItemData not found for name: {saved.itemName}. " +
//                                 "Check that the asset exists under Resources/Items/** and that its file name matches.");
//                continue;
//            }

//            var newItem = new Inventory_Item(itemData);

//            // Restore per-instance modifiers
//            if (saved.modifiers != null && saved.modifiers.Count > 0)
//            {
//                var rebuilt = new ItemModifier[saved.modifiers.Count];
//                for (int i = 0; i < rebuilt.Length; i++)
//                {
//                    rebuilt[i] = new ItemModifier
//                    {
//                        statType = saved.modifiers[i].statType,
//                        value = saved.modifiers[i].value
//                    };
//                }
//                newItem.SetInstanceModifiers(rebuilt);
//            }

//            // Restore stack
//            if (saved.stackSize > 1)
//                newItem.AddStack(saved.stackSize - 1);

//            inventory.itemList.Add(newItem);
//        }

//        inventory.gold = data.gold;
//        inventory.NotifyInventoryChanged();
//    }
//}
