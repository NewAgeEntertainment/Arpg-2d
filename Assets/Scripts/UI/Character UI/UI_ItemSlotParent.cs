using UnityEngine;
using System;
using System.Collections.Generic;

public class UI_ItemSlotParent : MonoBehaviour
{
    [SerializeField] private UI_ItemSlot[] slots;

    public event Action<Inventory_Item> OnSlotSubmit;

    private void Awake()
    {
        slots = GetComponentsInChildren<UI_ItemSlot>(true);

        foreach (var slot in slots)
        {
            // in Awake():
            slot.OnSlotSubmit += HandleSlotSubmit;
            slot.OnSlotRightClick += HandleSlotRightClick;

        }
    }

    private void HandleSlotSubmit(Inventory_Item item)
    {
        OnSlotSubmit?.Invoke(item);
    }

    private void HandleSlotRightClick(Inventory_Item item)
    {
        OnSlotSubmit?.Invoke(item);
    }

    public void UpdateSlots(List<Inventory_Item> items)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < items.Count)
                slots[i].UpdateSlot(items[i]);
            else
                slots[i].Clear();
        }
    }

    public bool TryGetSelectedItem(out Inventory_Item item)
    {
        foreach (var slot in slots)
        {
            if (slot.IsSelected())
            {
                item = slot.itemInSlot;
                return item != null;
            }
        }
        item = null;
        return false;
    }
}
