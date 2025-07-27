using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rewired;

public class UI_MerchantQuantityPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI totalPriceText;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button confirmButton;

    [Header("Rewired")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string increaseAction = "Increase";
    [SerializeField] private string decreaseAction = "Decrease";
    [SerializeField] private string increaseByTenAction = "IncreaseByTen";
    [SerializeField] private string decreaseByTenAction = "DecreaseByTen";
    [SerializeField] private string confirmAction = "UIConfirm";
    [SerializeField] private string cancelAction = "UICancel";

    private Rewired.Player rPlayer;

    private Inventory_Item currentItem;
    private Inventory_Merchant currentMerchant;
    private Inventory_Player currentPlayer;

    private System.Action<Inventory_Item, int> onConfirm;
    private System.Action onCancel;

    private int quantity = 1;
    private int maxQuantity = 1;
    private bool isSellMode = false;

    private void Awake()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);

        if (increaseButton != null) increaseButton.onClick.AddListener(() => ChangeQuantity(1));
        if (decreaseButton != null) decreaseButton.onClick.AddListener(() => ChangeQuantity(-1));
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (rPlayer == null) return;

        if (rPlayer.GetButtonDown(increaseAction)) ChangeQuantity(1);
        if (rPlayer.GetButtonDown(decreaseAction)) ChangeQuantity(-1);
        if (rPlayer.GetButtonDown(increaseByTenAction)) ChangeQuantity(10);
        if (rPlayer.GetButtonDown(decreaseByTenAction)) ChangeQuantity(-10);
        if (rPlayer.GetButtonDown(confirmAction)) Confirm();
        if (rPlayer.GetButtonDown(cancelAction)) Cancel();
    }

    public void Open(Inventory_Item item, Inventory_Merchant merchant, Inventory_Player player,
                     System.Action<Inventory_Item, int> confirmCallback, System.Action cancelCallback,
                     bool selling = false)
    {
        currentItem = item;
        currentMerchant = merchant;
        currentPlayer = player;
        onConfirm = confirmCallback;
        onCancel = cancelCallback;
        isSellMode = selling;

        quantity = 1;
        maxQuantity = isSellMode ? item.stackSize : 99;

        RefreshUI();
        gameObject.SetActive(true);
    }

    private void RefreshUI()
    {
        if (currentItem == null) return;

        if (itemNameText != null)
            itemNameText.text = currentItem.itemData.itemName;

        UpdateQuantityText();
        UpdateTotalPrice();
    }

    private void UpdateQuantityText()
    {
        if (quantityText != null)
            quantityText.text = quantity.ToString();
    }

    private void UpdateTotalPrice()
    {
        int unitPrice = isSellMode
            ? Mathf.FloorToInt(currentItem.sellPrice)
            : currentItem.buyPrice;

        int totalPrice = unitPrice * quantity;
        if (totalPriceText != null)
            totalPriceText.text = $"{totalPrice}g";
    }

    private void ChangeQuantity(int delta)
    {
        quantity += delta;
        quantity = Mathf.Clamp(quantity, 1, maxQuantity);
        UpdateQuantityText();
        UpdateTotalPrice();
    }

    private void Confirm()
    {
        onConfirm?.Invoke(currentItem, quantity);
    }

    private void Cancel()
    {
        onCancel?.Invoke();
    }
}
