using UnityEngine;
using System.Collections.Generic;

public class UI_EquipmentInventory : MonoBehaviour
{
    [Header("Inventory References")]
    [SerializeField] private Inventory_Equipment equipmentInventory;
    [SerializeField] private Inventory_Player playerInventory;

    [Header("Unequipped Slots")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private UI_EquipmentSlot slotPrefab;

    [Header("Equipped Slots Parent")]
    [SerializeField] private UI_EquipSlotParent equippedSlotsPanel;

    [Header("UI Toggle Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Dedicated Equipment ToolTip")]
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;

    private List<UI_EquipmentSlot> uiSlots = new List<UI_EquipmentSlot>();
    private bool isVisible = false;

    private void Awake()
    {
        if (equipmentInventory == null)
            equipmentInventory = FindFirstObjectByType<Inventory_Equipment>();

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<Inventory_Player>();

        if (equipmentInventory == null || playerInventory == null)
        {
            Debug.LogError("[UI_EquipmentInventory] Missing Inventory references.");
            return;
        }

        if (equipmentToolTip == null)
        {
            Debug.LogError("[UI_EquipmentInventory] Missing EquipmentToolTip reference!");
        }

        equipmentInventory.OnInventoryChange += UpdateUI;
        playerInventory.OnInventoryChange += UpdateUI;

        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("[UI_EquipmentInventory] SlotContainer or SlotPrefab missing.");
            return;
        }

        uiSlots.AddRange(slotContainer.GetComponentsInChildren<UI_EquipmentSlot>(true));
        foreach (var slot in uiSlots)
        {
            slot.SetEquipmentToolTip(equipmentToolTip);
        }

        Debug.Log($"[UI_EquipmentInventory] Initialized with {uiSlots.Count} slots.");

        if (panelRoot != null) panelRoot.SetActive(isVisible);
        else gameObject.SetActive(isVisible);

        UpdateUI();
    }

    public void Toggle()
    {
        isVisible = !isVisible;

        Debug.Log($"[UI_EquipmentInventory] Toggled → {(isVisible ? "Open" : "Closed")}");

        if (isVisible) UpdateUI();

        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
        else
            gameObject.SetActive(isVisible);
    }

    public void UpdateUI()
    {
        Debug.Log("[UI_EquipmentInventory] UpdateUI()");

        var items = equipmentInventory.itemList;

        if (items.Count > uiSlots.Count)
        {
            int toAdd = items.Count - uiSlots.Count;
            Debug.Log($"[UI_EquipmentInventory] Adding {toAdd} new slots dynamically.");
            for (int i = 0; i < toAdd; i++)
            {
                UI_EquipmentSlot newSlot = Instantiate(slotPrefab, slotContainer);
                newSlot.SetEquipmentToolTip(equipmentToolTip);
                uiSlots.Add(newSlot);
            }
        }

        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < items.Count)
                uiSlots[i].UpdateSlot(items[i]);
            else
                uiSlots[i].Clear();
        }

        if (equippedSlotsPanel != null)
            equippedSlotsPanel.UpdateEquipmentSlots(playerInventory.equipList);
    }

    private void OnDestroy()
    {
        if (equipmentInventory != null)
            equipmentInventory.OnInventoryChange -= UpdateUI;

        if (playerInventory != null)
            playerInventory.OnInventoryChange -= UpdateUI;
    }
}
