using UnityEngine;
using Rewired;

public class Object_Merchant : Object_NPC, IInteractable
{
    [Header("Rewired (optional – only for debug refill)")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string refillShopAction = "MerchantRefill"; // optional, for testing only
    private Rewired.Player rPlayer;

    private Inventory_Player inventory;
    private Inventory_Merchant merchant;

    protected override void Awake()
    {
        base.Awake();
        merchant = GetComponent<Inventory_Merchant>();
        rPlayer = ReInput.players.GetPlayer(playerID);
    }

    protected override void Update()
    {
        base.Update();

        // Optional testing refill (debug only)
        if (rPlayer != null && !string.IsNullOrEmpty(refillShopAction) && rPlayer.GetButtonDown(refillShopAction))
        {
            merchant.FillShopList();
            Debug.Log("[Object_Merchant] Refilled shop list via Rewired action.");
        }

        // DO NOT handle UICancel here. Let UI.cs call merchantUI.HandleCancel().
    }

    public void Interact()
    {
        // Open via the UI manager, which internally calls SetUpMerchantUI
        ui.OpenMerchant(merchant, inventory);
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

        // OPTIONAL: if you want auto-close when leaving the trigger, do it safely:
        // if (ui.MerchantUI != null && ui.MerchantUI.IsOpen)
        //     ui.MerchantUI.CloseMerchant();
        //
        // Otherwise, just leave it open and let the player close it with UICancel.
    }

    // in Object_Merchant
    public void DS_OpenShop()
    {
        // Make sure we have refs even if conversation started before trigger wired them.
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);

        if (merchant == null)
            merchant = GetComponent<Inventory_Merchant>();

        if (ui == null)
            ui = UI.Instance ?? FindFirstObjectByType<UI>(FindObjectsInactive.Include);

        if (merchant != null && inventory != null && ui != null)
        {
            // optional: refresh the shop list if you want
            // merchant.FillShopList();

            ui.OpenMerchant(merchant, inventory);
        }
        else
        {
            Debug.LogWarning("[Object_Merchant] DS_OpenShop: missing refs (merchant/inventory/ui).");
        }
    }

}
