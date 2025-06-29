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
        ui.StorageUI.gameObject.SetActive(true); // 🔑 activate first
        ui.StorageUI.SetupStorage(inventory, storage);
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
        //     ui.craftUI.gameObject.SetActive(false);
    }
}
