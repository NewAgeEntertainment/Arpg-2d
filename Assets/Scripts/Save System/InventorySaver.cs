// Assets/Scripts/Save System/InventorySaver.cs
using PixelCrushers;
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class InventorySaver : Saver
{
    private Inventory_Player inventory;

    // Multi-path ItemData lookup (add more folders if needed)
    private static Dictionary<string, ItemDataSO> itemLookup;
    private static readonly string[] LOOKUP_PATHS = { "Items", "Materials" };

    [System.Serializable]
    public class InventorySaveData
    {
        public List<SavedItem> items = new List<SavedItem>();   // backpack
        public int gold;

        // NEW: quick slots (fixed 4 entries, index = slot number - 1)
        public List<SavedQuickSlot> quickSlots = new List<SavedQuickSlot>(4);
    }

    [System.Serializable]
    public class SavedItem
    {
        public string itemName; // itemData.name
        public int stackSize;
        public List<SavedItemModifier> modifiers;
    }

    [System.Serializable]
    public class SavedQuickSlot
    {
        public string itemName; // null/empty means empty slot
        public int stackSize;
        public List<SavedItemModifier> modifiers;
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

        // ---- Backpack ----
        foreach (var item in inventory.itemList)
        {
            if (item?.itemData == null) continue;

            var saved = new SavedItem
            {
                itemName = item.itemData.name,
                stackSize = Mathf.Max(1, item.stackSize),
                modifiers = PackMods(item.GetInstanceModifiers())
            };

            data.items.Add(saved);
        }

        // ---- Quick Slots (4) ----
        data.quickSlots.Clear();
        int slotCount = (inventory.quickSlots != null) ? inventory.quickSlots.Length : 0;
        for (int i = 0; i < 4; i++)
        {
            Inventory_Player.QuickSlot src = default;
            if (i < slotCount) src = inventory.quickSlots[i];

            if (src.item == null || src.slotStack <= 0 || src.item.itemData == null)
            {
                data.quickSlots.Add(new SavedQuickSlot
                {
                    itemName = null,
                    stackSize = 0,
                    modifiers = new List<SavedItemModifier>()
                });
            }
            else
            {
                data.quickSlots.Add(new SavedQuickSlot
                {
                    itemName = src.item.itemData.name,
                    stackSize = Mathf.Max(1, src.slotStack),
                    modifiers = PackMods(src.item.GetInstanceModifiers())
                });
            }
        }

        return SaveSystem.Serialize(data);
    }

    public override void ApplyData(string s)
    {
        BuildItemLookupIfNeeded();

        var data = SaveSystem.Deserialize<InventorySaveData>(s);
        if (data == null || inventory == null) return;

        // ---- Backpack ----
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
                newItem.SetInstanceModifiers(UnpackMods(saved.modifiers));

            if (saved.stackSize > 1)
                newItem.AddStack(saved.stackSize - 1);

            inventory.itemList.Add(newItem);
        }

        // ---- Gold ----
        inventory.gold = data.gold;

        // ---- Quick Slots (restore exact 4) ----
        if (inventory.quickSlots == null || inventory.quickSlots.Length != 4)
        {
            // If field is not serialized or size differs, create 4 slots.
            inventory.quickSlots = new Inventory_Player.QuickSlot[4];
        }

        for (int i = 0; i < 4; i++)
        {
            var dst = new Inventory_Player.QuickSlot(); // works for struct/class
            if (data.quickSlots != null && i < data.quickSlots.Count)
            {
                var saved = data.quickSlots[i];
                if (!string.IsNullOrEmpty(saved.itemName) && saved.stackSize > 0)
                {
                    if (!itemLookup.TryGetValue(saved.itemName, out var so) || so == null)
                    {
                        Debug.LogWarning($"[InventorySaver] QuickSlot {i + 1}: ItemData not found for '{saved.itemName}'. Leaving empty.");
                    }
                    else
                    {
                        var item = new Inventory_Item(so);
                        if (saved.modifiers != null && saved.modifiers.Count > 0)
                            item.SetInstanceModifiers(UnpackMods(saved.modifiers));

                        dst.item = item;
                        dst.slotStack = Mathf.Max(1, saved.stackSize);
                    }
                }
            }

            // Assign back (struct-safe)
            inventory.quickSlots[i] = dst;
        }

        // One notify to refresh UI (UI_InGame listens and will repaint quick slots)
        inventory.NotifyInventoryChanged();
    }

    // ---------- helpers ----------
    private static List<SavedItemModifier> PackMods(ItemModifier[] mods)
    {
        var list = new List<SavedItemModifier>();
        if (mods == null || mods.Length == 0) return list;
        foreach (var m in mods)
            list.Add(new SavedItemModifier { statType = m.statType, value = m.value });
        return list;
    }

    private static ItemModifier[] UnpackMods(List<SavedItemModifier> mods)
    {
        if (mods == null || mods.Count == 0) return System.Array.Empty<ItemModifier>();
        var arr = new ItemModifier[mods.Count];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = new ItemModifier { statType = mods[i].statType, value = mods[i].value };
        return arr;
    }
}
