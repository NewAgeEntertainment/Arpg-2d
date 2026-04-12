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
        // Do not open crafting directly here.
        // Let Dialogue System call DS_OpenCraft().
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        if (player != null)
            inventory = player.GetComponent<Inventory_Player>();

        if (storage != null && inventory != null)
            storage.SetInventory(inventory);
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        base.OnTriggerExit2D(collision);
        ui.SwitchOffAllToolTips();

        // Optional:
        // if you want crafting to auto-close when leaving range, uncomment:
        // if (ui != null) ui.CloseCraft();
    }

    public void DS_OpenCraft()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);

        if (storage == null)
            storage = GetComponent<Inventory_Storage>();

        if (ui == null)
            ui = UI.Instance ?? FindFirstObjectByType<UI>(FindObjectsInactive.Include);

        if (storage != null && inventory != null && ui != null)
        {
            storage.SetInventory(inventory);
            ui.OpenCraft(storage);
        }
        else
        {
            Debug.LogWarning("[Object_Blacksmith] DS_OpenCraft: missing refs (storage/inventory/ui).");
        }
    }
}