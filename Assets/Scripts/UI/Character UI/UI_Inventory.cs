using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UI_Inventory : MonoBehaviour
{
    [SerializeField] private Inventory_Player inventory;
    [SerializeField] private UI_ItemSlotParent backpackSlotsParent;
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private TextMeshProUGUI goldText;


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
        UpdateUI();
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
            case "All":
                currentFilter = null;
                break;
            case "Items":
                currentFilter = ItemType.Consumable;
                break;
            case "Materials":
                currentFilter = ItemType.Material;
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
            case "KeyItems": // ✅ new
                currentFilter = ItemType.Key;
                break;
            default:
                currentFilter = null;
                break;
        }

        if (!isOpen)
            OpenInventory();

        UpdateUI();
    }


    public void OnSearchInputChanged() => UpdateUI();

    public void UpdateUI()
    {
        if (!isOpen) return;

        goldText.text = inventory.gold.ToString("N0") + "g.";

        List<Inventory_Item> combined = new List<Inventory_Item>();

        combined.AddRange(inventory.itemList); // real backpack → consumables
        combined.AddRange(inventory.equipmentInventory.itemList); // unequipped gear
        combined.AddRange(inventory.storage.materialStash); // materials live here only

        List<Inventory_Item> filtered = new List<Inventory_Item>();

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
