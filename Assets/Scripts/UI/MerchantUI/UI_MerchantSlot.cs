using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using Rewired;

public class UI_MerchantSlot : UI_ItemSlot, IPointerEnterHandler, IPointerExitHandler
{
    public enum MerchantSlotType { MerchantSlot, PlayerSlot }
    [Header("Type")]
    public MerchantSlotType slotType = MerchantSlotType.MerchantSlot;

    [Header("Merchant Wiring")]
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI qtyText;
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private Button plusBtn;
    [SerializeField] private Button minusBtn;
    [SerializeField] private Button plus10Btn;
    [SerializeField] private Button minus10Btn;
    [SerializeField] private Button confirmBtn;

    [Header("Rewired")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string incAction = "Increase";
    [SerializeField] private string decAction = "Decrease";
    [SerializeField] private string inc10Action = "IncreaseByTen";
    [SerializeField] private string dec10Action = "DecreaseByTen";
    [SerializeField] private string confirmAction = "UIConfirm";

    private Rewired.Player rPlayer;

    private Inventory_Merchant merchant;
    private Inventory_Player playerInventory;

    private int quantity = 1;
    private int maxQuantity = 1;
    private bool isBuying = true; // true => BUY flow, false => SELL flow

    // So UI_Merchant can repaint after a transaction.
    public System.Action OnTransactionFinished;

    // Hover/focus events for right stat panel
    public event System.Action<Inventory_Item> onHover;
    public event System.Action onExit;

    // Whether we should listen to Rewired inputs (hover/focus)
    private bool hot = false;

    private void Awake()
    {
        // Rewired can be missing in editor play – guard it
        if (Rewired.ReInput.isReady)
            rPlayer = ReInput.players.GetPlayer(playerID);

        // Hook UI buttons (if wired in inspector)
        if (plusBtn) plusBtn.onClick.AddListener(() => ChangeQuantity(+1));
        if (minusBtn) minusBtn.onClick.AddListener(() => ChangeQuantity(-1));
        if (plus10Btn) plus10Btn.onClick.AddListener(() => ChangeQuantity(+10));
        if (minus10Btn) minus10Btn.onClick.AddListener(() => ChangeQuantity(-10));
        if (confirmBtn) confirmBtn.onClick.AddListener(Confirm);
    }

    private void Update()
    {
        if (!hot || rPlayer == null || itemInSlot == null) return;

        if (rPlayer.GetButtonDown(incAction)) ChangeQuantity(+1);
        if (rPlayer.GetButtonDown(decAction)) ChangeQuantity(-1);
        if (rPlayer.GetButtonDown(inc10Action)) ChangeQuantity(+10);
        if (rPlayer.GetButtonDown(dec10Action)) ChangeQuantity(-10);
        if (rPlayer.GetButtonDown(confirmAction)) Confirm();
    }

    /// <summary>
    /// Called by UI_Merchant right after UpdateSlots to prepare the slot for Buy/Sell flow.
    /// </summary>
    public void SetUpMerchantUI(Inventory_Merchant merchant, Inventory_Player player, bool isBuyingFlow)
    {
        this.merchant = merchant;
        this.playerInventory = player;
        this.isBuying = isBuyingFlow;
    }

    /// <summary>
    /// Optional if you want to give focus without hovering (e.g., with gamepad navigation).
    /// </summary>
    public void SetFocused(bool focused)
    {
        hot = focused;
        if (focused && itemInSlot != null)
            onHover?.Invoke(itemInSlot);
        else
            onExit?.Invoke();
    }

    public override void UpdateSlot(Inventory_Item item)
    {
        base.UpdateSlot(item);

        if (priceText != null) priceText.text = "";
        if (qtyText != null) qtyText.text = "";
        if (totalText != null) totalText.text = "";

        if (itemInSlot == null || itemInSlot.itemData == null)
        {
            SetButtonsInteractable(false);
            return;
        }

        SetButtonsInteractable(true);

        quantity = 1;

        // --------- NEW: guard if SetUpMerchantUI() didn't run yet -----------
        if (playerInventory == null || merchant == null)
        {
            // Fall back to a safe/default behaviour so we don't crash
            isBuying = (slotType == MerchantSlotType.MerchantSlot);
            maxQuantity = Mathf.Max(1, itemInSlot.stackSize);

            if (priceText)
            {
                if (isBuying)
                    priceText.text = $"{itemInSlot.buyPrice}g";
                else
                    priceText.text = $"{Mathf.FloorToInt(itemInSlot.sellPrice)}g";
            }

            RefreshQtyAndTotal();
            return;
        }
        // --------------------------------------------------------------------

        if (isBuying)
        {
            if (isBuying)
            {
                int unitPrice = itemInSlot.buyPrice;
                int byGold = unitPrice > 0 ? playerInventory.gold / unitPrice : 0;

                maxQuantity = Mathf.Max(1, byGold); // Remove byStack limitation

                if (priceText) priceText.text = $"{unitPrice}g";
            }

        }
        else
        {
            int unitSell = Mathf.FloorToInt(itemInSlot.sellPrice);
            maxQuantity = Mathf.Max(1, itemInSlot.stackSize);

            if (priceText)
            {
                int totalSell = unitSell * itemInSlot.stackSize;
                priceText.text = $"x{itemInSlot.stackSize} - {totalSell}g";
            }
        }

        RefreshQtyAndTotal();
    }


    public override void Clear()
    {
        base.Clear();
        if (priceText != null) priceText.text = "";
        if (qtyText != null) qtyText.text = "";
        if (totalText != null) totalText.text = "";
        SetButtonsInteractable(false);
        hot = false;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        hot = true;
        if (itemInSlot != null)
            onHover?.Invoke(itemInSlot);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        hot = false;
        onExit?.Invoke();
    }

    // -------------------------------------------------
    // Quantity logic
    // -------------------------------------------------
    private void ChangeQuantity(int delta)
    {
        if (itemInSlot == null) return;

        quantity += delta;
        quantity = Mathf.Clamp(quantity, 1, maxQuantity);

        RefreshQtyAndTotal();
    }

    private void RefreshQtyAndTotal()
    {
        if (qtyText) qtyText.text = quantity.ToString();

        int unit = isBuying ? itemInSlot.buyPrice : Mathf.FloorToInt(itemInSlot.sellPrice);
        int total = unit * quantity;

        if (totalText) totalText.text = $"{total}g";
    }

    private void Confirm()
    {
        if (itemInSlot == null || merchant == null || playerInventory == null) return;

        if (isBuying)
        {
            for (int i = 0; i < quantity; i++)
                merchant.TryBuyItem(itemInSlot, false);
        }
        else
        {
            for (int i = 0; i < quantity; i++)
                merchant.TrySellItem(itemInSlot, false);
        }

        OnTransactionFinished?.Invoke();
    }

    private void SetButtonsInteractable(bool on)
    {
        if (plusBtn) plusBtn.interactable = on;
        if (minusBtn) minusBtn.interactable = on;
        if (plus10Btn) plus10Btn.interactable = on;
        if (minus10Btn) minus10Btn.interactable = on;
        if (confirmBtn) confirmBtn.interactable = on;
    }
}
