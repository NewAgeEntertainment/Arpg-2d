using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Rewired;

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
    [SerializeField] private GameObject assignPopupPanel;

    [Header("Assign Popup UI")]
    [SerializeField] private TMP_InputField assignAmountInput;

    [Header("Actor Select")]
    [SerializeField] private List<UI_CharacterProfileButton> actorButtons;
    [SerializeField] private TextMeshProUGUI actorSelectItemLabel;

    [Header("Sound")]
    [SerializeField] private AudioClip panelOpenSound;
    [SerializeField] private AudioClip panelCloseSound;
    [SerializeField] private AudioClip assignSound;
    [SerializeField] private AudioClip noItemSound;   // 🔹 New sound
    [SerializeField] private AudioClip itemUsedSound; // SFX for item used
    [SerializeField] private AudioSource audioSource;


    private bool _playedNoItemSFXThisOpen = false;    // ← avoid double-playing per panel open

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
        {
            audioSource.ignoreListenerPause = true; // play even if AudioListener.pause is true
        }


        CloseInventory();
    }

    private void Start()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);
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
            ForceRefresh();

        if (currentState == PanelState.ActorSelect)
        {
            UpdateActorSelectButtons();
            UpdateActorSelectHeader();
            MaybePlayNoItemSFX(); // ← cover external consumption too
        }

        if (goldText != null && inventory != null)
            goldText.text = $"{inventory.gold:N0}g.";
    }


    public void ForceRefresh()
    {
        _dirty = false;
        UpdateUI(force: true);
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

        FindObjectOfType<UI>()?.OpenMainMenuDirect();
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
                assignPopupPanel.SetActive(false);
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

        // Nothing left of the selected item?
        int remaining = (inventory != null && itemBeingAssigned?.itemData != null)
            ? inventory.CountItem(itemBeingAssigned.itemData)
            : 0;

        if (remaining <= 0)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(noItemSound);
            else
                AudioSource.PlayClipAtPoint(noItemSound, Vector3.zero); // fallback

            _playedNoItemSFXThisOpen = true;
        }
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
    }

    public void OpenItemListPanel()
    {
        CloseAllPanels();
        itemListPanel.SetActive(true);
        currentState = PanelState.ItemList;
        ForceRefresh();
        PlayOpenSound();
    }

    public void OpenActorSelectPanel(Inventory_Item item)
    {
        CloseAllPanels();
        actorSelectPanel.SetActive(true);
        currentState = PanelState.ActorSelect;
        itemBeingAssigned = item;
        _playedNoItemSFXThisOpen = false;   // ← reset here
        PlayOpenSound();

        // ...
        foreach (var btn in actorButtons)
        {
            if (btn == null) continue;

            btn.Setup(OnActorPicked);
            btn.SetUseItemContext(itemBeingAssigned, inventory, () =>
            {
                // Always play a click SFX based on whether an item was used or not
                int remainingBefore = inventory.CountItem(itemBeingAssigned.itemData);

                bool used = false;
                if (remainingBefore > 0)
                {
                    used = true;
                    PlayItemUsedSFX();
                }
                else
                {
                    PlayNoItemSFX();
                }

                UpdateActorSelectButtons();
                UpdateActorSelectHeader();

                // If used, check again after update
                int remainingAfter = inventory.CountItem(itemBeingAssigned.itemData);
                if (used && remainingAfter <= 0)
                {
                    // Optional: could also trigger a special sound here if last one used
                }
            });


            btn.SetSelected(false);
        }

        UpdateActorSelectButtons();
        UpdateActorSelectHeader();
    }


    public void OpenAssignPopup(Inventory_Item item)
    {
        CloseAllPanels();
        assignPopupPanel.SetActive(true);
        currentState = PanelState.AssignPopup;
        itemBeingAssigned = item;
        if (assignAmountInput != null) assignAmountInput.text = "1";
        PlayOpenSound();
    }

    private void CloseAllPanels()
    {
        categoryPanel?.SetActive(false);
        itemListPanel?.SetActive(false);
        actorSelectPanel?.SetActive(false);
        assignPopupPanel?.SetActive(false);
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
        foreach (var item in combined)
        {
            if (item?.itemData == null) continue;
            if (!currentFilter.HasValue || item.itemData.itemType == currentFilter.Value)
                filtered.Add(item);
        }

        if (backpackSlotsParent == null)
        {
            Debug.LogError("[UI_Inventory] backpackSlotsParent is null!");
            return;
        }

        backpackSlotsParent.UpdateSlots(filtered);
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

        // 🔹 Play "no item" sound if out of items
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
}
