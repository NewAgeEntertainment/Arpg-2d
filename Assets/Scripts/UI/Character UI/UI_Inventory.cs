using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UI_Inventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory_Player inventory;
    [SerializeField] private UI_ItemSlotParent backpackSlotsParent;
    [SerializeField] private TMP_InputField searchField;
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

    private ItemType? currentFilter = null;
    private bool isOpen = false;

    private Inventory_Item itemBeingAssigned;

    private enum PanelState { None, Category, ItemList, ActorSelect, AssignPopup }
    private PanelState currentState = PanelState.None;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>();

        inventory.OnInventoryChange += UpdateUI;
        CloseInventory();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChange -= UpdateUI;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleCancel();
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
        CloseAll();
        gameObject.SetActive(false);
    }

    public bool IsOpen() => isOpen;

    public void HandleCancel()
    {
        switch (currentState)
        {
            case PanelState.ActorSelect:
                CloseActorSelectPanel();
                OpenItemListPanel();
                break;
            case PanelState.AssignPopup:
                CloseAssignPopup();
                OpenItemListPanel();
                break;
            case PanelState.ItemList:
                CloseItemListPanel();
                OpenCategoryPanel();
                break;
            case PanelState.Category:
                CloseInventory();
                break;
        }
    }

    public void SetFilter(string filterName)
    {
        switch (filterName)
        {
            case "All": currentFilter = null; break;
            case "Items": currentFilter = ItemType.Consumable; break;
            case "Materials": currentFilter = ItemType.Material; break;
            case "Weapons": currentFilter = ItemType.Weapon; break;
            case "Armor": currentFilter = ItemType.Armor; break;
            case "Trinkets": currentFilter = ItemType.trinket; break;
            case "KeyItems": currentFilter = ItemType.Key; break;
            default: currentFilter = null; break;
        }

        OpenItemListPanel();
        UpdateUI();
    }

    public void OpenCategoryPanel()
    {
        CloseAll();
        categoryPanel.SetActive(true);
        currentState = PanelState.Category;
    }

    public void OpenItemListPanel()
    {
        CloseAll();
        itemListPanel.SetActive(true);
        currentState = PanelState.ItemList;
    }

    public void OpenActorSelectPanel(Inventory_Item item)
    {
        CloseAll();
        actorSelectPanel.SetActive(true);
        currentState = PanelState.ActorSelect;

        itemBeingAssigned = item;

        foreach (var button in actorButtons)
        {
            if (button != null && button.gameObject.activeSelf)
            {
                if (button.linkedPlayer != null)
                {
                    button.Setup((p) => OnPlayerSelectedFromActorPanel(p));
                }
                else
                {
                    Debug.LogWarning($"[Inventory] Button {button.name} is missing a linked player reference.");
                }
            }
        }

        Debug.Log($"[Inventory] Opened Actor Select for {item.itemData.itemName}");
    }

    public void OpenAssignPopup(Inventory_Item item)
    {
        CloseAll();
        assignPopupPanel.SetActive(true);
        currentState = PanelState.AssignPopup;

        itemBeingAssigned = item;

        if (assignAmountInput != null)
            assignAmountInput.text = "1";

        Debug.Log($"[Inventory] Opened Assign Popup for {item.itemData.itemName}");
    }

    private void CloseItemListPanel() => itemListPanel?.SetActive(false);
    private void CloseActorSelectPanel() => actorSelectPanel?.SetActive(false);
    private void CloseAssignPopup() => assignPopupPanel?.SetActive(false);

    private void CloseAll()
    {
        categoryPanel?.SetActive(false);
        itemListPanel?.SetActive(false);
        actorSelectPanel?.SetActive(false);
        assignPopupPanel?.SetActive(false);
        currentState = PanelState.None;
    }

    public void OnSearchInputChanged() => UpdateUI();

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
            if (currentFilter.HasValue)
            {
                if (item.itemData.itemType == currentFilter.Value)
                    filtered.Add(item);
            }
            else
            {
                filtered.Add(item);
            }
        }

        if (searchField != null && !string.IsNullOrEmpty(searchField.text))
        {
            string query = searchField.text.ToLower();
            filtered = filtered.FindAll(i => i.itemData.itemName.ToLower().Contains(query));
        }

        backpackSlotsParent.UpdateSlots(filtered);
    }

    public void OnPlayerSelectedFromActorPanel(Player targetPlayer)
    {
        if (itemBeingAssigned == null) return;

        Inventory_Item matchedItem = inventory.FindSameItem(itemBeingAssigned);
        if (matchedItem == null) return;

        if (matchedItem.itemEffect != null && matchedItem.itemEffect.CanBeUsed(targetPlayer))
        {
            matchedItem.itemEffect.Subscribe(targetPlayer);
            matchedItem.itemEffect.ExecuteEffect(targetPlayer);

            Debug.Log($"[Inventory] Used {matchedItem.itemData.itemName} on {targetPlayer.name}");

            inventory.RemoveOneItem(matchedItem);
            inventory.TriggerUpdateUI();

            UpdateActorSelectButtons();
        }
        else
        {
            Debug.Log($"[Inventory] {matchedItem.itemData.itemName} cannot be used on {targetPlayer.name}");
        }
    }

    private void UpdateActorSelectButtons()
    {
        var buttons = actorSelectPanel.GetComponentsInChildren<UI_CharacterProfileButton>(true);
        foreach (var button in buttons)
        {
            button.RefreshBars();
        }
    }

    // ------------------------------
    // 🔹 Assign to Quick Slot
    // ------------------------------

    public void AssignSlot1() => AssignToQuickSlot(1);
    public void AssignSlot2() => AssignToQuickSlot(2);
    public void AssignSlot3() => AssignToQuickSlot(3);
    public void AssignSlot4() => AssignToQuickSlot(4);

    private void AssignToQuickSlot(int slotNumber)
    {
        if (itemBeingAssigned == null) return;

        int amount = GetAssignAmount();
        if (amount <= 0) return;

        inventory.SetQuickItemInSlot(slotNumber, itemBeingAssigned, amount);
        Debug.Log($"[Inventory] Assigned {amount}x {itemBeingAssigned.itemData.itemName} to Slot {slotNumber}");

        itemBeingAssigned = null;
        assignPopupPanel.SetActive(false);
        OpenItemListPanel();
    }

    public void IncreaseAssignAmount()
    {
        if (itemBeingAssigned == null || assignAmountInput == null) return;

        int current = GetAssignAmount();
        current++;

        int totalOwned = 0;
        foreach (var item in inventory.itemList)
        {
            if (item.itemData == itemBeingAssigned.itemData)
                totalOwned += item.stackSize;
        }

        int assignedElsewhere = 0;
        foreach (var slot in inventory.quickSlots)
        {
            if (slot.item != null && slot.item.itemData == itemBeingAssigned.itemData)
                assignedElsewhere += slot.slotStack;
        }

        int maxAssignable = totalOwned;
        if (current > maxAssignable) current = maxAssignable;

        assignAmountInput.text = current.ToString();
    }

    public void DecreaseAssignAmount()
    {
        if (itemBeingAssigned == null || assignAmountInput == null) return;

        int current = GetAssignAmount();
        current = Mathf.Max(1, current - 1);

        assignAmountInput.text = current.ToString();
    }

    private int GetAssignAmount()
    {
        if (assignAmountInput == null) return 1;

        if (int.TryParse(assignAmountInput.text, out int result))
            return Mathf.Max(1, result);

        return 1;
    }


}
