using UnityEngine;

public class Object_Blacksmith : Object_NPC, IInteractable
{
    private Inventory_Player inventory;
    private Inventory_Storage storage;

    protected override void Awake()
    {
        base.Awake();
        storage = GetComponent<Inventory_Storage>();
    }

    public void Interact()
    {
        storage.SetInventory(player.GetComponent<Inventory_Player>());
        ui.CraftUI.SetupCraftUI(storage);

        ui.OpenCraft();
        ui.StopPlayerControls(true); // ✅ Freeze player input
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        inventory = player.GetComponent<Inventory_Player>();
        storage.SetInventory(inventory);
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        base.OnTriggerExit2D(collision);
        ui.SwitchOffAllToolTips();
        ui.CloseCraft();
        ui.StopPlayerControls(false); // ✅ Resume player input
    }

    public bool HandleCancel()
    {
        var ui = FindObjectOfType<UI>();
        if (ui != null && ui.CraftUI == this)
        {
            ui.CloseCraft();
            return true;
        }
        return false;
    }

}
