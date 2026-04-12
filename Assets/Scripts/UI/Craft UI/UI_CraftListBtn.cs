using System.Collections.Generic;
using UnityEngine;

public class UI_CraftListBtn : MonoBehaviour
{
    public enum CraftFilterType
    {
        All,
        Weapon,
        Armor,
        Trinket,
        Consumable
    }

    [SerializeField] private ItemListDataSO craftData;
    [SerializeField] private CraftFilterType filterType = CraftFilterType.All;

    private UI_CraftSlot[] craftSlots;

    public void setCraftSlots(UI_CraftSlot[] craftSlots) => this.craftSlots = craftSlots;

    public void UpdateCraftSlots()
    {
        if (craftSlots == null)
        {
            Debug.LogWarning("Craft slots not set. Please set the craft slots before updating.");
            return;
        }

        if (craftData == null || craftData.itemList == null)
        {
            Debug.LogWarning("Craft data is missing.");
            return;
        }

        foreach (var slot in craftSlots)
            slot.gameObject.SetActive(false);

        List<ItemDataSO> filteredItems = GetFilteredAndSortedItems();

        for (int i = 0; i < filteredItems.Count && i < craftSlots.Length; i++)
        {
            craftSlots[i].gameObject.SetActive(true);
            craftSlots[i].SetupButton(filteredItems[i]);
        }
    }

    private List<ItemDataSO> GetFilteredAndSortedItems()
    {
        List<ItemDataSO> results = new List<ItemDataSO>();

        foreach (var item in craftData.itemList)
        {
            if (item == null)
                continue;

            if (MatchesFilter(item.itemType))
                results.Add(item);
        }

        results.Sort((a, b) =>
        {
            int orderA = GetSortOrder(a.itemType);
            int orderB = GetSortOrder(b.itemType);

            if (orderA != orderB)
                return orderA.CompareTo(orderB);

            return string.Compare(a.itemName, b.itemName, System.StringComparison.OrdinalIgnoreCase);
        });

        return results;
    }

    private bool MatchesFilter(ItemType itemType)
    {
        switch (filterType)
        {
            case CraftFilterType.Weapon:
                return itemType == ItemType.Weapon;

            case CraftFilterType.Armor:
                return itemType == ItemType.Armor;

            case CraftFilterType.Trinket:
                return itemType == ItemType.trinket;

            case CraftFilterType.Consumable:
                return itemType == ItemType.Consumable;

            case CraftFilterType.All:
            default:
                return itemType == ItemType.Weapon ||
                       itemType == ItemType.Armor ||
                       itemType == ItemType.trinket ||
                       itemType == ItemType.Consumable;
        }
    }

    private int GetSortOrder(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon: return 0;
            case ItemType.Armor: return 1;
            case ItemType.trinket: return 2;
            case ItemType.Consumable: return 3;
            default: return 99;
        }
    }
}