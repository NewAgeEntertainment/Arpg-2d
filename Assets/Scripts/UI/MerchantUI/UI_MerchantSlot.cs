using UnityEngine;
using UnityEngine.EventSystems;

public class UI_MerchantSlot : UI_ItemSlot
{
    private Inventory_Merchant merchant;
    private Inventory_Player inventory;

    [Header("Optional Equip Confirm UI")]
    [SerializeField] private GameObject equipConfirmPopup; // Drag your popup here!
    private Inventory_Item pendingItemToEquip;

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
                if (itemInSlot.itemData.itemType == ItemType.Consumable)
                {
                    inventory.TryUseItem(itemInSlot, inventory.GetComponent<Player>());
                }
                else
                {
                    Debug.Log($"[{itemInSlot.itemData.itemType}] cannot be used here. Equip in equipment panel.");
                }
            }
        }
        else if (slotType == MerchantSlotType.MerchantSlot)
        {
            if (leftClick)
            {
                bool buyFullStack = Input.GetKey(KeyCode.LeftControl);
                merchant.TryBuyItem(itemInSlot, buyFullStack);

                if (itemInSlot.itemData.itemType == ItemType.Weapon ||
                    itemInSlot.itemData.itemType == ItemType.Armor ||
                    itemInSlot.itemData.itemType == ItemType.trinket)
                {
                    // Gear → show confirm popup
                    pendingItemToEquip = itemInSlot;
                    if (equipConfirmPopup != null)
                        equipConfirmPopup.SetActive(true);
                }
            }
        }

        ui.itemToolTip.ShowToolTip(false, null);
    }

    public void ConfirmEquipYes()
    {
        if (pendingItemToEquip != null)
        {
            inventory.TryEquipFromEquipmentInventory(pendingItemToEquip);
            pendingItemToEquip = null;
        }

        if (equipConfirmPopup != null)
            equipConfirmPopup.SetActive(false);
    }

    public void ConfirmEquipNo()
    {
        pendingItemToEquip = null;

        if (equipConfirmPopup != null)
            equipConfirmPopup.SetActive(false);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInSlot == null) return;
        ui.itemToolTip.ShowToolTip(true, itemInSlot);
    }
}
