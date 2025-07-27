using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_Merchant : MonoBehaviour
{
    // --------------------------
    //  Data
    // --------------------------
    private Inventory_Player playerInventory;
    private Inventory_Merchant merchant;

    // --------------------------
    //  UI refs
    // --------------------------
    [Header("Money")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Panels (flow)")]
    [SerializeField] private GameObject buySellPanel;   // Buy/Sell choice screen
    [SerializeField] private GameObject buyListPanel;   // Merchant item list
    [SerializeField] private GameObject sellListPanel;  // Player item list
    [SerializeField] private GameObject shopStatPanel;  // Stat panel (optional)
    [SerializeField] private UI_MerchantQuantityPanel quantityPanel;  // Quantity panel

    [Header("Slot Parents")]
    [SerializeField] private UI_ItemSlotParent buySlotsParent;   // Merchant slots
    [SerializeField] private UI_ItemSlotParent sellSlotsParent;  // Player slots
    [SerializeField] private UI_EquipSlotParent equippedSlotsParent;

    [Header("Right Stat Panel")]
    [SerializeField] private UI_MerchantStatsPanel statsPanel;

    private readonly List<UI_MerchantSlot> cachedMerchantSlots = new();
    private readonly List<UI_MerchantSlot> cachedPlayerSlots = new();

    private enum MerchantState { None, BuySell, ShopList, Quantity }
    private MerchantState state = MerchantState.None;
    private bool isBuying = true; // true = Buy flow, false = Sell flow

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    // ------------------------------------------------------------------
    //  Setup & Closing
    // ------------------------------------------------------------------
    public void SetUpMerchantUI(Inventory_Merchant merchant, Inventory_Player playerInventory)
    {
        this.merchant = merchant;
        this.playerInventory = playerInventory;

        merchant.SetInventory(playerInventory);

        SubscribeInventories();
        ResetPanels();

        UpdateMoney();
        ShowPlayerStats();
        state = MerchantState.BuySell;
        isOpen = true;

        Debug.Log("[UI] Merchant UI opened");
    }

    public void CloseMerchant()
    {
        isOpen = false;
        state = MerchantState.None;

        ResetPanels();

        var ui = FindObjectOfType<UI>();
        ui?.StopPlayerControls(false);
        ui?.SwitchOffAllToolTips();

        gameObject.SetActive(false);
        Debug.Log("[UI] Merchant UI closed");
    }

    private void ResetPanels()
    {
        SetActiveSafe(buySellPanel, true);
        SetActiveSafe(buyListPanel, false);
        SetActiveSafe(sellListPanel, false);
        SetActiveSafe(shopStatPanel, false);
        SetActiveSafe(quantityPanel != null ? quantityPanel.gameObject : null, false);
    }

    // ------------------------------------------------------------------
    //  Flow Buttons
    // ------------------------------------------------------------------
    public void OnClickBuy()
    {
        isBuying = true;

        SetActiveSafe(buySellPanel, false);
        SetActiveSafe(buyListPanel, true);
        SetActiveSafe(sellListPanel, false);
        SetActiveSafe(shopStatPanel, true);
        SetActiveSafe(quantityPanel != null ? quantityPanel.gameObject : null, false);

        buySlotsParent.UpdateSlots(merchant.itemList);
        FixSlotTypes(buySlotsParent, UI_MerchantSlot.MerchantSlotType.MerchantSlot);
        WireMerchantSlots(buySlotsParent, true);

        UpdateMoney();
        ShowPlayerStats();

        state = MerchantState.ShopList;
    }

    private void FixSlotTypes(UI_ItemSlotParent parent, UI_MerchantSlot.MerchantSlotType type)
    {
        if (parent == null) return;

        var slots = parent.GetComponentsInChildren<UI_MerchantSlot>(true);
        for (int i = 0; i < slots.Length; i++)
            slots[i].slotType = type;
    }


    public void OnClickSell()
    {
        isBuying = false;

        SetActiveSafe(buySellPanel, false);
        SetActiveSafe(buyListPanel, false);
        SetActiveSafe(sellListPanel, true);
        SetActiveSafe(shopStatPanel, true);
        SetActiveSafe(quantityPanel != null ? quantityPanel.gameObject : null, false);

        var items = GetPlayerAllItems();
        sellSlotsParent.UpdateSlots(items);
        FixSlotTypes(sellSlotsParent, UI_MerchantSlot.MerchantSlotType.PlayerSlot);
        WirePlayerSlots(sellSlotsParent, false);

        UpdateMoney();
        ShowPlayerStats();

        state = MerchantState.ShopList;
    }

    // ------------------------------------------------------------------
    //  Cancel Stack
    // ------------------------------------------------------------------
    public bool HandleCancel()
    {
        switch (state)
        {
            case MerchantState.Quantity:
                quantityPanel.gameObject.SetActive(false);
                if (isBuying) buyListPanel.SetActive(true);
                else sellListPanel.SetActive(true);
                state = MerchantState.ShopList;
                return true;

            case MerchantState.ShopList:
                buyListPanel.SetActive(false);
                sellListPanel.SetActive(false);
                shopStatPanel.SetActive(false);
                buySellPanel.SetActive(true);
                state = MerchantState.BuySell;
                return true;

            case MerchantState.BuySell:
                CloseMerchant();
                return true;
        }

        return false;
    }

    // ------------------------------------------------------------------
    //  Hover & Quantity
    // ------------------------------------------------------------------
    private void OnItemClicked_OpenQuantity(Inventory_Item item, bool isBuyFlow)
    {
        if (item == null || quantityPanel == null) return;

        if (isBuyFlow)
        {
            // Hide the buy list while quantity panel is open
            if (buyListPanel) buyListPanel.SetActive(false);
            if (quantityPanel) quantityPanel.gameObject.SetActive(true);

            quantityPanel.Open(item, merchant, playerInventory, OnConfirmBuy, OnCancelBuy, selling: false);
        }
        else
        {
            // Hide the sell list while quantity panel is open
            if (sellListPanel) sellListPanel.SetActive(false);
            if (quantityPanel) quantityPanel.gameObject.SetActive(true);

            quantityPanel.Open(item, merchant, playerInventory, OnConfirmSell, OnCancelSell, selling: true);
        }

        state = MerchantState.Quantity;
    }


    private void OnConfirmBuy(Inventory_Item item, int qty)
    {
        for (int i = 0; i < qty; i++)
            merchant.TryBuyItem(item, false);

        RefreshAfterTransaction();
        buyListPanel.SetActive(true);
        state = MerchantState.ShopList;
    }

    private void OnCancelBuy()
    {
        quantityPanel.gameObject.SetActive(false);
        buyListPanel.SetActive(true);
        state = MerchantState.ShopList;
    }

    private void OnConfirmSell(Inventory_Item item, int qty)
    {
        for (int i = 0; i < qty; i++)
            merchant.TrySellItem(item, false);

        RefreshAfterTransaction();
        sellListPanel.SetActive(true);
        state = MerchantState.ShopList;
    }

    private void OnCancelSell()
    {
        quantityPanel.gameObject.SetActive(false);
        sellListPanel.SetActive(true);
        state = MerchantState.ShopList;
    }

    private void OnItemHovered(Inventory_Item item)
    {
        if (statsPanel == null) return;
        statsPanel.ShowItem(item, playerInventory);
    }

    private void OnItemExit()
    {
        ShowPlayerStats();
    }

    private void ShowPlayerStats()
    {
        if (statsPanel == null) return;
        statsPanel.ShowPlayer(playerInventory);
    }

    // ------------------------------------------------------------------
    //  Refresh & Wiring
    // ------------------------------------------------------------------
    private void RefreshAfterTransaction()
    {
        UpdateSlotUI();
        quantityPanel.gameObject.SetActive(false);

        buySlotsParent.UpdateSlots(merchant.itemList);
        sellSlotsParent.UpdateSlots(GetPlayerAllItems());

        WireMerchantSlots(buySlotsParent, true);
        WirePlayerSlots(sellSlotsParent, false);

        UpdateMoney();
        ShowPlayerStats();
    }

    private void UpdateSlotUI()
    {
        if (playerInventory == null || merchant == null) return;

        UpdateMoney();
        if (equippedSlotsParent != null)
            equippedSlotsParent.UpdateEquipmentSlots(playerInventory.equipList);
    }

    private void UpdateMoney()
    {
        if (goldText)
            goldText.text = playerInventory.gold.ToString("N0") + "g.";
    }

    private List<Inventory_Item> GetPlayerAllItems()
    {
        var combined = new List<Inventory_Item>();
        combined.AddRange(playerInventory.itemList);
        combined.AddRange(playerInventory.equipmentInventory.itemList);
        foreach (var slot in playerInventory.equipList)
            if (slot.HasItem()) combined.Add(slot.equipedItem);
        return combined;
    }

    private void WireMerchantSlots(UI_ItemSlotParent parent, bool buyFlow)
    {
        ClearSlotEvents(cachedMerchantSlots);

        foreach (var slot in parent.GetComponentsInChildren<UI_MerchantSlot>(true))
        {
            slot.SetUpMerchantUI(merchant, playerInventory);
            slot.onHover += OnItemHovered;
            slot.onExit += OnItemExit;
            slot.onClick += (item) => OnItemClicked_OpenQuantity(item, buyFlow);
            cachedMerchantSlots.Add(slot);
        }
    }

    private void WirePlayerSlots(UI_ItemSlotParent parent, bool buyFlow)
    {
        ClearSlotEvents(cachedPlayerSlots);

        foreach (var slot in parent.GetComponentsInChildren<UI_MerchantSlot>(true))
        {
            slot.SetUpMerchantUI(merchant, playerInventory);
            slot.onHover += OnItemHovered;
            slot.onExit += OnItemExit;
            slot.onClick += (item) => OnItemClicked_OpenQuantity(item, buyFlow);
            cachedPlayerSlots.Add(slot);
        }
    }

    private void ClearSlotEvents(List<UI_MerchantSlot> cache)
    {
        foreach (var slot in cache)
        {
            slot.onHover -= OnItemHovered;
            slot.onExit -= OnItemExit;
        }
        cache.Clear();
    }

    private void SubscribeInventories()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChange += UpdateSlotUI;
            if (playerInventory.equipmentInventory != null)
                playerInventory.equipmentInventory.OnInventoryChange += UpdateSlotUI;
        }
        if (merchant != null)
            merchant.OnInventoryChange += UpdateSlotUI;
    }

    private void UnsubscribeInventories()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChange -= UpdateSlotUI;
            if (playerInventory.equipmentInventory != null)
                playerInventory.equipmentInventory.OnInventoryChange -= UpdateSlotUI;
        }
        if (merchant != null)
            merchant.OnInventoryChange -= UpdateSlotUI;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
