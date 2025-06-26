using System.Collections.Generic;
using UnityEngine;

public class UI_EquipmentInventory : MonoBehaviour
{
    [SerializeField] private Inventory_Equipment equipmentInventory;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private UI_EquipmentSlot slotPrefab;

    private List<UI_EquipmentSlot> uiSlots = new List<UI_EquipmentSlot>();

    private void Start()
    {
        equipmentInventory.OnInventoryChange += UpdateUI;
        InitializeSlots();
        UpdateUI();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < equipmentInventory.maxInventorySize; i++)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            uiSlots.Add(slot);
        }
    }

    private void UpdateUI()
    {
        var items = equipmentInventory.itemList;

        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < items.Count)
                uiSlots[i].SetItem(items[i]);
            else
                uiSlots[i].Clear();
        }
    }

    private void OnDestroy()
    {
        equipmentInventory.OnInventoryChange -= UpdateUI;
    }
}

