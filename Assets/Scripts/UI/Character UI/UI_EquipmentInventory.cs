using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Rewired;

public class UI_EquipmentInventory : UI_Panel
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
    [SerializeField] private UI_PlayerStats playerStatsPanel;
    [SerializeField] private Button removeButton;

    [Header("Audio")]
    [SerializeField] private AudioClip equipSound;
    [SerializeField] private AudioClip unequipSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "UICancel";

    private Rewired.Player rPlayer;
    private List<UI_EquipmentSlot> uiSlots = new List<UI_EquipmentSlot>();

    private bool isOpen = false;
    public bool IsOpen => isOpen;
    public UI_EquipSlotParent EquippedSlotsPanel => equippedSlotsPanel;

    private bool selectionEnabled = false;
    private bool inSelectionMode = false;
    private ItemType? currentFilter = null;

    private void Awake()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);
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

        if (rPlayer.GetButtonDown(cancelAction))
        {
            HandleCancel();
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
        currentFilter = slotType;
        FilterBySlotType(slotType);
        if (removeButton != null)
            removeButton.interactable = true;
    }

    public void ExitSelectionMode()
    {
        inSelectionMode = false;
        if (removeButton != null)
            removeButton.interactable = false;

        foreach (var slot in uiSlots)
        {
            slot.SetSelectable(false);
        }

        ResetAllHighlights();
    }

    public void RemoveCurrentlyEquipped()
    {
        if (!inSelectionMode || currentFilter == null) return;

        playerInventory.UnequipItemByType(currentFilter.Value);
        PlaySound(unequipSound);

        ExitSelectionMode();
        EnableEquippedSlotInteraction(true);
        ResetAllHighlights();
        UpdateUI();
    }

    public void SwapEquippedItem(Inventory_Item newItem)
    {
        if (newItem == null) return;

        playerInventory.TryEquipFromEquipmentInventory(newItem);
        PlaySound(equipSound);

        ExitSelectionMode();
        EnableEquippedSlotInteraction(true);
        ResetAllHighlights();
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
    }

    public bool IsSelectionEnabled() => selectionEnabled;
    public bool IsInSelectionMode() => inSelectionMode;

    public void EnableEquipmentSlotSelection(bool enable)
    {
        foreach (var slot in uiSlots)
        {
            slot.SetSelectable(enable);
        }
    }

    public void EnableEquippedSlotInteraction(bool enable)
    {
        foreach (var slot in equippedSlotsPanel.GetEquippedSlots())
        {
            slot.SetInteractable(enable);
        }
    }

    public void ResetAllHighlights()
    {
        foreach (var equipSlot in equippedSlotsPanel.GetComponentsInChildren<UI_EquippedSlot>())
        {
            equipSlot.ResetHighlight();
        }

        foreach (var slot in uiSlots)
        {
            slot.ResetHighlight();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public override bool HandleCancel()
    {
        if (IsInSelectionMode())
        {
            ExitSelectionMode();
            return true;
        }

        Close();
        var ui = FindObjectOfType<UI>();
        ui?.OpenMainMenuDirect();
        return true;
    }

    public void GoToMainMenuPanel()
    {
        Close();
        var ui = FindObjectOfType<UI>();
        ui?.OpenMainMenuDirect();
    }
}
