using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object_Merchant : Object_NPC, IInteractable
{
    private Inventory_Player inventory;
    private Inventory_Merchant merchant;

    protected override void Awake()
    {
        base.Awake();
        merchant = GetComponent<Inventory_Merchant>();
    }

    protected override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Z))
            merchant.FillShopList(); // For testing: refill the merchant's inventory when pressing Z
    }

    public void Interact()
    {
        // ✅ Setup slots
        ui.MerchantUI.SetUpMerchantUI(merchant, inventory);

        // ✅ Always open using your UI manager → handles isMerchantOpen + input!
        ui.OpenMerchant();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        inventory = player.GetComponent<Inventory_Player>();
        merchant.SetInventory(inventory);
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        base.OnTriggerExit2D(collision);
        ui.SwitchOffAllToolTips();

        // ✅ Use the UI manager to close properly
        ui.CloseMerchant();
    }
}
