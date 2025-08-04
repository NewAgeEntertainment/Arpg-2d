using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Base : MonoBehaviour
{
    protected Player player;
    public event Action OnInventoryChange;

    public int maxInventorySize = 10;
    public List<Inventory_Item> itemList = new List<Inventory_Item>();

    protected virtual void Awake() 
    {
        player = GetComponent<Player>();
    }

    public void NotifyInventoryChanged()
    {
        OnInventoryChange?.Invoke();
    }

    public virtual bool AddItem(Inventory_Item itemToAdd)
    {
        if (itemToAdd == null || itemToAdd.itemData == null)
        {
            Debug.LogWarning("[Inventory_Base] Tried to add null item.");
            return false;
        }

        Inventory_Item existing = FindStackable(itemToAdd);

        if (existing != null)
        {
            existing.AddStack();
        }
        else
        {
            if (itemList.Count >= maxInventorySize)
            {
                Debug.LogWarning("[Inventory_Base] Inventory full, cannot add item.");
                return false;
            }

            itemList.Add(itemToAdd);
        }

        NotifyInventoryChanged();
        return true;
    }


    public virtual void RemoveOneItem(Inventory_Item itemToRemove)
    {
        Inventory_Item found = itemList.Find(item => item == itemToRemove);
        if (found != null)
        {
            if (found.stackSize > 1)
                found.RemoveStack();
            else
                itemList.Remove(found);

            NotifyInventoryChanged();
        }
    }

    public Inventory_Item FindStackable(Inventory_Item item)
    {
        return itemList.Find(i => i.itemData == item.itemData && i.CanAddStack());
    }

    public bool CanAddItem(Inventory_Item itemToAdd)
    {
        // If the item is stackable and there’s already a stack, allow it
        if (FindStackable(itemToAdd) != null)
            return true;

        // If the item’s max stack size is greater than 1, treat it as stackable
        if (itemToAdd.itemData.maxStackSize > 1)
            return itemList.Count <= maxInventorySize;

        // If the item is NOT stackable, we need a free slot
        return itemList.Count < maxInventorySize;
    }


    public Inventory_Item FindItem(ItemDataSO itemData)
    {
        return itemList.Find(item => item.itemData == itemData);
    }

    public Inventory_Item FindSameItem(Inventory_Item itemToFind)
    {
        return itemList.Find(item => item.itemData == itemToFind.itemData);
    }

    public void TryUseItem(Inventory_Item itemToUse, Player targetPlayer)
    {
        if (itemToUse == null || !itemList.Contains(itemToUse)) return;

        if (itemToUse.itemEffect != null)
        {
            if (!itemToUse.itemEffect.CanBeUsed(targetPlayer)) return;

            itemToUse.itemEffect.ExecuteEffect(targetPlayer); // ✅ Use on the selected player

            if (itemToUse.stackSize > 1)
                itemToUse.RemoveStack();
            else
                RemoveOneItem(itemToUse);

            NotifyInventoryChanged();
        }
    }
    public void TriggerUpdateUI() => OnInventoryChange?.Invoke();

}