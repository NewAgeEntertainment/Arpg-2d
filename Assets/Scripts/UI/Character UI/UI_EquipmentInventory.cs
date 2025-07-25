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
    [SerializeField] private Transform equipmentSlotPanel;
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

    private float cancelCooldown = 0f;
    private const float cancelCooldownDuration = 0.2f; // 200 ms debounce

    private enum PanelState { None, EquippedPanel, ItemList }
    private PanelState currentState = PanelState.None;

    private ItemType? currentFilter = null;

    private void Awake()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);
        equipmentInventory = FindFirstObjectByType<Inventory_Equipment>();
        playerInventory = FindFirstObjectByType<Inventory_Player>();

        equipmentInventory.OnInventoryChange += UpdateUI;
        playerInventory.OnInventoryChange += UpdateUI;

        uiSlots.AddRange(equipmentSlotPanel.GetComponentsInChildren<UI_EquipmentSlot>(true));
        foreach (var slot in uiSlots)
        {
            slot.SetEquipmentToolTip(equipmentToolTip);
            slot.SetSelectable(true);
        }

        Close();
    }

    public void Open()
    {
        isOpen = true;
        panelRoot?.SetActive(true);
        ShowEquippedPanel();
        UpdateUI();
    }

    public void Close()
    {
        isOpen = false;
        panelRoot?.SetActive(false);
        equipmentToolTip?.ShowEquipmentToolTip(false, null);
        currentState = PanelState.None;
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

    public void ShowEquipmentInventoryPanel(ItemType filterType)
    {
        currentFilter = filterType;
        equippedSlotsPanel?.gameObject.SetActive(false);
        equipmentSlotPanel?.gameObject.SetActive(true);
        removeButton?.gameObject.SetActive(true);
        currentState = PanelState.ItemList;
        UpdateUI();
    }

    public void RemoveCurrentlyEquipped()
    {
        if (currentFilter == null) return;

        playerInventory.UnequipItemByType(currentFilter.Value);
        PlaySound(unequipSound);

        ShowEquippedPanel();
        UpdateUI();
    }

    public void SwapEquippedItem(Inventory_Item newItem)
    {
        if (newItem == null) return;

        playerInventory.TryEquipFromEquipmentInventory(newItem);
        PlaySound(equipSound);

        ShowEquippedPanel();
        UpdateUI();
    }

    private void ShowEquippedPanel()
    {
        equipmentSlotPanel?.gameObject.SetActive(false);
        equippedSlotsPanel?.gameObject.SetActive(true);
        removeButton?.gameObject.SetActive(false);
        ResetAllHighlights();
        currentState = PanelState.EquippedPanel;
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

    private bool IsOnItemListPanel()
    {
        return equipmentSlotPanel.gameObject.activeSelf && !equippedSlotsPanel.gameObject.activeSelf;
    }

    public override bool HandleCancel()
    {
        Debug.Log("[UI_EquipmentInventory] HandleCancel() called. Current state: " + currentState);

        if (IsOnItemListPanel() || currentState == PanelState.ItemList)
        {
            Debug.Log("[UI_EquipmentInventory] Returning to Equipped Slot Panel");
            ShowEquippedPanel();
            return true;
        }

        Debug.Log("[UI_EquipmentInventory] Closing Equipment UI");
        Close();
        FindObjectOfType<UI>()?.OpenMainMenuDirect();
        return true;
    }

    public void GoToMainMenuPanel()
    {
        Close();
        var ui = FindObjectOfType<UI>();
        ui?.OpenMainMenuDirect();
    }
}
