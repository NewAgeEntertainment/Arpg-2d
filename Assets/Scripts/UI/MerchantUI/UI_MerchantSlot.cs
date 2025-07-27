using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UI_MerchantSlot : UI_ItemSlot
{
    private Inventory_Merchant merchant;
    private Inventory_Player inventory;

    [Header("Merchant-Specific UI")]
    [SerializeField] private TextMeshProUGUI priceText; // Assign in Inspector

    public enum MerchantSlotType { MerchantSlot, PlayerSlot }
    public MerchantSlotType slotType = MerchantSlotType.MerchantSlot;

    public event System.Action<Inventory_Item> onClick;
    public event System.Action<Inventory_Item> onHover;
    public event System.Action onExit;

    public void SetUpMerchantUI(Inventory_Merchant merchant, Inventory_Player inventory)
    {
        this.merchant = merchant;
        this.inventory = inventory;
    }

    public override void UpdateSlot(Inventory_Item item)
    {
        base.UpdateSlot(item);

        if (priceText != null)
        {
            if (item == null || item.itemData == null)
            {
                priceText.text = "";
                return;
            }

            if (slotType == MerchantSlotType.MerchantSlot)
            {
                // Show BUY price (per item)
                priceText.text = $"{item.buyPrice}g";
            }
            else if (slotType == MerchantSlotType.PlayerSlot)
            {
                // Show SELL price with stack size
                int totalSellPrice = Mathf.FloorToInt(item.sellPrice * item.stackSize);
                priceText.text = $"x{item.stackSize} - {totalSellPrice}g";
            }
        }
    }

    public override void Clear()
    {
        base.Clear();
        if (priceText != null)
            priceText.text = "";
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            onClick?.Invoke(itemInSlot);
        }
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInSlot != null)
        {
            onHover?.Invoke(itemInSlot);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        onExit?.Invoke();
    }
}
