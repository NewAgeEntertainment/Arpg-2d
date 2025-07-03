using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    [SerializeField] private Inventory_Player inventory;
    [Header("Backpack")]
    [SerializeField] private UI_ItemSlotParent backpackSlotsParent;  // UI_ItemPack
    [Header("Equipment Panel")]
    [SerializeField] public UI_EquipmentInventory equipmentInventoryPanel; // shows unequipped gear
    [SerializeField] private UI_EquippedSlot[] equippedSlots;  // Equipped slots on character
    [SerializeField] private UI_EquipSlotParent equippedSlotsParent; // Optional: for updating equipped slots

    private void Awake()
    {
        inventory = FindFirstObjectByType<Inventory_Player>();
        inventory.OnInventoryChange += UpdateUI;
        

        UpdateUI();

       
    }

    

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChange -= UpdateUI;
            inventory.equipmentInventory.OnInventoryChange -= UpdateUI;
        }
    }

    public void UpdateUI()
    {

        equippedSlotsParent.UpdateEquipmentSlots(inventory.equipList);
        if (backpackSlotsParent != null)
        {
            Debug.Log("[UI_Inventory] Updating backpack slots with: " + inventory.itemList.Count);
            backpackSlotsParent.UpdateSlots(inventory.itemList);
        }

        if (equipmentInventoryPanel != null)
            equipmentInventoryPanel.UpdateUI();

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (i >= inventory.equipList.Count) continue;
            var slot = inventory.equipList[i];
            equippedSlots[i].UpdateSlot(slot.HasItem() ? slot.equipedItem : null);
        }
       
        Debug.Log($"Backpack items: {inventory.itemList.Count}");
        for (int i = 0; i < inventory.itemList.Count; i++)
        {
            Debug.Log($"Item {i}: {inventory.itemList[i].itemData.itemName}");
        }

    }
}
