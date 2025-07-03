using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CraftPreview : MonoBehaviour
{
    private Inventory_Item itemToCraft; // The item that is currently being previewed
    private Inventory_Storage storage; // Reference to the storage to check available materials
    private UI_CraftPreviewSlot[] craftPreviewSlots; // Array of material slots for displaying required materials

    [Header("Item Preview Setup")]
    [SerializeField] private Image itemIcon; // Image component to display the item
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemInfo;
    [SerializeField] private TextMeshProUGUI buttonText;

    public void SetupCraftPreview(Inventory_Storage storage)
    {
        this.storage = storage; // Assign the storage reference
        

        craftPreviewSlots = GetComponentsInChildren<UI_CraftPreviewSlot>(); // Get all child slots for displaying materials
        
        foreach (var slot in craftPreviewSlots)
        {
            slot.gameObject.SetActive(false); // Initially hide all slots
        }
    }

    public void ConfirmCraft()
    {
        if (itemToCraft == null)
        {
            buttonText.text = "Pick an item!";
            return;
        }

        if (storage.CanCraftItem(itemToCraft))
        {
            Inventory_Player player = storage.playerInventory;

            bool added = false;

            if (itemToCraft.itemData.itemType == ItemType.Weapon ||
                itemToCraft.itemData.itemType == ItemType.Armor ||
                itemToCraft.itemData.itemType == ItemType.trinket)
            {
                if (player.equipmentInventory.CanAddItem(itemToCraft))
                {
                    storage.ConsumedMaterials(itemToCraft); // Remove mats first
                    player.equipmentInventory.AddItem(itemToCraft);
                    Debug.Log($"[Craft] Added {itemToCraft.itemData.itemName} to Equipment Inventory");
                    added = true;
                }
                else
                {
                    Debug.LogWarning("[Craft] No space in Equipment Inventory!");
                }
            }
            else
            {
                if (player.CanAddItem(itemToCraft))
                {
                    storage.CraftItem(itemToCraft); // Remove mats first
                    added = true;
                }
                else
                {
                    Debug.LogWarning("[Craft] No space in Backpack!");
                }
            }

            if (!added)
            {
                Debug.Log("[Craft] Item could not be added.");
            }
        }

        UpdateCraftPreviewSlots();
    }



    public void UpdateCraftPreview(ItemDataSO itemData)
    {
        itemToCraft = new Inventory_Item(itemData); // Create a new inventory item from the provided item data

        itemIcon.sprite = itemData.itemIcon; // Set the icon of the item
        itemName.text = itemData.itemName; // Set the name of the item
        itemInfo.text = itemToCraft.GetItemInfo(); // Set the item info text

        UpdateCraftPreviewSlots();

    }

    private void UpdateCraftPreviewSlots()
    {
        foreach (var slot in craftPreviewSlots)
        {
            slot.gameObject.SetActive(false); // Hide all slots initially
        }

        for (int i = 0; i < itemToCraft.itemData.craftRecipe.Length; i++)
        {
            Inventory_Item requiredItem = itemToCraft.itemData.craftRecipe[i]; // Get the required item for this slot
            int availableAmount = storage.GetAvailableAmountOf(requiredItem.itemData); // Check how many of this item are available in storage
            int requiredAmount = requiredItem.stackSize; // Get the required amount for crafting

            craftPreviewSlots[i].gameObject.SetActive(true); // Show the slot for this material
            craftPreviewSlots[i].SetupPreviewSlot(requiredItem.itemData, availableAmount, requiredAmount); // Setup the slot with the material data
        }
    }
}
