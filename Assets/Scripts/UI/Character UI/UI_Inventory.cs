using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UI_Inventory : MonoBehaviour
{
    [Header("Inventory Reference")]
    [SerializeField] private Inventory_Player inventory;

    [Header("Backpack Slots Parent")]
    [SerializeField] private UI_ItemSlotParent backpackSlotsParent;

    [Header("Equipped Slots")]
    [SerializeField] private UI_EquippedSlot[] equippedSlots;
    [SerializeField] private UI_EquipSlotParent equippedSlotsParent;

    [Header("Optional: Equipment Panel")]
    [SerializeField] private UI_EquipmentInventory equipmentInventoryPanel;

    [Header("Search Bar (Optional)")]
    [SerializeField] private TMP_InputField searchField;

    [Header("Tab Highlights")]
    [SerializeField] private List<Image> tabHighlights; // One Image per tab (child highlight image)

    private bool isOpen = false;
    private ItemType? currentFilter = null;

    private void Awake()
    {
        inventory = FindFirstObjectByType<Inventory_Player>();
        inventory.OnInventoryChange += UpdateUI;

        CloseInventory();
    }


    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChange -= UpdateUI;
    }

    public void ToggleInventory()
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        isOpen = true;
        gameObject.SetActive(true);
        UpdateUI();
    }

    public void CloseInventory()
    {
        isOpen = false;
        gameObject.SetActive(false);
    }


    public void SetFilter(string filterName)
    {
        switch (filterName)
        {
            case "All":
                currentFilter = null;
                break;
            case "Items":
                currentFilter = null; // Special: shows Materials & Consumables
                break;
            case "Weapons":
                currentFilter = ItemType.Weapon;
                break;
            case "Armor":
                currentFilter = ItemType.Armor;
                break;
            case "Trinkets":
                currentFilter = ItemType.trinket;
                break;
        }

        if (!isOpen)
            OpenInventory();

        UpdateUI();
    }

    private void UpdateTabHighlights(string filterName)
    {
        for (int i = 0; i < tabHighlights.Count; i++)
        {
            bool shouldHighlight = false;

            switch (filterName)
            {
                case "All": shouldHighlight = (i == 0); break;
                case "Items": shouldHighlight = (i == 1); break;
                case "Weapons": shouldHighlight = (i == 2); break;
                case "Armor": shouldHighlight = (i == 3); break;
                case "Trinkets": shouldHighlight = (i == 4); break;
            }

            tabHighlights[i].color = shouldHighlight ? Color.yellow : Color.white;
        }
    }

    public void OnSearchInputChanged()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (!isOpen) return;

        List<Inventory_Item> filtered = new List<Inventory_Item>();

        foreach (var item in inventory.itemList)
        {
            bool matchesFilter = true;

            if (currentFilter.HasValue)
            {
                matchesFilter = item.itemData.itemType == currentFilter.Value;
            }
            else
            {
                // "Items" shows Materials + Consumables
                matchesFilter = (item.itemData.itemType == ItemType.Consumable ||
                                 item.itemData.itemType == ItemType.Material);
            }

            if (matchesFilter)
                filtered.Add(item);
        }

        // Optional search filter
        if (searchField != null && !string.IsNullOrEmpty(searchField.text))
        {
            string query = searchField.text.ToLower();
            filtered = filtered.FindAll(item =>
                item.itemData.itemName.ToLower().Contains(query));
        }

        if (backpackSlotsParent != null)
            backpackSlotsParent.UpdateSlots(filtered);

        if (equippedSlotsParent != null)
            equippedSlotsParent.UpdateEquipmentSlots(inventory.equipList);

        if (equipmentInventoryPanel != null)
            equipmentInventoryPanel.UpdateUI();
    }
}
