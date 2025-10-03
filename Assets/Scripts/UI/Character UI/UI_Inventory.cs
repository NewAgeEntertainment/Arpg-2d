using Rewired;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_Inventory : UI_Panel
{
    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "Cancel";
    [SerializeField] private string assignPopupAction = "AssignPopup";
    [SerializeField] private string useOnPlayerAction = "UseOnPlayer";
    private Rewired.Player rPlayer;

    [Header("References")]
    [SerializeField] private Inventory_Player inventory;
    [SerializeField] private UI_ItemSlotParent backpackSlotsParent;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Panels")]
    [SerializeField] private GameObject categoryPanel;
    [SerializeField] private GameObject itemListPanel;
    [SerializeField] private GameObject actorSelectPanel;
    [SerializeField] private GameObject assignPopupPanel; // (legacy panel; safe to keep)

    // In UI_Inventory fields:
    [SerializeField] private UI_AssignPopup assignPopup; // ← drag your popup here

    [Header("Assign Popup UI")]
    [SerializeField] private TMP_InputField assignAmountInput;

    [Header("Actor Select")]
    [SerializeField] private List<UI_CharacterProfileButton> actorButtons;
    [SerializeField] private TextMeshProUGUI actorSelectItemLabel;

    [Header("Sound")]
    [SerializeField] private AudioClip panelOpenSound;
    [SerializeField] private AudioClip panelCloseSound;
    [SerializeField] private AudioClip assignSound;
    [SerializeField] private AudioClip noItemSound;
    [SerializeField] private AudioClip itemUsedSound;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private InventoryMenuController menuController; // <- drag in Inspector


    // -------- Legacy compatibility: assignslot / AssignSlot (uses UI_ItemSlot) --------
    [Header("Legacy Slot Binding (optional)")]
    [Tooltip("Enables AssignSlot/assignslot compatibility for older code paths.")]
    [SerializeField] private bool enableLegacyAssignSlot = true;
    private readonly List<UI_ItemSlot> _legacySlots = new();

    // ======== NEW: Main Menu Controller-style navigation ========
    [Header("Navigation (Main Menu Controller)")]
    [SerializeField] private InventoryMenuController menuNav;

    private void EnsureMenuNav()
    {
        if (menuNav == null)
            menuNav = GetComponent<InventoryMenuController>();
        if (menuNav == null)
            menuNav = FindFirstObjectByType<InventoryMenuController>(FindObjectsInactive.Include);
    }
    // ============================================================

    private bool _playedNoItemSFXThisOpen = false;

    private bool isOpen = false;
    private bool _dirty = false;
    private ItemType? currentFilter = null;
    private Inventory_Item itemBeingAssigned;

    private enum PanelState { None, Category, ItemList, ActorSelect, AssignPopup }
    private PanelState currentState = PanelState.None;

    private System.Action<int> _goldChangedHandler;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>();

        if (inventory != null)
        {
            inventory.OnInventoryChange += HandleInventoryChanged;
            _goldChangedHandler = _ => HandleInventoryChanged();
            inventory.OnGoldChanged += _goldChangedHandler;
        }

        if (backpackSlotsParent != null)
            backpackSlotsParent.OnSlotSubmit += OnItemSlotSubmit;

        if (audioSource != null)
            audioSource.ignoreListenerPause = true;

        CloseInventory();
    }

    private void Start()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);
        EnsureMenuNav();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChange -= HandleInventoryChanged;
            if (_goldChangedHandler != null) inventory.OnGoldChanged -= _goldChangedHandler;
        }

        if (backpackSlotsParent != null)
            backpackSlotsParent.OnSlotSubmit -= OnItemSlotSubmit;
    }

    private void Update()
    {
        if (rPlayer == null || !isOpen) return;

        if (rPlayer.GetButtonDown(cancelAction))
            HandleCancel();

        if (rPlayer.GetButtonDown(assignPopupAction) && currentState == PanelState.ItemList)
        {
            if (backpackSlotsParent.TryGetSelectedItem(out Inventory_Item selected))
            {
                if (selected.itemData.itemType == ItemType.Consumable && !selected.itemData.isUsable)
                    OpenAssignPopup(selected);
            }
        }

        if (rPlayer.GetButtonDown(useOnPlayerAction) && currentState == PanelState.ItemList)
        {
            if (backpackSlotsParent.TryGetSelectedItem(out Inventory_Item selected))
            {
                if (selected.itemData.itemType == ItemType.Consumable && selected.itemData.isUsable)
                    OpenActorSelectPanel(selected);
            }
        }
    }

    private void HandleInventoryChanged()
    {
        _dirty = true;

        if (isOpen && currentState == PanelState.ItemList)
        {
            ForceRefresh();
            // Rebuild item grid nav if visible & changed
            EnsureMenuNav();
            menuNav?.BuildForItemGrid();
        }

        if (currentState == PanelState.ActorSelect)
        {
            UpdateActorSelectButtons();
            UpdateActorSelectHeader();
            MaybePlayNoItemSFX();

            // keep actor grid nav fresh
            EnsureMenuNav();
            menuNav?.BuildForActorGrid();
        }

        if (goldText != null && inventory != null)
            goldText.text = $"{inventory.gold:N0}g.";

        // keep legacy-wired slots in sync
        RefreshAllLegacySlots();
    }

    public void ForceRefresh()
    {
        _dirty = false;
        UpdateUI(force: true);
        RefreshAllLegacySlots();

        // Rebuild nav for whichever panel is active
        EnsureMenuNav();
        switch (currentState)
        {
            case PanelState.Category: menuNav?.BuildForCategory(); break;
            case PanelState.ItemList: menuNav?.BuildForItemGrid(); break;
            case PanelState.ActorSelect: menuNav?.BuildForActorGrid(); break;
            case PanelState.AssignPopup: menuNav?.BuildForAssignPopup(); break;
        }
    }

    public void OpenInventory()
    {
        isOpen = true;
        gameObject.SetActive(true);
        OpenCategoryPanel();

        if (_dirty) ForceRefresh();
    }

    public void CloseInventory()
    {
        isOpen = false;
        CloseAllPanels();
        gameObject.SetActive(false);
        PlayCloseSound();

        FindFirstObjectByType<UI>()?.OpenMainMenuDirect();
    }

    public bool IsOpen() => isOpen;

    public override bool HandleCancel()
    {
        switch (currentState)
        {
            case PanelState.ActorSelect:
                actorSelectPanel.SetActive(false);
                OpenItemListPanel();
                PlayCloseSound();
                return true;
            case PanelState.AssignPopup:
                // If you used a legacy panel, hide it. The new popup manages itself.
                if (assignPopupPanel != null) assignPopupPanel.SetActive(false);
                OpenItemListPanel();
                PlayCloseSound();
                return true;
            case PanelState.ItemList:
                itemListPanel.SetActive(false);
                OpenCategoryPanel();
                PlayCloseSound();
                return true;
            case PanelState.Category:
                CloseInventory();
                return true;
            default:
                return false;
        }
    }

    public void SetFilter(string filterName)
    {
        currentFilter = filterName switch
        {
            "All" => null,
            "Items" => ItemType.Consumable,
            "Materials" => ItemType.Material,
            "Weapons" => ItemType.Weapon,
            "Armor" => ItemType.Armor,
            "Trinkets" => ItemType.trinket,
            "KeyItems" => ItemType.Key,
            _ => null
        };

        OpenItemListPanel();
        ForceRefresh();
    }

    private void MaybePlayNoItemSFX()
    {
        if (_playedNoItemSFXThisOpen) return;
        if (noItemSound == null) return;

        int remaining = (inventory != null && itemBeingAssigned?.itemData != null)
            ? inventory.CountItem(itemBeingAssigned.itemData)
            : 0;

        if (remaining <= 0)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(noItemSound);
            else
                AudioSource.PlayClipAtPoint(noItemSound, Vector3.zero);

            _playedNoItemSFXThisOpen = true;
        }
    }

    // UI_Inventory.cs (add this method anywhere in the class)
    private void TryOpenAssignPopupFromSelection()
    {
        if (currentState != PanelState.ItemList) return;
        if (backpackSlotsParent == null) return;

        if (!backpackSlotsParent.TryGetSelectedItem(out Inventory_Item selected) ||
            selected == null || selected.itemData == null)
            return;

        // Only open assign popup for consumables that are *not* "use on player"
        if (selected.itemData.itemType == ItemType.Consumable && !selected.itemData.isUsable)
            OpenAssignPopup(selected);
    }


    private void PlayItemUsedSFX()
    {
        if (audioSource != null && itemUsedSound != null)
            audioSource.PlayOneShot(itemUsedSound);
    }

    private void PlayNoItemSFX()
    {
        if (audioSource != null && noItemSound != null)
            audioSource.PlayOneShot(noItemSound);
    }

    private void PlayOpenSound()
    {
        if (audioSource != null && panelOpenSound != null)
            audioSource.PlayOneShot(panelOpenSound);
    }

    private void PlayCloseSound()
    {
        if (audioSource != null && panelCloseSound != null)
            audioSource.PlayOneShot(panelCloseSound);
    }

    public void OpenCategoryPanel()
    {
        CloseAllPanels();
        categoryPanel.SetActive(true);
        currentState = PanelState.Category;
        PlayOpenSound();

        // build & focus category row
        menuController?.BuildForCategory();
    }

    public void OpenItemListPanel()
    {
        CloseAllPanels();
        itemListPanel.SetActive(true);
        currentState = PanelState.ItemList;

        // make sure slots exist first, THEN wire/focus
        ForceRefresh();
        PlayOpenSound();

        menuNav?.BuildForItemGrid();
    }

    public void OpenActorSelectPanel(Inventory_Item item)
    {
        CloseAllPanels();
        actorSelectPanel.SetActive(true);
        currentState = PanelState.ActorSelect;
        itemBeingAssigned = item;
        _playedNoItemSFXThisOpen = false;
        PlayOpenSound();

        foreach (var btn in actorButtons)
        {
            if (btn == null) continue;

            btn.Setup(OnActorPicked);
            btn.SetUseItemContext(itemBeingAssigned, inventory, () =>
            {
                int remainingBefore = inventory.CountItem(itemBeingAssigned.itemData);
                bool used = remainingBefore > 0;
                if (used) PlayItemUsedSFX(); else PlayNoItemSFX();

                UpdateActorSelectButtons();
                UpdateActorSelectHeader();
            });

            btn.SetSelected(false);
        }

        UpdateActorSelectButtons();
        UpdateActorSelectHeader();

        // wire/focus actor grid
        menuNav?.BuildForActorGrid();
    }

    public void OpenAssignPopup(Inventory_Item item)
    {
        CloseAllPanels();
        currentState = PanelState.AssignPopup;

        if (assignPopup == null)
        {
            Debug.LogError("[UI_Inventory] assignPopup is not set in the Inspector.");
            return;
        }

        assignPopup.gameObject.SetActive(true);
        assignPopup.Open(item, OnAssignConfirmedToSlot);

        // Let the popup become active this frame, then wire (in case it spawns buttons on Open)
        StartCoroutine(DelayBuildAssignPopup());
    }

    private System.Collections.IEnumerator DelayBuildAssignPopup()
    {
        yield return null; // next frame after elements exist
        menuNav?.BuildForAssignPopup();
    }



    // ------------------------------
    // Assign-to-slot (NEW)
    // ------------------------------
    // UI_Inventory.cs
    private void OnAssignConfirmedToSlot(Inventory_Item item, int amount, int slotIndex1Based)
    {
        if (inventory == null || item == null || item.itemData == null) return;

        // Assign (clamps by owned - already assigned), uses a fresh instance for the slot
        inventory.SetQuickItemInSlot(slotIndex1Based, item, Mathf.Max(1, amount), useFreshInstance: true);

        // Optional SFX
        if (assignSound != null && audioSource != null)
            audioSource.PlayOneShot(assignSound);

        // Refresh HUD
        var ui = FindFirstObjectByType<UI>(FindObjectsInactive.Include);
        ui?.inGameUI?.UpdateQuickSlots();

        // Return to list
        OpenItemListPanel();
        ForceRefresh();
    }


    // UI_Inventory.cs
    private void AssignToSpecificQuickSlot(Inventory_Item item, int amount, int slotIndex1Based)
    {
        // Safety checks / lazy bind
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);
        if (inventory == null || item == null || item.itemData == null)
        {
            Debug.LogWarning("[UI_Inventory] AssignToSpecificQuickSlot: missing inventory or item.");
            return;
        }

        // Clamp inputs and assign (stores a fresh instance in the quick slot)
        int slot = Mathf.Clamp(slotIndex1Based, 1, (inventory.quickSlots?.Length ?? 4));
        int count = Mathf.Max(1, amount);
        inventory.SetQuickItemInSlot(slot, item, count, useFreshInstance: true);

        // Optional SFX
        if (assignSound != null && audioSource != null)
            audioSource.PlayOneShot(assignSound);

        // Refresh HUD & return to list
        var uiRoot = FindFirstObjectByType<UI>(FindObjectsInactive.Include);
        uiRoot?.inGameUI?.UpdateQuickSlots();

        if (assignPopupPanel != null) assignPopupPanel.SetActive(false);
        OpenItemListPanel();
        ForceRefresh();
    }




    // ------------------------------
    // (Kept for your older code paths)
    // ------------------------------
    private void OnAssignConfirmed(Inventory_Item item, int amount)
    {
        if (inventory == null || item == null || item.itemData == null) return;

        AssignToQuickSlots(item, amount);

        OpenItemListPanel();
        ForceRefresh();

        var ui = FindFirstObjectByType<UI>(FindObjectsInactive.Include);
        ui?.inGameUI?.UpdateQuickSlots();
    }

    private void AssignToQuickSlots(Inventory_Item item, int amount)
    {
        if (inventory.quickSlots == null || inventory.quickSlots.Length < 4)
        {
            Debug.LogError("[UI_Inventory] inventory.quickSlots not set or too small (need 4).");
            return;
        }

        // 1) Merge with a slot holding the same item (if any)
        for (int i = 0; i < inventory.quickSlots.Length; i++)
        {
            var qs = inventory.quickSlots[i];
            if (qs.item != null && qs.item.itemData == item.itemData)
            {
                inventory.quickSlots[i].slotStack = Mathf.Max(1, qs.slotStack + amount);
                Debug.Log($"[UI_Inventory] Added {amount} to quick slot {i + 1} (now x{inventory.quickSlots[i].slotStack}).");
                return;
            }
        }

        // 2) Otherwise pick the first empty slot
        for (int i = 0; i < inventory.quickSlots.Length; i++)
        {
            var qs = inventory.quickSlots[i];
            if (qs.item == null || qs.slotStack <= 0)
            {
                var copy = new Inventory_Item(item.itemData);
                copy.SetInstanceModifiers(item.GetInstanceModifiers());

                inventory.quickSlots[i].item = copy;
                inventory.quickSlots[i].slotStack = Mathf.Max(1, amount);

                Debug.Log($"[UI_Inventory] Assigned {item.itemData.itemName} x{amount} to quick slot {i + 1}.");
                return;
            }
        }

        // 3) No space -> replace slot 1 (policy choice)
        {
            var copy = new Inventory_Item(item.itemData);
            copy.SetInstanceModifiers(item.GetInstanceModifiers());
            inventory.quickSlots[0].item = copy;
            inventory.quickSlots[0].slotStack = Mathf.Max(1, amount);
            Debug.Log($"[UI_Inventory] No empty quick slot. Replaced slot 1 with {item.itemData.itemName} x{amount}.");
        }
    }

    private void CloseAllPanels()
    {
        categoryPanel?.SetActive(false);
        itemListPanel?.SetActive(false);
        actorSelectPanel?.SetActive(false);
        assignPopupPanel?.SetActive(false); // legacy only; new popup hides itself
        currentState = PanelState.None;
    }

    public void UpdateUI(bool force = false)
    {
        if (!force && !isOpen) return;

        if (goldText != null && inventory != null)
            goldText.text = $"{inventory.gold:N0}g.";

        var combined = new List<Inventory_Item>();

        if (inventory?.itemList != null)
            combined.AddRange(inventory.itemList);

        if (inventory?.equipmentInventory?.itemList != null)
            combined.AddRange(inventory.equipmentInventory.itemList);

        if (inventory?.storage?.materialStash != null)
            combined.AddRange(inventory.storage.materialStash);

        var filtered = new List<Inventory_Item>();
        foreach (var it in combined)
        {
            if (it?.itemData == null) continue;
            if (!currentFilter.HasValue || it.itemData.itemType == currentFilter.Value)
                filtered.Add(it);
        }

        if (backpackSlotsParent == null)
        {
            Debug.LogError("[UI_Inventory] backpackSlotsParent is null!");
            return;
        }

        backpackSlotsParent.UpdateSlots(filtered);

        // Keep nav in sync if we changed the items while on ItemList
        if (currentState == PanelState.ItemList)
        {
            EnsureMenuNav();
            menuNav?.BuildForItemGrid();
        }
    }

    private void OnActorPicked(Player p)
    {
        foreach (var btn in actorButtons)
            btn?.SetSelected(btn.linkedPlayer == p);
    }

    private void UpdateActorSelectButtons()
    {
        foreach (var button in actorButtons)
            button?.RefreshBars();
    }

    private void UpdateActorSelectHeader()
    {
        if (actorSelectItemLabel == null || itemBeingAssigned == null || itemBeingAssigned.itemData == null)
            return;

        int remaining = inventory != null ? inventory.CountItem(itemBeingAssigned.itemData) : 0;

        actorSelectItemLabel.text = $"{itemBeingAssigned.itemData.itemName} x{remaining}";

        if (remaining <= 0 && noItemSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(noItemSound);
        }
    }

    private int GetAssignAmount()
    {
        if (assignAmountInput == null) return 1;
        return int.TryParse(assignAmountInput.text, out int result) ? Mathf.Max(1, result) : 1;
    }

    private void OnItemSlotSubmit(Inventory_Item item)
    {
        if (item == null || item.itemData == null) return;

        if (item.itemData.itemType != ItemType.Consumable)
        {
            Debug.Log($"[Inventory] {item.itemData.itemName} is not a consumable and cannot be used.");
            return;
        }

        if (item.itemData.isUsable)
            OpenActorSelectPanel(item);
        else
            OpenAssignPopup(item);
    }

    // ======================
    // Legacy assignslot shim (uses UI_ItemSlot)
    // ======================
    public void assignslot(int index, UI_ItemSlot slot) => AssignSlot(index, slot);

    public void AssignSlot(int index, UI_ItemSlot slot)
    {
        if (!enableLegacyAssignSlot || slot == null) return;

        while (_legacySlots.Count <= index) _legacySlots.Add(null);
        _legacySlots[index] = slot;

        // No Initialize(...) on UI_ItemSlot; just repaint it.
        RefreshLegacySlot(index);
    }

    private void RefreshLegacySlot(int index)
    {
        if (index < 0 || index >= _legacySlots.Count) return;
        var slot = _legacySlots[index];
        if (slot == null) return;

        Inventory_Item item = null;
        if (inventory != null && inventory.itemList != null &&
            index >= 0 && index < inventory.itemList.Count)
        {
            item = inventory.itemList[index];
        }

        // UI_ItemSlot API uses UpdateSlot(item) and Clear()
        if (item == null) slot.Clear();
        else slot.UpdateSlot(item);
    }

    private void RefreshAllLegacySlots()
    {
        for (int i = 0; i < _legacySlots.Count; i++) RefreshLegacySlot(i);
    }
}
