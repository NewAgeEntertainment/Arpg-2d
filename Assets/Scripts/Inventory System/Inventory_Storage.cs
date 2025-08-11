using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Storage : Inventory_Base
{
    public Inventory_Player playerInventory { get; private set; }

    // Materials kept separately from normal storage items.
    public List<Inventory_Item> materialStash = new List<Inventory_Item>();

    // 🔸 let UIs subscribe to storage changes
    public event Action OnInventoryChange;

    private void Awake()
    {
        // ensure we have a player inventory (can still be set explicitly via SetInventory)
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<Inventory_Player>();
    }

    public void SetInventory(Inventory_Player inventory) => playerInventory = inventory;

    // 🔔 call this anytime item lists change (including after load)
    protected new void NotifyInventoryChanged()
    {
        // call base if it exists
        try { base.NotifyInventoryChanged(); } catch { /* base may be non-virtual/private */ }

        OnInventoryChange?.Invoke();           // -> UI_Storage
        playerInventory?.TriggerUpdateUI();    // -> UI_Inventory (list shows materials too)
    }

    // =========================
    // Crafting
    // =========================

    public void CraftItem(Inventory_Item itemToCraft)
    {
        ConsumedMaterials(itemToCraft);
        playerInventory?.AddItem(itemToCraft);
        Debug.Log($"[Craft] Added {itemToCraft.itemData.itemName} to Backpack");
        NotifyInventoryChanged();
    }

    public bool CanCraftItem(Inventory_Item itemToCraft)
    {
        return HasEnoughMatrials(itemToCraft) &&
               playerInventory != null && playerInventory.CanAddItem(itemToCraft) &&
               itemList.Count < maxInventorySize;
    }

    public void ConsumedMaterials(Inventory_Item itemToCraft)
    {
        foreach (var requiredMaterial in itemToCraft.itemData.craftRecipe)
        {
            int amountToConsume = requiredMaterial.stackSize;

            amountToConsume -= ConsumedMaterialsAmount(playerInventory?.itemList, requiredMaterial);

            if (amountToConsume > 0)
                amountToConsume -= ConsumedMaterialsAmount(itemList, requiredMaterial);

            if (amountToConsume > 0)
                amountToConsume -= ConsumedMaterialsAmount(materialStash, requiredMaterial);
        }

        NotifyInventoryChanged();
    }

    private int ConsumedMaterialsAmount(List<Inventory_Item> list, Inventory_Item neededItem)
    {
        if (list == null) return 0;

        int amountNeeded = neededItem.stackSize;
        int consumedAmount = 0;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            var item = list[i];
            if (item.itemData != neededItem.itemData) continue;

            int removeAmount = Mathf.Min(item.stackSize, amountNeeded - consumedAmount);
            item.stackSize -= removeAmount;
            consumedAmount += removeAmount;

            if (item.stackSize <= 0)
                list.RemoveAt(i);

            if (consumedAmount >= amountNeeded)
                break;
        }

        return consumedAmount;
    }

    private bool HasEnoughMatrials(Inventory_Item itemToCraft)
    {
        foreach (var requiredMaterial in itemToCraft.itemData.craftRecipe)
        {
            if (GetAvailableAmountOf(requiredMaterial.itemData) < requiredMaterial.stackSize)
                return false;
        }
        return true;
    }

    public int GetAvailableAmountOf(ItemDataSO requiredItem)
    {
        int amount = 0;

        if (playerInventory != null)
            foreach (var item in playerInventory.itemList)
                if (item.itemData == requiredItem) amount += item.stackSize;

        foreach (var item in itemList)
            if (item.itemData == requiredItem) amount += item.stackSize;

        foreach (var item in materialStash)
            if (item.itemData == requiredItem) amount += item.stackSize;

        return amount;
    }

    // =========================
    // Materials API
    // =========================

    public bool AddMaterialToStash(Inventory_Item item)
    {
        if (item == null || item.itemData == null) return false;

        // Merge into an existing stack if possible
        foreach (var existing in materialStash)
        {
            if (existing.itemData == item.itemData && existing.CanAddStack())
            {
                existing.stackSize = Mathf.Min(existing.stackSize + item.stackSize, existing.itemData.maxStackSize);
                NotifyInventoryChanged();
                return true;
            }
        }

        // Add as new stack (copy data; materials usually don’t carry per-instance mods)
        materialStash.Add(new Inventory_Item(item.itemData) { stackSize = item.stackSize });
        NotifyInventoryChanged();
        return true;
    }

    public override void RemoveOneItem(Inventory_Item itemToRemove)
    {
        base.RemoveOneItem(itemToRemove);

        // Also support removal from materials list
        var found = materialStash.Find(i => i == itemToRemove);
        if (found != null)
        {
            if (found.stackSize > 1) found.RemoveStack();
            else materialStash.Remove(found);

            NotifyInventoryChanged();
        }
    }

    public Inventory_Item StackableInStash(Inventory_Item itemToAdd)
    {
        foreach (var stackableItem in materialStash)
        {
            if (stackableItem.itemData == itemToAdd.itemData && stackableItem.CanAddStack())
                return stackableItem;
        }
        return null;
    }

    // =========================
    // Transfers
    // =========================

    public void FromPlayerToStorage(Inventory_Item item, bool transferFullStack)
    {
        if (item == null || item.itemData == null || playerInventory == null) return;

        int transferAmount = transferFullStack ? item.stackSize : 1;

        for (int i = 0; i < transferAmount; i++)
        {
            if (item.itemData.itemType == ItemType.Material)
            {
                var itemToAdd = new Inventory_Item(item.itemData);
                playerInventory.RemoveOneItem(item);
                AddMaterialToStash(itemToAdd);
            }
            else if (item.itemData.itemType == ItemType.Weapon ||
                     item.itemData.itemType == ItemType.Armor ||
                     item.itemData.itemType == ItemType.trinket)
            {
                if (CanAddItem(item))
                {
                    var itemToAdd = new Inventory_Item(item.itemData);
                    playerInventory.equipmentInventory.RemoveOneItem(item);
                    AddItem(itemToAdd);
                }
                else
                {
                    Debug.LogWarning("No space in storage for this equipment item.");
                    break;
                }
            }
            else
            {
                if (CanAddItem(item))
                {
                    var itemToAdd = new Inventory_Item(item.itemData);
                    playerInventory.RemoveOneItem(item);
                    AddItem(itemToAdd);
                }
                else
                {
                    Debug.LogWarning("No space in storage for this item.");
                    break;
                }
            }
        }

        NotifyInventoryChanged();
    }

    public void FromStorageToPlayer(Inventory_Item item, bool transferFullStack)
    {
        if (item == null || item.itemData == null || playerInventory == null) return;

        int transferAmount = transferFullStack ? item.stackSize : 1;

        for (int i = 0; i < transferAmount; i++)
        {
            if (playerInventory.CanAddItem(item))
            {
                var itemToAdd = new Inventory_Item(item.itemData);
                RemoveOneItem(item);
                playerInventory.AddItem(itemToAdd);
            }
            else if (item.itemData.itemType == ItemType.Weapon ||
                     item.itemData.itemType == ItemType.Armor ||
                     item.itemData.itemType == ItemType.trinket)
            {
                if (playerInventory.equipmentInventory.CanAddItem(item))
                {
                    var itemToAdd = new Inventory_Item(item.itemData);
                    RemoveOneItem(item);
                    playerInventory.equipmentInventory.AddItem(itemToAdd);
                }
                else
                {
                    Debug.LogWarning("No space in equipment inventory for this equipment item.");
                    break;
                }
            }
            else
            {
                Debug.LogWarning("No space in player inventory for this item.");
                break;
            }
        }

        NotifyInventoryChanged();
    }
}
