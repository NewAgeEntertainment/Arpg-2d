using System.Collections.Generic;
using UnityEngine;

public class UI_Merchant : MonoBehaviour
{
    private Inventory_Player playerInventory;
    private Inventory_Merchant merchant;

    [SerializeField] private UI_ItemSlotParent merchantSlots;
    [SerializeField] private UI_ItemSlotParent playerSlots;
    [SerializeField] private UI_EquipSlotParent equippedSlotsParent;

    public void SetUpMerchantUI(Inventory_Merchant merchant, Inventory_Player playerInventory)
    {
        this.merchant = merchant;
        this.playerInventory = playerInventory;

        merchant.SetInventory(playerInventory); // ✅ link player ref to merchant

        playerInventory.OnInventoryChange += UpdateSlotUI;
        playerInventory.equipmentInventory.OnInventoryChange += UpdateSlotUI;
        merchant.OnInventoryChange += UpdateSlotUI;

        foreach (var slot in GetComponentsInChildren<UI_MerchantSlot>(true))
        {
            slot.SetUpMerchantUI(merchant, playerInventory);
        }

        UpdateSlotUI();
    }

    private void OnDisable()
    {
        if (playerInventory != null) playerInventory.OnInventoryChange -= UpdateSlotUI;
        if (playerInventory?.equipmentInventory != null) playerInventory.equipmentInventory.OnInventoryChange -= UpdateSlotUI;
        if (merchant != null) merchant.OnInventoryChange -= UpdateSlotUI;
    }

    private void UpdateSlotUI()
    {
        if (playerInventory == null || merchant == null) return;

        var combined = new List<Inventory_Item>();
        combined.AddRange(playerInventory.itemList);
        combined.AddRange(playerInventory.equipmentInventory.itemList);

        foreach (var slot in playerInventory.equipList)
        {
            if (slot.HasItem())
                combined.Add(slot.equipedItem);
        }

        playerSlots.UpdateSlots(combined);
        merchantSlots.UpdateSlots(merchant.itemList);

        if (equippedSlotsParent != null)
            equippedSlotsParent.UpdateEquipmentSlots(playerInventory.equipList);

        Debug.Log($"[UI_Merchant] PlayerItems={combined.Count} | MerchantItems={merchant.itemList.Count}");
    }
}
