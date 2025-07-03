using UnityEngine;
using System.Collections.Generic;

public class UI_EquipmentInventory : MonoBehaviour
{
    [Header("Inventory References")]
    [SerializeField] private Inventory_Equipment equipmentInventory; // Bag for unequipped
    [SerializeField] private Inventory_Player playerInventory;       // So we can see equipped slots

    [Header("Unequipped Slots")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private UI_EquipmentSlot slotPrefab;

    [Header("Equipped Slots Parent")]
    [SerializeField] private UI_EquipSlotParent equippedSlotsPanel; // ✅ NEW: add your equipped slots group here!

    [Header("UI Toggle Root")]
    [SerializeField] private GameObject panelRoot;

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

        equipmentInventory.OnInventoryChange += UpdateUI;
        playerInventory.OnInventoryChange += UpdateUI;

        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("[UI_EquipmentInventory] SlotContainer or SlotPrefab missing.");
            return;
        }

        uiSlots.AddRange(slotContainer.GetComponentsInChildren<UI_EquipmentSlot>(true));
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

        // ✅ Update unequipped slots
        var items = equipmentInventory.itemList;

        if (items.Count > uiSlots.Count)
        {
            int toAdd = items.Count - uiSlots.Count;
            Debug.Log($"[UI_EquipmentInventory] Adding {toAdd} new slots dynamically.");
            for (int i = 0; i < toAdd; i++)
                uiSlots.Add(Instantiate(slotPrefab, slotContainer));
        }

        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < items.Count)
                uiSlots[i].UpdateSlot(items[i]);
            else
                uiSlots[i].Clear();
        }

        // ✅ Update equipped slots too!
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
