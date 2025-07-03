using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_StorageSlot : UI_ItemSlot
{

    private Inventory_Storage storage;

    public enum StorageSlotType { StorageSlot,PlayerInventorySlot}
    public StorageSlotType slotType;
    public void SetStorage(Inventory_Storage storage) => this.storage = storage;


    public override void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null)
            return;

        bool transferFullStack = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl); // Check if Shift key is held down for full stack transfer

        if (slotType == StorageSlotType.StorageSlot)
            storage.FromStorageToPlayer(itemInSlot,transferFullStack); // Move item from storage to player inventory

        if (slotType == StorageSlotType.PlayerInventorySlot)
            storage.FromPlayerToStorage(itemInSlot, transferFullStack); // Move item from player inventory to storage

        ui.itemToolTip.ShowToolTip(false, null); // Hide the tooltip after moving the item
    }
}
