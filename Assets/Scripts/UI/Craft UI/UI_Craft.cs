using System.Collections.Generic;
using UnityEngine;
using Rewired; // keep this

public class UI_Craft : MonoBehaviour
{
    [Header("Backpack & Equip Slots")]
    [SerializeField] private UI_ItemSlotParent inventoryParent; // shows BOTH bags

    private Inventory_Player playerInventory;
    private Inventory_Storage storage;

    private UI_CraftPreview craftPreviewUI;
    private UI_CraftSlot[] craftSlots;
    private UI_CraftListBtn[] craftListButtons;

    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "UICancel";
    private Rewired.Player rPlayer;               // <-- make sure it's Rewired.Player

    private float cancelCooldown = 0f;
    private const float cancelCooldownDuration = 0.2f;

    private void Awake()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);  // OK now
    }

    private void OnEnable()
    {
        cancelCooldown = 0f;
        UpdateUI();
    }

    private void Update()
    {
        if (cancelCooldown > 0f)
            cancelCooldown -= Time.deltaTime;

        if (rPlayer != null && cancelCooldown <= 0f && rPlayer.GetButtonDown(cancelAction))
        {
            if (HandleCancel())
            {
                cancelCooldown = cancelCooldownDuration;
            }
        }
    }

    /// <summary>
    /// Called by the Blacksmith (or any other source) to wire up Craft UI.
    /// </summary>
    public void SetupCraftUI(Inventory_Storage storage)
    {
        this.storage = storage;
        playerInventory = storage.playerInventory;

        Debug.Log($"[UI_Craft] Setup: Backpack {playerInventory.itemList.Count} Equip {playerInventory.equipmentInventory.itemList.Count}");

        craftPreviewUI = GetComponentInChildren<UI_CraftPreview>();
        craftPreviewUI.SetupCraftPreview(storage);

        SetupCraftListButtons();

        playerInventory.OnInventoryChange += UpdateUI;
        playerInventory.equipmentInventory.OnInventoryChange += UpdateUI;
        storage.OnInventoryChange += UpdateUI;

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
        combined.AddRange(playerInventory.equipmentInventory.itemList);

        Debug.Log($"[UI_Craft] UpdateUI → CombinedItems: {combined.Count}");

        // If these slots are only inside the craft UI, keep. Otherwise remove to avoid touching storage UI.
        foreach (var slot in inventoryParent.GetComponentsInChildren<UI_StorageSlot>(true))
            slot.SetStorage(storage);

        inventoryParent.UpdateSlots(combined);
    }

    /// <summary>
    /// Called from UI.cs HandleBackAction() or internally via Update() when UICancel is pressed.
    /// </summary>
    public bool HandleCancel()
    {
        var ui = FindObjectOfType<UI>();
        if (ui != null && ui.CraftUI == this)
        {
            ui.CloseCraft();
            return true;
        }
        return false;
    }
}
