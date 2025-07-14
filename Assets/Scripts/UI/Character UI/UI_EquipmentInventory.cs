using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;

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

    private bool isOpen = false;
    public bool IsOpen => isOpen;
    public UI_EquipSlotParent EquippedSlotsPanel => equippedSlotsPanel;

    private bool selectionEnabled = false;
    private bool inSelectionMode = false;
    private ItemType? currentFilter = null;

    private void Awake()
    {
        equipmentInventory = FindFirstObjectByType<Inventory_Equipment>();
        playerInventory = FindFirstObjectByType<Inventory_Player>();

        equipmentInventory.OnInventoryChange += UpdateUI;
        playerInventory.OnInventoryChange += UpdateUI;

        uiSlots.AddRange(slotContainer.GetComponentsInChildren<UI_EquipmentSlot>(true));
        foreach (var slot in uiSlots)
        {
            slot.SetEquipmentToolTip(equipmentToolTip);
        }

        Close();
    }

    private void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inSelectionMode)
                ExitSelectionMode();
            else
                Close();
        }
    }

    public void Open()
    {
        isOpen = true;
        panelRoot?.SetActive(true);
        UpdateUI();
    }

    public void Close()
    {
        isOpen = false;
        panelRoot?.SetActive(false);
        equipmentToolTip?.ShowEquipmentToolTip(false, null);
    }

    public void UpdateUI()
    {
        if (!isOpen) return;

        var items = equipmentInventory.itemList;
        List<Inventory_Item> filteredItems = new List<Inventory_Item>();

        foreach (var item in items)
        {
            if (currentFilter == null || item.itemData.itemType == currentFilter)
                filteredItems.Add(item);
        }

        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < filteredItems.Count)
                uiSlots[i].UpdateSlot(filteredItems[i]);
            else
                uiSlots[i].Clear();
        }

        equippedSlotsPanel?.UpdateEquipmentSlots(playerInventory.equipList);
    }

    public void EnterSelectionMode(ItemType slotType)
    {
        inSelectionMode = true;
        FilterBySlotType(slotType);
        Debug.Log($"[EquipmentInventory] Entered selection mode for slot type: {slotType}");
    }

    public void SwapEquippedItem(Inventory_Item newItem)
    {
        if (newItem == null)
        {
            Debug.LogWarning("[EquipmentInventory] Attempted to swap with null item.");
            return;
        }

        playerInventory.TryEquipFromEquipmentInventory(newItem);
        Debug.Log($"[EquipmentInventory] Equipped new item: {newItem.itemData.itemName}");

        ExitSelectionMode();
        UpdateUI();
    }

    public void FilterBySlotType(ItemType slotType)
    {
        currentFilter = slotType;
        UpdateUI();
    }

    public void SetSelectionEnabled(bool enabled)
    {
        selectionEnabled = enabled;
        Debug.Log($"[EquipmentInventory] Selection enabled set to: {enabled}");
    }

    public bool IsSelectionEnabled() => selectionEnabled;

    public bool IsInSelectionMode() => inSelectionMode;

    public void EnableEquipmentSlotSelection(bool enable)
    {
        foreach (var slot in uiSlots)
        {
            slot.SetSelectable(enable);
            Debug.Log("is working");
        }
        Debug.Log($"[EquipmentInventory] Equipment slot selection set to: {enable}");
    }

    public void ExitSelectionMode()
    {
        inSelectionMode = false;

        foreach (var slot in uiSlots)
        {
            slot.SetSelectable(false);
        }
    }
}
