using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Base : MonoBehaviour
{
    public event Action OnInventoryChange;

    public int maxInventorySize = 10;
    public List<Inventory_Item> itemList = new List<Inventory_Item>();

    protected virtual void Awake() { }

    public void NotifyInventoryChanged()
    {
        OnInventoryChange?.Invoke();
    }

    public virtual void AddItem(Inventory_Item itemToAdd)
    {
        Inventory_Item existing = FindStackable(itemToAdd);
        if (existing != null)
            existing.AddStack();
        else
            itemList.Add(itemToAdd);

        NotifyInventoryChanged();
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
        return FindStackable(itemToAdd) != null || itemList.Count < maxInventorySize;
    }

    public Inventory_Item FindItem(ItemDataSO itemData)
    {
        return itemList.Find(item => item.itemData == itemData);
    }

    public void TryUseItem(Inventory_Item itemToUse)
    {
        if (itemToUse == null || !itemList.Contains(itemToUse)) return;

        if (itemToUse.itemEffect != null)
        {
            itemToUse.itemEffect.ExecuteEffect();

            if (itemToUse.stackSize > 1)
                itemToUse.RemoveStack();
            else
                RemoveOneItem(itemToUse);

            NotifyInventoryChanged();
        }
    }
}