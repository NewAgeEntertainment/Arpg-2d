using UnityEngine;
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

    [Header("Actor Buttons")]
    [SerializeField] private List<UI_CharacterProfileButton> actorButtons;

    [Header("Sound")]
    [SerializeField] private AudioClip panelOpenSound;
    [SerializeField] private AudioClip panelCloseSound;
    [SerializeField] private AudioClip assignSound;
    [SerializeField] private AudioSource audioSource;
    



    private bool isOpen = false;
    private ItemType? currentFilter = null;
    private Inventory_Item itemBeingAssigned;

    private enum PanelState { None, Category, ItemList, ActorSelect, AssignPopup }
    private PanelState currentState = PanelState.None;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>();

        inventory.OnInventoryChange += UpdateUI;

        if (backpackSlotsParent != null)
            backpackSlotsParent.OnSlotSubmit += OnItemSlotSubmit;

        CloseInventory();
    }

    private void Start()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChange -= UpdateUI;

        if (backpackSlotsParent != null)
            backpackSlotsParent.OnSlotSubmit -= OnItemSlotSubmit;
    }

    private void Update()
    {
        if (rPlayer == null || !isOpen) return;

        if (rPlayer.GetButtonDown(cancelAction))
        {
            HandleCancel();
        }

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

    public void OpenInventory()
    {
        isOpen = true;
        gameObject.SetActive(true);
        OpenCategoryPanel();
    }

    public void CloseInventory()
    {
        isOpen = false;
        CloseAllPanels();
        gameObject.SetActive(false);
        PlayCloseSound();

        // 🟩 Open main menu directly
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
        UpdateUI();
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
        UpdateUI();
        PlayOpenSound();
    }

    public void OpenActorSelectPanel(Inventory_Item item)
    {
        CloseAllPanels();
        actorSelectPanel.SetActive(true);
        currentState = PanelState.ActorSelect;
        itemBeingAssigned = item;
        PlayOpenSound();
        
}

    public void OpenAssignPopup(Inventory_Item item)
    {
        CloseAllPanels();
        assignPopupPanel.SetActive(true);
        currentState = PanelState.AssignPopup;
        itemBeingAssigned = item;
        assignAmountInput.text = "1";
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

    public void UpdateUI()
    {
        if (!isOpen) return;

        goldText.text = $"{inventory.gold:N0}g.";

        var combined = new List<Inventory_Item>();
        combined.AddRange(inventory.itemList);
        combined.AddRange(inventory.equipmentInventory.itemList);
        combined.AddRange(inventory.storage.materialStash);

        var filtered = new List<Inventory_Item>();
        foreach (var item in combined)
        {
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

    public void OnPlayerSelectedFromActorPanel(Player player)
    {
        if (itemBeingAssigned == null) return;

        var matchedItem = inventory.FindSameItem(itemBeingAssigned);
        if (matchedItem == null) return;

        if (matchedItem.itemEffect != null && matchedItem.itemEffect.CanBeUsed(player))
        {
            matchedItem.itemEffect.Subscribe(player);
            matchedItem.itemEffect.ExecuteEffect(player);

            inventory.RemoveOneItem(matchedItem);
            inventory.TriggerUpdateUI();
            UpdateActorSelectButtons();
        }
    }

    private void UpdateActorSelectButtons()
    {
        foreach (var button in actorButtons)
            button.RefreshBars();
    }

    public void AssignSlot1() => AssignToQuickSlot(1);
    public void AssignSlot2() => AssignToQuickSlot(2);
    public void AssignSlot3() => AssignToQuickSlot(3);
    public void AssignSlot4() => AssignToQuickSlot(4);

    private void AssignToQuickSlot(int slotNumber)
    {
        if (itemBeingAssigned == null) return;

        int amount = GetAssignAmount();
        if (amount <= 0) return;

        // Assign to quick slot
        inventory.SetQuickItemInSlot(slotNumber, itemBeingAssigned, amount);

        // 🔻 Reduce stack size
        Inventory_Item itemInInventory = inventory.FindSameItem(itemBeingAssigned);
        if (itemInInventory != null)
        {
            itemInInventory.stackSize -= amount;
            if (itemInInventory.stackSize <= 0)
                inventory.itemList.Remove(itemInInventory);
        }

        // ✅ Play sound
        if (audioSource != null && assignSound != null)
            audioSource.PlayOneShot(assignSound);

        inventory.TriggerUpdateUI();
        itemBeingAssigned = null;
        assignPopupPanel?.SetActive(false);
        OpenItemListPanel();
    }



    public void IncreaseAssignAmount()
    {
        if (itemBeingAssigned == null || assignAmountInput == null) return;

        int current = GetAssignAmount() + 1;
        int totalOwned = inventory.CountItem(itemBeingAssigned.itemData);
        int assignedElsewhere = inventory.CountAssigned(itemBeingAssigned.itemData);

        int maxAssignable = totalOwned - assignedElsewhere;
        if (current > maxAssignable) current = maxAssignable;

        assignAmountInput.text = Mathf.Max(1, current).ToString();
    }

    public void DecreaseAssignAmount()
    {
        if (itemBeingAssigned == null || assignAmountInput == null) return;

        int current = Mathf.Max(1, GetAssignAmount() - 1);
        assignAmountInput.text = current.ToString();
    }

    private int GetAssignAmount()
    {
        if (assignAmountInput == null) return 1;
        return int.TryParse(assignAmountInput.text, out int result) ? Mathf.Max(1, result) : 1;
    }

    public void GoToMainMenuPanel()
    {
        FindObjectOfType<UI>()?.OpenMainMenuDirect();
    }

    private void OnItemSlotSubmit(Inventory_Item item)
    {
        if (item == null || item.itemData == null) return;

        // Only allow AssignPopup or ActorSelect for Consumables
        if (item.itemData.itemType != ItemType.Consumable)
        {
            Debug.Log($"[Inventory] {item.itemData.itemName} is not a consumable and cannot be used.");
            return;
        }

        // Now decide based on isUsable
        if (item.itemData.isUsable)
            OpenActorSelectPanel(item);
        else
            OpenAssignPopup(item);
    }


}
