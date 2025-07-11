using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UI_Inventory : MonoBehaviour
{
    [SerializeField] private Inventory_Player inventory;
    [SerializeField] private UI_ItemSlotParent backpackSlotsParent;
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Panels")]
    [SerializeField] private GameObject buttonSelectionPanel;
    [SerializeField] private GameObject itemsPanel;

    private ItemType? currentFilter = null;
    private bool isOpen = false;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>();

        inventory.OnInventoryChange += UpdateUI;
        inventory.equipmentInventory.OnInventoryChange += UpdateUI;

        CloseInventory();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChange -= UpdateUI;

        if (inventory?.equipmentInventory != null)
            inventory.equipmentInventory.OnInventoryChange -= UpdateUI;
    }

    public void OpenInventory()
    {
        isOpen = true;
        gameObject.SetActive(true);

        // ✅ Always show selection buttons by default
        buttonSelectionPanel?.SetActive(true);
        itemsPanel?.SetActive(false);

        UpdateUI();
        Debug.Log("[UI_Inventory] Inventory OPENED — showing selection panel.");
    }

    public void CloseInventory()
    {
        isOpen = false;
        gameObject.SetActive(false);
    }

    public void ToggleInventory()
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    public void SetFilter(string filterName)
    {
        switch (filterName)
        {
            case "All": currentFilter = null; break;
            case "Items": currentFilter = ItemType.Consumable; break;
            case "Materials": currentFilter = ItemType.Material; break;
            case "Weapons": currentFilter = ItemType.Weapon; break;
            case "Armor": currentFilter = ItemType.Armor; break;
            case "Trinkets": currentFilter = ItemType.trinket; break;
            case "KeyItems": currentFilter = ItemType.Key; break;
            default: currentFilter = null; break;
        }

        // ✅ Hide selection buttons, show items grid
        buttonSelectionPanel?.SetActive(false);
        itemsPanel?.SetActive(true);

        UpdateUI();
        Debug.Log($"[UI_Inventory] Filter set: {filterName} → showing Items panel.");
    }

    public void OnSearchInputChanged() => UpdateUI();

    public void UpdateUI()
    {
        if (!isOpen) return;

        goldText.text = inventory.gold.ToString("N0") + "g.";

        List<Inventory_Item> combined = new();
        combined.AddRange(inventory.itemList);
        combined.AddRange(inventory.equipmentInventory.itemList);
        combined.AddRange(inventory.storage.materialStash);

        List<Inventory_Item> filtered = new();

        foreach (var item in combined)
        {
            if (currentFilter.HasValue)
            {
                if (item.itemData.itemType == currentFilter.Value)
                    filtered.Add(item);
            }
            else
            {
                filtered.Add(item);
            }
        }

        if (searchField != null && !string.IsNullOrEmpty(searchField.text))
        {
            string query = searchField.text.ToLower();
            filtered = filtered.FindAll(i => i.itemData.itemName.ToLower().Contains(query));
        }

        backpackSlotsParent.UpdateSlots(filtered);
    }
}
