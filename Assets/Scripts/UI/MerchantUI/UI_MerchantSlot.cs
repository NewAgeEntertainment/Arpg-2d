using UnityEngine;
using UnityEngine.EventSystems;

public class UI_MerchantSlot : UI_ItemSlot
{
    private Inventory_Merchant merchant;
    private Inventory_Player inventory;

    public enum MerchantSlotType { MerchantSlot, PlayerSlot }
    public MerchantSlotType slotType;

    public void SetUpMerchantUI(Inventory_Merchant merchant, Inventory_Player inventory)
    {
        this.merchant = merchant;
        this.inventory = inventory;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null) return;

        bool rightClick = eventData.button == PointerEventData.InputButton.Right;
        bool leftClick = eventData.button == PointerEventData.InputButton.Left;

        if (slotType == MerchantSlotType.PlayerSlot)
        {
            if (rightClick)
            {
                bool sellFullStack = Input.GetKey(KeyCode.LeftControl);
                merchant.TrySellItem(itemInSlot, sellFullStack);
            }
            else if (leftClick)
            {
                if (itemInSlot.itemData.itemType == ItemType.Weapon ||
                    itemInSlot.itemData.itemType == ItemType.Armor ||
                    itemInSlot.itemData.itemType == ItemType.trinket)
                {
                    inventory.TryEquipItem(itemInSlot);
                }
                else
                {
                    inventory.TryUseItem(itemInSlot);
                }
            }
        }
        else if (slotType == MerchantSlotType.MerchantSlot)
        {
            if (leftClick)
            {
                bool buyFullStack = Input.GetKey(KeyCode.LeftControl);
                merchant.TryBuyItem(itemInSlot, buyFullStack);
            }
        }

        ui.itemToolTip.ShowToolTip(false, null);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInSlot == null) return;
        ui.itemToolTip.ShowToolTip(true, itemInSlot);
    }
}
