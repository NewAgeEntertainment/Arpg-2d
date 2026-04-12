using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class UI_Craft : MonoBehaviour
{
    [Header("Backpack & Equip Slots")]
    [SerializeField] private UI_ItemSlotParent inventoryParent;

    private Inventory_Player playerInventory;
    private Inventory_Storage storage;

    private UI_CraftPreview craftPreviewUI;
    private UI_CraftSlot[] craftSlots;
    private UI_CraftListBtn[] craftListButtons;

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "UICancel";
    private Rewired.Player rPlayer;

    private float cancelCooldown = 0f;
    private const float cancelCooldownDuration = 0.2f;

    private void Awake()
    {
        try
        {
            rPlayer = ReInput.players.GetPlayer(playerID);
        }
        catch
        {
            rPlayer = null;
        }
    }

    private void OnEnable()
    {
        cancelCooldown = 0f;
        UpdateUI();
    }

    private void OnDisable()
    {
        UnsubscribeInventoryEvents();
    }

    private void Update()
    {
     

        if (cancelCooldown > 0f)
            cancelCooldown -= Time.deltaTime;

        
    }

    public void SetupCraftUI(Inventory_Storage storage)
    {
        UnsubscribeInventoryEvents();

        this.storage = storage;
        if (this.storage == null)
        {
            Debug.LogWarning("[UI_Craft] SetupCraftUI called with null storage.");
            playerInventory = null;
            return;
        }

        playerInventory = this.storage.playerInventory;
        if (playerInventory == null)
        {
            Debug.LogWarning("[UI_Craft] Storage has no playerInventory assigned.");
            return;
        }

        Debug.Log($"[UI_Craft] Setup: Backpack {playerInventory.itemList.Count} Equip {playerInventory.equipmentInventory.itemList.Count}");

        craftPreviewUI = GetComponentInChildren<UI_CraftPreview>(true);
        if (craftPreviewUI != null)
            craftPreviewUI.SetupCraftPreview(this.storage);

        SetupCraftListButtons();

        playerInventory.OnInventoryChange += UpdateUI;
        if (playerInventory.equipmentInventory != null)
            playerInventory.equipmentInventory.OnInventoryChange += UpdateUI;

        this.storage.OnInventoryChange += UpdateUI;

        UpdateUI();
    }

    private void SetupCraftListButtons()
    {
        craftSlots = GetComponentsInChildren<UI_CraftSlot>(true);
        craftListButtons = GetComponentsInChildren<UI_CraftListBtn>(true);

        foreach (var slot in craftSlots)
            slot.gameObject.SetActive(false);

        foreach (var btn in craftListButtons)
            btn.setCraftSlots(craftSlots);
    }

    public void UpdateUI()
    {
        if (playerInventory == null || inventoryParent == null) return;

        var combined = new List<Inventory_Item>();
        combined.AddRange(playerInventory.itemList);

        if (playerInventory.equipmentInventory != null)
            combined.AddRange(playerInventory.equipmentInventory.itemList);

        Debug.Log($"[UI_Craft] UpdateUI → CombinedItems: {combined.Count}");

        foreach (var slot in inventoryParent.GetComponentsInChildren<UI_StorageSlot>(true))
            slot.SetStorage(storage);

        inventoryParent.UpdateSlots(combined);
    }

    private void UnsubscribeInventoryEvents()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChange -= UpdateUI;

        if (playerInventory != null && playerInventory.equipmentInventory != null)
            playerInventory.equipmentInventory.OnInventoryChange -= UpdateUI;

        if (storage != null)
            storage.OnInventoryChange -= UpdateUI;
    }

    public bool HandleCancel()
    {
        var ui = UI.Instance ?? FindFirstObjectByType<UI>(FindObjectsInactive.Include);
        if (ui != null && ui.CraftUI == this)
        {
            ui.CloseCraft();
            return true;
        }
        return false;
    }
}