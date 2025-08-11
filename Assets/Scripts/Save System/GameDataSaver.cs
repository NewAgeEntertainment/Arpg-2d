// Assets/Scripts/Save System/GameDataSaver.cs
using PixelCrushers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkillTreeState = UI_SkillTree.SkillTreeState;

[DisallowMultipleComponent]
public class GameDataSaver : Saver
{
    [Header("Auto-found if not assigned")]
    [SerializeField] private Inventory_Player playerInv;
    [SerializeField] private Inventory_Equipment equipmentInv;
    [SerializeField] private Inventory_Storage storage;
    [SerializeField] private Player player;
    [SerializeField] private Player_Stats stats;
    [SerializeField] private Entity_Health health;
    [SerializeField] private Entity_Mana mana;

    // Cache: Item name -> SO (Resources/Items/**)
    private static Dictionary<string, ItemDataSO> itemLookup;

    #region DTOs

    [Serializable]
    public class SavedItemModifier
    {
        public StatType statType;
        public float value;
    }

    [Serializable]
    public class SavedStackItem
    {
        public string itemName;                    // ItemDataSO.name
        public int stackSize;
        public List<SavedItemModifier> modifiers;  // per-instance modifiers (Inventory_Item)
    }

    [Serializable]
    public class SavedEquipped
    {
        public string slotType;                    // ItemType.ToString()
        public string itemName;                    // ItemDataSO.name
        public List<SavedItemModifier> modifiers;  // per-instance modifiers applied to that equipped copy
    }

    

    [Serializable]
    public class SaveBlob
    {
        // Money & vitals
        public int gold;
        public float currentHealth;
        public float currentMana;

        // Normal Level/EXP
        public int level;
        public float currentExp;

        // Sex Level/EXP
        public int sexLevel;
        public float sexExp;

        // Inventories
        public List<SavedStackItem> backpack = new();        // Inventory_Player.itemList
        public List<SavedStackItem> equipmentBag = new();     // Inventory_Equipment.itemList
        public List<SavedStackItem> storageItems = new();     // Inventory_Storage.itemList
        public List<SavedStackItem> storageMaterials = new(); // Inventory_Storage.materialStash

        // Equipped slots
        public List<SavedEquipped> equipped = new();

        // Scene & position
        public string lastScene;
        public Vector3 lastPlayerPosition;

        // Skill tree
        public SkillTreeState skillTree = new SkillTreeState();
    }

    #endregion

    private void Awake()
    {
        CacheRefs();
        BuildItemLookupIfNeeded();
    }

    private void CacheRefs()
    {
        if (playerInv == null) playerInv = FindFirstObjectByType<Inventory_Player>(UnityEngine.FindObjectsInactive.Include);
        if (equipmentInv == null) equipmentInv = FindFirstObjectByType<Inventory_Equipment>(UnityEngine.FindObjectsInactive.Include);
        if (storage == null) storage = FindFirstObjectByType<Inventory_Storage>(UnityEngine.FindObjectsInactive.Include);
        if (player == null) player = FindFirstObjectByType<Player>(UnityEngine.FindObjectsInactive.Include);
        if (stats == null) stats = player != null ? player.GetComponent<Player_Stats>() : null;
        if (health == null) health = player != null ? player.GetComponent<Entity_Health>() : null;
        if (mana == null) mana = player != null ? player.GetComponent<Entity_Mana>() : null;
    }

    private static void BuildItemLookupIfNeeded()
    {
        if (itemLookup != null) return;

        itemLookup = new Dictionary<string, ItemDataSO>();
        var all = Resources.LoadAll<ItemDataSO>("Items"); // recursive under Resources/Items/**
        foreach (var so in all)
        {
            if (so == null) continue;
            if (!itemLookup.ContainsKey(so.name))
                itemLookup.Add(so.name, so);
            else
                Debug.LogWarning($"[GameDataSaver] Duplicate ItemDataSO name detected: {so.name}. Keep asset names unique.");
        }
    }

    public override string RecordData()
    {
        CacheRefs();
        BuildItemLookupIfNeeded();

        var data = new SaveBlob();

        // Gold
        data.gold = playerInv != null ? playerInv.gold : 0;

        // HP/MP
        data.currentHealth = health != null ? health.GetCurrentHealth() : 0f;
        data.currentMana = mana != null ? mana.GetCurrentMana() : 0f;

        // Normal Level/EXP
        if (stats != null)
        {
            data.level = stats.CurrentLevel;
            data.currentExp = stats.CurrentEXP;
        }

        // Sex Level/EXP
        if (player != null)
        {
            data.sexLevel = player.SexLevel;
            data.sexExp = player.CurrentSexExp;
        }

        // Inventories
        if (playerInv != null) PackList(playerInv.itemList, data.backpack);
        if (equipmentInv != null) PackList(equipmentInv.itemList, data.equipmentBag);
        if (storage != null)
        {
            PackList(storage.itemList, data.storageItems);
            PackList(storage.materialStash, data.storageMaterials);
        }

        // Equipped
        if (playerInv != null && playerInv.equipList != null)
        {
            foreach (var slot in playerInv.equipList)
            {
                if (slot == null || !slot.HasItem() || slot.equipedItem?.itemData == null) continue;

                data.equipped.Add(new SavedEquipped
                {
                    slotType = slot.slotType.ToString(),
                    itemName = slot.equipedItem.itemData.name,
                    modifiers = PackMods(slot.equipedItem.GetInstanceModifiers())
                });
            }
        }

        // Scene & position
        data.lastScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (player != null) data.lastPlayerPosition = player.transform.position;

        // Skill tree
        data.skillTree = CaptureSkillTreeState();

        return SaveSystem.Serialize(data);
    }

    public override void ApplyData(string s)
    {
        CacheRefs();
        BuildItemLookupIfNeeded();

        if (string.IsNullOrEmpty(s)) return;
        var data = SaveSystem.Deserialize<SaveBlob>(s);
        if (data == null) return;

        // Money
        if (playerInv != null)
        {
            playerInv.gold = data.gold;
            playerInv.NotifyInventoryChanged();
        }

        // HP/MP
        if (health != null && data.currentHealth > 0f)
            health.SetCurrentHealth(data.currentHealth);
        if (mana != null)
            mana.SetCurrentMana(data.currentMana);

        // Normal Level/EXP
        if (stats != null)
            stats.SetLevelAndExp(data.level, data.currentExp);

        // Sex Level/EXP
        if (player != null)
        {
            player.SetSexLevel(data.sexLevel);
            player.SetCurrentSexEXP(data.sexExp);
        }

        // Backpack
        if (playerInv != null)
        {
            playerInv.itemList.Clear();
            UnpackList(data.backpack, playerInv.itemList);
            playerInv.NotifyInventoryChanged();
        }

        // Equipment Bag
        if (equipmentInv != null)
        {
            equipmentInv.itemList.Clear();
            UnpackList(data.equipmentBag, equipmentInv.itemList);
            equipmentInv.NotifyInventoryChanged();
        }

        // Storage
        if (storage != null)
        {
            storage.itemList.Clear();
            storage.materialStash.Clear();

            UnpackList(data.storageItems, storage.itemList);
            UnpackList(data.storageMaterials, storage.materialStash);

            storage.NotifyInventoryChanged();
        }

        // Equipped: clear current, then re-equip from saved
        if (playerInv != null && playerInv.equipList != null)
        {
            // Clear current equipped (remove effects)
            foreach (var slot in playerInv.equipList)
            {
                if (slot == null || !slot.HasItem()) continue;
                var old = slot.equipedItem;
                if (old != null)
                {
                    old.RemoveModifiers(playerInv.player.stats);
                    old.RemoveItemEffect();
                }
                slot.equipedItem = null;
            }

            // Re-equip using instances from equipment bag if available
            if (data.equipped != null)
            {
                foreach (var eq in data.equipped)
                {
                    if (string.IsNullOrEmpty(eq.itemName) || string.IsNullOrEmpty(eq.slotType)) continue;

                    if (!itemLookup.TryGetValue(eq.itemName, out var so) || so == null) continue;

                    // Find the first free slot matching the type; fallback to first of that type
                    var targetSlot = playerInv.equipList.Find(s => s != null && s.slotType.ToString() == eq.slotType && !s.HasItem());
                    if (targetSlot == null)
                        targetSlot = playerInv.equipList.Find(s => s != null && s.slotType.ToString() == eq.slotType);
                    if (targetSlot == null) continue;

                    // Try to take one instance from equipment bag
                    Inventory_Item instanceFromBag = null;
                    if (equipmentInv != null)
                    {
                        for (int i = 0; i < equipmentInv.itemList.Count; i++)
                        {
                            var candidate = equipmentInv.itemList[i];
                            if (candidate?.itemData == so)
                            {
                                instanceFromBag = candidate;
                                equipmentInv.itemList.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    // If not found, create a new instance anyway
                    var equippedItem = instanceFromBag ?? new Inventory_Item(so);

                    // Restore per-instance modifiers
                    if (eq.modifiers != null && eq.modifiers.Count > 0)
                        equippedItem.SetInstanceModifiers(UnpackMods(eq.modifiers));

                    // Apply to slot and add effects/mods
                    targetSlot.equipedItem = equippedItem;
                    targetSlot.equipedItem.AddModifiers(playerInv.player.stats);
                    targetSlot.equipedItem.AddItemEffect(playerInv.player);
                }
            }

            // Refresh inventories/UI once after equipping
            equipmentInv?.NotifyInventoryChanged();
            playerInv.NotifyInventoryChanged();
        }

        // Optional: reposition player if desired
        // if (player != null) player.transform.position = data.lastPlayerPosition;

        // UI refresh
        player?.ui?.inGameUI?.UpdateGoldDisplay(playerInv?.gold ?? 0);
        player?.ui?.inGameUI?.UpdateExpBar();
        player?.ui?.inGameUI?.UpdateSexExpBar();
        player?.ui?.playerHealthBar?.UpdateHealth(health?.GetCurrentHealth() ?? 0, stats?.GetMaxHealth() ?? 0);
        player?.ui?.playerManaBar?.UpdateMana(mana?.GetCurrentMana() ?? 0, stats?.GetMaxMana() ?? 0);
        player?.ui?.StatusPanel?.UpdateStatus(player);

        // Apply skill tree state (async so UI is ready), then rebind skill bar icons
        StartCoroutine(ApplySkillTreeWhenReady(data.skillTree));

        // Force inventory panel rebuild one frame later (fixes “only updates after opening Equipment” issue)
        StartCoroutine(RefreshInventoryUIAfterLoadCo());
    }

    private IEnumerator RefreshInventoryUIAfterLoadCo()
    {
        yield return null; // one frame
        var invUI = FindFirstObjectByType<UI_Inventory>(UnityEngine.FindObjectsInactive.Include);
        invUI?.ForceRefresh();
    }

    #region Skill Tree Save/Load

    private SkillTreeState CaptureSkillTreeState()
    {
        var state = new SkillTreeState();

        var tree = FindFirstObjectByType<UI_SkillTree>(UnityEngine.FindObjectsInactive.Include);
        if (tree == null) return state;

        var nodes = tree.GetComponentsInChildren<UI_TreeNode>(true);
        foreach (var n in nodes)
        {
            if (n == null || n.skillData == null) continue;
            var name = n.skillData.name;
            if (n.isUnlocked) state.unlockedSkillNames.Add(name);
            if (n.isLocked) state.lockedSkillNames.Add(name);
        }

        state.combatSkillPoints = tree.GetCombatSkillPoints();
        state.sexSkillPoints = tree.GetSexSkillPoints();
        return state;
    }

    private IEnumerator ApplySkillTreeWhenReady(SkillTreeState state)
    {
        UI_SkillTree tree = null;
        const float timeout = 3f;
        float t = 0f;

        while (tree == null && t < timeout)
        {
            tree = FindFirstObjectByType<UI_SkillTree>(UnityEngine.FindObjectsInactive.Include);
            if (tree != null) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (tree != null)
        {
            yield return null; // ensure Start ran
            tree.ApplySaveState(state);
            tree.UpdateAllConnections();

            yield return null; // let UI settle

            var ui = FindFirstObjectByType<UI>(UnityEngine.FindObjectsInactive.Include);
            ui?.inGameUI?.RefreshSkillSlotsFromTree(tree);
        }
        else
        {
            UI_SkillTree.SetPendingState(state);
        }
    }


    #endregion

    #region Pack/Unpack helpers

    private static void PackList(List<Inventory_Item> src, List<SavedStackItem> dst)
    {
        dst.Clear();
        if (src == null) return;

        foreach (var it in src)
        {
            if (it?.itemData == null) continue;

            dst.Add(new SavedStackItem
            {
                itemName = it.itemData.name,
                stackSize = Mathf.Max(1, it.stackSize),
                modifiers = PackMods(it.GetInstanceModifiers())
            });
        }
    }

    private static void UnpackList(List<SavedStackItem> src, List<Inventory_Item> dst)
    {
        dst.Clear();
        if (src == null) return;

        foreach (var saved in src)
        {
            if (string.IsNullOrEmpty(saved.itemName)) continue;

            if (!itemLookup.TryGetValue(saved.itemName, out var so) || so == null)
            {
                Debug.LogWarning($"[GameDataSaver] ItemData not found for name: {saved.itemName}. Is it under Resources/Items/** ?");
                continue;
            }

            var newItem = new Inventory_Item(so);

            // Restore per-instance modifiers
            if (saved.modifiers != null && saved.modifiers.Count > 0)
                newItem.SetInstanceModifiers(UnpackMods(saved.modifiers));

            // Restore stack
            if (saved.stackSize > 1)
                newItem.AddStack(saved.stackSize - 1);

            dst.Add(newItem);
        }
    }

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
        if (mods == null || mods.Count == 0) return Array.Empty<ItemModifier>();
        var arr = new ItemModifier[mods.Count];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = new ItemModifier { statType = mods[i].statType, value = mods[i].value };
        return arr;
    }

    #endregion
}
