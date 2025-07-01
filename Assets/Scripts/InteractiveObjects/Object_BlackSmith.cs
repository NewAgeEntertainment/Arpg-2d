using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object_BlackSmith : Object_NPC, IInteractable
{
    private Animator anim;
    private Inventory_Player inventory;
    private Inventory_Storage storage;


    protected override void Awake()
    {
        base.Awake();
        storage = GetComponent<Inventory_Storage>();
        anim = GetComponentInChildren<Animator>();
        anim.SetBool("isBlacksmith", true);
        
    }

    public void Interact()
    {
        Debug.Log("[BlackSmith] Interact()");

        // 🔑 1. Ensure player inventory is linked
        if (inventory == null)
            inventory = player.GetComponent<Inventory_Player>();

        storage.SetInventory(inventory);

        // 🔑 2. Open Storage first
        if (ui.StorageUI != null)
        {
            if (!ui.StorageUI.gameObject.activeSelf)
            {
                ui.StorageUI.gameObject.SetActive(true);
                Debug.Log("[BlackSmith] Activated StorageUI");
            }

            ui.StorageUI.SetupStorageUI(storage);
        }

        // 🔑 3. Open Craft next
        if (ui.craftUI != null)
        {
            if (!ui.craftUI.gameObject.activeSelf)
            {
                ui.craftUI.gameObject.SetActive(true);
                Debug.Log("[BlackSmith] Activated CraftUI");
            }

            ui.craftUI.SetupCraftUI(storage);
        }

        Debug.Log("[BlackSmith] Both Storage & Craft set up & visible!");
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

        if (ui != null)
            ui.SwitchOffAllToolTips();

        if (ui != null && ui.StorageUI != null)
            ui.StorageUI.gameObject.SetActive(false);

        // Same if you bring back crafting:
        // if (ui != null && ui.craftUI != null)
           ui.craftUI.gameObject.SetActive(false);
    }
}
