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
    private readonly List<UI_EquipmentSlot> uiSlots = new List<UI_EquipmentSlot>();

    private bool isOpen = false;
    public bool IsOpen => isOpen;
    public UI_EquipSlotParent EquippedSlotsPanel => equippedSlotsPanel;

    private enum PanelState { None, EquippedPanel, ItemList }
    private PanelState currentState = PanelState.None;

    private ItemType? currentFilter = null;
    private bool subscribed = false;

    private void Awake()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);

        if (equipmentInventory == null) equipmentInventory = FindFirstObjectByType<Inventory_Equipment>();
        if (playerInventory == null) playerInventory = FindFirstObjectByType<Inventory_Player>();

        // cache existing UI_EquipmentSlot children (if laid out in editor)
        if (equipmentSlotPanel != null)
            uiSlots.AddRange(equipmentSlotPanel.GetComponentsInChildren<UI_EquipmentSlot>(true));

        foreach (var slot in uiSlots)
        {
            slot.SetEquipmentToolTip(equipmentToolTip);
            slot.SetSelectable(true);
        }

        Close();
    }

    private void OnEnable()
    {
        HookEvents();
        // If opened via code while panel is inactive, ForceRefresh ensures lists match data
        ForceRefresh();
    }

    private void OnDisable() => UnhookEvents();
    private void OnDestroy() => UnhookEvents();

    private void HookEvents()
    {
        if (subscribed) return;
        if (equipmentInventory != null) equipmentInventory.OnInventoryChange += UpdateUI;
        if (playerInventory != null) playerInventory.OnInventoryChange += UpdateUI;
        subscribed = true;
    }

    private void UnhookEvents()
    {
        if (!subscribed) return;
        if (equipmentInventory != null) equipmentInventory.OnInventoryChange -= UpdateUI;
        if (playerInventory != null) playerInventory.OnInventoryChange -= UpdateUI;
        subscribed = false;
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

    /// <summary>
    /// Rebuild both equipped & unequipped lists. (Called by saver post-load)
    /// </summary>
    public void RefreshPanels()
    {
        UpdateEquippedSlots();
        UpdateUnequippedItemList();
    }

    /// <summary>
    /// Force a rebuild even if panel isn’t open yet (safe to call post-load).
    /// </summary>
    public void ForceRefresh()
    {
        // Temporarily let UpdateUI run even if panel is closed.
        if (isOpen) UpdateUI();
        else
        {
            UpdateEquippedSlots();
            UpdateUnequippedItemList();
        }
    }

    public void UpdateUI()
    {
        if (!isOpen) return;
        UpdateUnequippedItemList();
        UpdateEquippedSlots();
    }

    private void UpdateUnequippedItemList()
    {
        if (equipmentInventory == null || equipmentSlotPanel == null) return;

        var items = equipmentInventory.itemList;
        List<Inventory_Item> filteredItems = new List<Inventory_Item>();

        foreach (var item in items)
        {
            if (currentFilter == null || item.itemData.itemType == currentFilter)
                filteredItems.Add(item);
        }

        // Ensure we have enough UI slots; create more if the panel uses a prefab pattern
        if (slotPrefab != null && uiSlots.Count < filteredItems.Count)
        {
            int toCreate = filteredItems.Count - uiSlots.Count;
            for (int i = 0; i < toCreate; i++)
            {
                var slot = Instantiate(slotPrefab, equipmentSlotPanel);
                slot.SetEquipmentToolTip(equipmentToolTip);
                slot.SetSelectable(true);
                uiSlots.Add(slot);
            }
        }

        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < filteredItems.Count)
                uiSlots[i].UpdateSlot(filteredItems[i]);
            else
                uiSlots[i].Clear();
        }
    }

    private void UpdateEquippedSlots()
    {
        if (equippedSlotsPanel == null || playerInventory == null) return;
        equippedSlotsPanel.UpdateEquipmentSlots(playerInventory.equipList);
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
        if (equippedSlotsPanel != null)
        {
            foreach (var equipSlot in equippedSlotsPanel.GetComponentsInChildren<UI_EquippedSlot>(true))
                equipSlot.ResetHighlight();
        }

        foreach (var slot in uiSlots)
            slot.ResetHighlight();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private bool IsOnItemListPanel()
    {
        return equipmentSlotPanel != null && equipmentSlotPanel.gameObject.activeSelf
               && (equippedSlotsPanel == null || !equippedSlotsPanel.gameObject.activeSelf);
    }

    public override bool HandleCancel()
    {
        Debug.Log("[UI_EquipmentInventory] HandleCancel() called. Current state: " + currentState);

        // ItemList -> EquippedPanel
        if (IsOnItemListPanel() || currentState == PanelState.ItemList)
        {
            Debug.Log("[UI_EquipmentInventory] Returning to Equipped Slot Panel");
            ShowEquippedPanel();
            return true;
        }

        // EquippedPanel -> Close via UI manager (same pattern as Inventory)
        Debug.Log("[UI_EquipmentInventory] Request close via UI.Instance");
        if (UI.Instance != null)
            UI.Instance.CloseEquipment();
        else
            Close(); // fallback

        return true;
    }


    public void GoToMainMenuPanel()
    {
        Close();
        var ui = FindObjectOfType<UI>();
        ui?.OpenMainMenuDirect();
    }
}
