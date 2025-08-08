using PixelCrushers;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Saves and loads the player's inventory (backpack) and gold.
/// </summary>
public class InventorySaver : Saver
{
    private Inventory_Player inventory;

    // Add all your folders here
    private readonly string[] itemFolders = new string[]
    {
        "Items/Consumables",
        "Items/Weapons",
        "Items/Armor",
        "Items/Materials"
    };

    [System.Serializable]
    public class InventorySaveData
    {
        public List<SavedItem> items = new List<SavedItem>();
        public int gold;
    }

    [System.Serializable]
    public class SavedItem
    {
        public string itemName;
        public int stackSize;
    }

    private void Awake()
    {
        inventory = GetComponent<Inventory_Player>();
    }

    public override string RecordData()
    {
        if (inventory == null) return string.Empty;

        InventorySaveData data = new InventorySaveData();

        foreach (var item in inventory.itemList)
        {
            if (item.itemData == null) continue;

            data.items.Add(new SavedItem
            {
                itemName = item.itemData.name,
                stackSize = item.stackSize
            });
        }

        data.gold = inventory.gold;

        return SaveSystem.Serialize(data);
    }

    public override void ApplyData(string s)
    {
        StartCoroutine(DelayedApply(s));
    }

    private IEnumerator DelayedApply(string s)
    {
        yield return null; // Ensure Inventory_Player is initialized

        var data = SaveSystem.Deserialize<InventorySaveData>(s);
        if (data == null || inventory == null) yield break;

        inventory.itemList.Clear();

        foreach (var savedItem in data.items)
        {
            ItemDataSO itemData = null;

            foreach (var folder in itemFolders)
            {
                itemData = Resources.Load<ItemDataSO>($"{folder}/{savedItem.itemName}");
                if (itemData != null)
                    break;
            }

            if (itemData != null)
            {
                var newItem = new Inventory_Item(itemData);
                if (savedItem.stackSize > 1)
                    newItem.AddStack(savedItem.stackSize - 1);

                inventory.itemList.Add(newItem);
            }
            else
            {
                Debug.LogWarning($"[InventorySaver] Item not found in any folder: {savedItem.itemName}");
            }
        }

        inventory.gold = data.gold;
        inventory.NotifyInventoryChanged();
    }
}
