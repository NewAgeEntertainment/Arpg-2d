//using PixelCrushers;
//using System;
//using System.Collections.Generic;
//using UnityEngine;

///// <summary>
///// Saves & loads the player's Equipment Inventory (bag) and Equipped slots,
///// including per-instance item modifiers, using Resources/Items/** lookup by asset name.
///// No ItemRegistry/DB required.
///// </summary>
//[DisallowMultipleComponent]
//public class EquipmentSaver : Saver
//{
//    [Header("Auto-found if not assigned")]
//    [SerializeField] private Inventory_Player playerInv;
//    [SerializeField] private Inventory_Equipment equipmentInv;
//    [SerializeField] private Player player;

//    // Same lookup approach as your InventorySaver: name -> SO
//    private static Dictionary<string, ItemDataSO> itemLookup;

//    [Serializable]
//    public class SavedItemModifier
//    {
//        public StatType statType;
//        public float value;
//    }

//    [Serializable]
//    public class SavedBagItem
//    {
//        public string itemName;               // Asset file name (SO.name)
//        public int stackSize;
//        public List<SavedItemModifier> modifiers; // per-instance mods
//    }

//    [Serializable]
//    public class SavedEquipped
//    {
//        public string slotType;               // ItemType.ToString(), e.g. "Weapon", "Armor", "trinket"
//        public string itemName;               // Asset file name (SO.name)
//        public List<SavedItemModifier> modifiers; // the instance mods applied to that equipped copy
//    }

//    [Serializable]
//    public class SaveData
//    {
//        public List<SavedBagItem> equipmentBag = new();
//        public List<SavedEquipped> equipped = new();
//    }

//    private void Awake()
//    {
//        if (playerInv == null) playerInv = FindFirstObjectByType<Inventory_Player>();
//        if (equipmentInv == null) equipmentInv = FindFirstObjectByType<Inventory_Equipment>();
//        if (player == null) player = FindFirstObjectByType<Player>();
//        BuildItemLookupIfNeeded();
//    }

//    private static void BuildItemLookupIfNeeded()
//    {
//        if (itemLookup != null) return;

//        itemLookup = new Dictionary<string, ItemDataSO>();
//        var all = Resources.LoadAll<ItemDataSO>("Items"); // recursive under Resources/Items/**
//        foreach (var so in all)
//        {
//            if (so == null) continue;
//            if (!itemLookup.ContainsKey(so.name))
//                itemLookup.Add(so.name, so);
//            else
//                Debug.LogWarning($"[EquipmentSaver] Duplicate ItemDataSO name: {so.name}. Keep asset names unique.");
//        }
//    }

//    public override string RecordData()
//    {
//        if (equipmentInv == null || playerInv == null) return string.Empty;

//        var data = new SaveData();

//        // Save equipment bag
//        foreach (var it in equipmentInv.itemList)
//        {
//            if (it?.itemData == null) continue;

//            var saved = new SavedBagItem
//            {
//                itemName = it.itemData.name,
//                stackSize = it.stackSize,
//                modifiers = new List<SavedItemModifier>()
//            };

//            var instMods = it.GetInstanceModifiers();
//            if (instMods != null)
//            {
//                foreach (var m in instMods)
//                    data.equipmentBag.Add(new SavedBagItem
//                    {
//                        itemName = it.itemData.name,
//                        stackSize = it.stackSize,
//                        modifiers = PackMods(instMods)
//                    });
//            }
//            else
//            {
//                data.equipmentBag.Add(saved);
//            }
//        }

//        // Save equipped (slot -> item)
//        if (playerInv.equipList != null)
//        {
//            foreach (var slot in playerInv.equipList)
//            {
//                if (slot == null || !slot.HasItem() || slot.equipedItem?.itemData == null) continue;

//                data.equipped.Add(new SavedEquipped
//                {
//                    slotType = slot.slotType.ToString(),
//                    itemName = slot.equipedItem.itemData.name,
//                    modifiers = PackMods(slot.equipedItem.GetInstanceModifiers())
//                });
//            }
//        }

//        return SaveSystem.Serialize(data);
//    }

//    public override void ApplyData(string s)
//    {
//        BuildItemLookupIfNeeded();

//        var data = SaveSystem.Deserialize<SaveData>(s);
//        if (data == null || equipmentInv == null || playerInv == null) return;

//        // 1) Clear existing bag
//        equipmentInv.itemList.Clear();

//        // 2) Rebuild bag
//        foreach (var saved in data.equipmentBag)
//        {
//            if (string.IsNullOrEmpty(saved.itemName)) continue;
//            if (!itemLookup.TryGetValue(saved.itemName, out var so) || so == null)
//            {
//                Debug.LogWarning($"[EquipmentSaver] Missing ItemDataSO for '{saved.itemName}'. Skipping.");
//                continue;
//            }

//            var item = new Inventory_Item(so);
//            // Per-instance modifiers
//            if (saved.modifiers != null && saved.modifiers.Count > 0)
//                item.SetInstanceModifiers(UnpackMods(saved.modifiers));

//            if (saved.stackSize > 1)
//                item.AddStack(saved.stackSize - 1);

//            equipmentInv.itemList.Add(item);
//        }

//        // 3) Clear currently equipped (remove their effects)
//        if (playerInv.equipList != null)
//        {
//            foreach (var slot in playerInv.equipList)
//            {
//                if (slot == null || !slot.HasItem()) continue;
//                var old = slot.equipedItem;
//                if (old != null)
//                {
//                    old.RemoveModifiers(playerInv.player.stats);
//                    old.RemoveItemEffect();
//                }
//                slot.equipedItem = null;
//            }
//        }

//        // 4) Re-equip from save (and remove that copy from bag)
//        if (data.equipped != null && playerInv.equipList != null)
//        {
//            foreach (var eq in data.equipped)
//            {
//                if (string.IsNullOrEmpty(eq.itemName) || string.IsNullOrEmpty(eq.slotType)) continue;

//                if (!itemLookup.TryGetValue(eq.itemName, out var so) || so == null) continue;

//                // Find the target slot by type string
//                var slot = playerInv.equipList.Find(s => s != null && s.slotType.ToString() == eq.slotType);
//                if (slot == null) continue;

//                // If the bag has a matching item, use that exact instance (so the bag count stays correct)
//                Inventory_Item instanceFromBag = null;
//                for (int i = 0; i < equipmentInv.itemList.Count; i++)
//                {
//                    var candidate = equipmentInv.itemList[i];
//                    if (candidate?.itemData == so)
//                    {
//                        instanceFromBag = candidate;
//                        equipmentInv.itemList.RemoveAt(i);
//                        break;
//                    }
//                }

//                // If not found in bag, create a new instance anyway (edge case)
//                var equippedItem = instanceFromBag ?? new Inventory_Item(so);

//                // Restore per-instance modifiers for the equipped copy
//                if (eq.modifiers != null && eq.modifiers.Count > 0)
//                    equippedItem.SetInstanceModifiers(UnpackMods(eq.modifiers));

//                // Equip: set slot, apply stat mods & effects
//                slot.equipedItem = equippedItem;
//                slot.equipedItem.AddModifiers(playerInv.player.stats);
//                slot.equipedItem.AddItemEffect(playerInv.player);
//            }
//        }

//        // 5) Refresh UIs
//        equipmentInv.NotifyInventoryChanged();
//        playerInv.NotifyInventoryChanged();
//        player?.ui?.StatusPanel?.UpdateStatus(player);
//    }

//    private static List<SavedItemModifier> PackMods(ItemModifier[] mods)
//    {
//        var list = new List<SavedItemModifier>();
//        if (mods == null) return list;
//        foreach (var m in mods)
//            list.Add(new SavedItemModifier { statType = m.statType, value = m.value });
//        return list;
//    }

//    private static ItemModifier[] UnpackMods(List<SavedItemModifier> mods)
//    {
//        if (mods == null || mods.Count == 0) return Array.Empty<ItemModifier>();
//        var arr = new ItemModifier[mods.Count];
//        for (int i = 0; i < arr.Length; i++)
//            arr[i] = new ItemModifier { statType = mods[i].statType, value = mods[i].value };
//        return arr;
//    }
//}
