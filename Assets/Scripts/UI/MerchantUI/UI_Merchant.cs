using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Slot Parents")]
    [SerializeField] private UI_ItemSlotParent buySlotsParent;   // merchant list
    [SerializeField] private UI_ItemSlotParent sellSlotsParent;  // player list
    [SerializeField] private UI_EquipSlotParent equippedSlotsParent;

    [Header("Right Stat Panel")]
    [SerializeField] private UI_MerchantStatsPanel statsPanel;

    // --------------------------
    //  Simple Category Buttons (optional – you can also wire calls via Inspector)
    // --------------------------
    [Header("Buy Category Buttons (optional)")]
    [SerializeField] private Button buyAllBtn;
    [SerializeField] private Button buyWeaponsBtn;
    [SerializeField] private Button buyArmorBtn;
    [SerializeField] private Button buyAccessoryBtn;   // trinket
    [SerializeField] private Button buyMaterialsBtn;
    [SerializeField] private Button buyConsumablesBtn;

    [Header("Sell Category Buttons (optional)")]
    [SerializeField] private Button sellAllBtn;
    [SerializeField] private Button sellWeaponsBtn;
    [SerializeField] private Button sellArmorBtn;
    [SerializeField] private Button sellAccessoryBtn;  // trinket
    [SerializeField] private Button sellMaterialsBtn;
    [SerializeField] private Button sellConsumablesBtn;

    // current active filter (null/empty => All)
    private ItemType[] currentFilterTypes = null;

    private readonly List<UI_MerchantSlot> cachedMerchantSlots = new();
    private readonly List<UI_MerchantSlot> cachedPlayerSlots = new();

    private enum MerchantState { None, BuySell, List }
    private MerchantState state = MerchantState.None;

    private bool isBuying = true; // true = Buy flow, false = Sell flow
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    #region Unity
    private void Awake()
    {
        // Optional auto‑wiring of category buttons
        // BUY
        if (buyAllBtn) buyAllBtn.onClick.AddListener(OnClickBuyCategory_All);
        if (buyWeaponsBtn) buyWeaponsBtn.onClick.AddListener(OnClickBuyCategory_Weapons);
        if (buyArmorBtn) buyArmorBtn.onClick.AddListener(OnClickBuyCategory_Armor);
        if (buyAccessoryBtn) buyAccessoryBtn.onClick.AddListener(OnClickBuyCategory_Accessory);
        if (buyMaterialsBtn) buyMaterialsBtn.onClick.AddListener(OnClickBuyCategory_Materials);
        if (buyConsumablesBtn) buyConsumablesBtn.onClick.AddListener(OnClickBuyCategory_Consumables);

        // SELL
        if (sellAllBtn) sellAllBtn.onClick.AddListener(OnClickSellCategory_All);
        if (sellWeaponsBtn) sellWeaponsBtn.onClick.AddListener(OnClickSellCategory_Weapons);
        if (sellArmorBtn) sellArmorBtn.onClick.AddListener(OnClickSellCategory_Armor);
        if (sellAccessoryBtn) sellAccessoryBtn.onClick.AddListener(OnClickSellCategory_Accessory);
        if (sellMaterialsBtn) sellMaterialsBtn.onClick.AddListener(OnClickSellCategory_Materials);
        if (sellConsumablesBtn) sellConsumablesBtn.onClick.AddListener(OnClickSellCategory_Consumables);
    }

    private void OnDisable()
    {
        UnsubscribeInventories();
        ClearSlotEvents(cachedMerchantSlots);
        ClearSlotEvents(cachedPlayerSlots);
    }
    #endregion

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

        // default to All
        currentFilterTypes = null;

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
    }

    // ------------------------------------------------------------------
    //  Flow Buttons
    // ------------------------------------------------------------------
    public void OnClickBuy()
    {
        isBuying = true;
        currentFilterTypes = null; // default "All"

        SetActiveSafe(buySellPanel, false);
        SetActiveSafe(buyListPanel, true);
        SetActiveSafe(sellListPanel, false);
        SetActiveSafe(shopStatPanel, true);

        RefreshBuyList();

        state = MerchantState.List;
    }

    public void OnClickSell()
    {
        isBuying = false;
        currentFilterTypes = null; // default "All"

        SetActiveSafe(buySellPanel, false);
        SetActiveSafe(buyListPanel, false);
        SetActiveSafe(sellListPanel, true);
        SetActiveSafe(shopStatPanel, true);

        RefreshSellList();

        state = MerchantState.List;
    }

    // ------------------------------------------------------------------
    //  BUY Category Buttons
    // ------------------------------------------------------------------
    public void OnClickBuyCategory_All() { currentFilterTypes = null; RefreshBuyList(); }
    public void OnClickBuyCategory_Weapons() { currentFilterTypes = new[] { ItemType.Weapon }; RefreshBuyList(); }
    public void OnClickBuyCategory_Armor() { currentFilterTypes = new[] { ItemType.Armor }; RefreshBuyList(); }
    public void OnClickBuyCategory_Accessory() { currentFilterTypes = new[] { ItemType.trinket }; RefreshBuyList(); }
    public void OnClickBuyCategory_Materials() { currentFilterTypes = new[] { ItemType.Material }; RefreshBuyList(); }
    public void OnClickBuyCategory_Consumables() { currentFilterTypes = new[] { ItemType.Consumable }; RefreshBuyList(); }

    // ------------------------------------------------------------------
    //  SELL Category Buttons
    // ------------------------------------------------------------------
    public void OnClickSellCategory_All() { currentFilterTypes = null; RefreshSellList(); }
    public void OnClickSellCategory_Weapons() { currentFilterTypes = new[] { ItemType.Weapon }; RefreshSellList(); }
    public void OnClickSellCategory_Armor() { currentFilterTypes = new[] { ItemType.Armor }; RefreshSellList(); }
    public void OnClickSellCategory_Accessory() { currentFilterTypes = new[] { ItemType.trinket }; RefreshSellList(); }
    public void OnClickSellCategory_Materials() { currentFilterTypes = new[] { ItemType.Material }; RefreshSellList(); }
    public void OnClickSellCategory_Consumables() { currentFilterTypes = new[] { ItemType.Consumable }; RefreshSellList(); }

    // ------------------------------------------------------------------
    //  Cancel stack
    // ------------------------------------------------------------------
    public bool HandleCancel()
    {
        switch (state)
        {
            case MerchantState.List:
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
    //  Hover -> stats
    // ------------------------------------------------------------------
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
    private void RefreshBuyList()
    {
        FixSlotTypes(buySlotsParent, UI_MerchantSlot.MerchantSlotType.MerchantSlot);
        PreWireSlots(buySlotsParent, isBuyingFlow: true);

        var filtered = FilterItems(merchant.itemList, currentFilterTypes);
        buySlotsParent.UpdateSlots(filtered);

        WireMerchantSlots(buySlotsParent, true);

        UpdateMoney();
        ShowPlayerStats();
    }

    private void RefreshSellList()
    {
        FixSlotTypes(sellSlotsParent, UI_MerchantSlot.MerchantSlotType.PlayerSlot);
        PreWireSlots(sellSlotsParent, isBuyingFlow: false);

        var items = GetPlayerAllItems();
        var filtered = FilterItems(items, currentFilterTypes);
        sellSlotsParent.UpdateSlots(filtered);

        WirePlayerSlots(sellSlotsParent, false);

        UpdateMoney();
        ShowPlayerStats();
    }

    private void RefreshAllListsAfterTransaction()
    {
        UpdateSlotUI();

        if (isBuying)
            RefreshBuyList();
        else
            RefreshSellList();
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

    private List<Inventory_Item> FilterItems(List<Inventory_Item> source, ItemType[] filter)
    {
        if (source == null) return new List<Inventory_Item>();
        if (filter == null || filter.Length == 0)   // All
            return new List<Inventory_Item>(source);

        var set = new HashSet<ItemType>(filter);
        var res = new List<Inventory_Item>(source.Count);
        foreach (var it in source)
        {
            if (it == null || it.itemData == null) continue;
            if (set.Contains(it.itemData.itemType))
                res.Add(it);
        }
        return res;
    }

    private void PreWireSlots(UI_ItemSlotParent parent, bool isBuyingFlow)
    {
        foreach (var slot in parent.GetComponentsInChildren<UI_MerchantSlot>(true))
            slot.SetUpMerchantUI(merchant, playerInventory, isBuyingFlow);
    }

    private void WireMerchantSlots(UI_ItemSlotParent parent, bool buyingFlow)
    {
        ClearSlotEvents(cachedMerchantSlots);

        foreach (var slot in parent.GetComponentsInChildren<UI_MerchantSlot>(true))
        {
            slot.onHover += OnItemHovered;
            slot.onExit += OnItemExit;
            slot.OnTransactionFinished = RefreshAllListsAfterTransaction;

            cachedMerchantSlots.Add(slot);
        }
    }

    private void WirePlayerSlots(UI_ItemSlotParent parent, bool buyingFlow)
    {
        ClearSlotEvents(cachedPlayerSlots);

        foreach (var slot in parent.GetComponentsInChildren<UI_MerchantSlot>(true))
        {
            slot.onHover += OnItemHovered;
            slot.onExit += OnItemExit;
            slot.OnTransactionFinished = RefreshAllListsAfterTransaction;

            cachedPlayerSlots.Add(slot);
        }
    }

    private void ClearSlotEvents(List<UI_MerchantSlot> cache)
    {
        foreach (var slot in cache)
        {
            if (slot == null) continue;
            slot.onHover -= OnItemHovered;
            slot.onExit -= OnItemExit;
            slot.OnTransactionFinished = null;
        }
        cache.Clear();
    }

    private void FixSlotTypes(UI_ItemSlotParent parent, UI_MerchantSlot.MerchantSlotType type)
    {
        var slots = parent.GetComponentsInChildren<UI_MerchantSlot>(true);
        foreach (var s in slots) s.slotType = type;
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
