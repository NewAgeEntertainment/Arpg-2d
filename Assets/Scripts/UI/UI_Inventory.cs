using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class UI_Inventory : MonoBehaviour
{
    private Inventory_Player inventory;
    private UI_EquippedSlot[] uiEquipSlots;

    [SerializeField] private UI_ItemSlotParent inventorySlotsParent;
    [SerializeField] private Transform uiEquipSlotParent;

    [Header("Input Settings")]
    [SerializeField] private int playerId = 0;
    [SerializeField] private Rewired.Player rplayer; // Rewired player instance for input handling
    [SerializeField] private string toggleAction = "Inventory"; // Set up in Rewired
    private Player player;

    private void Awake()
    {
        uiEquipSlots = uiEquipSlotParent.GetComponentsInChildren<UI_EquippedSlot>();

        inventory = FindFirstObjectByType<Inventory_Player>();
        inventory.OnInventoryChange += UpdateUI;

        rplayer = ReInput.players.GetPlayer(playerId);

        UpdateUI();
        gameObject.SetActive(false); // Start hidden
    }

    private void Update()
    {
        if (rplayer.GetButtonDown(toggleAction))
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        bool isActive = !gameObject.activeSelf;
        gameObject.SetActive(isActive);

        if (isActive)
            UpdateUI();
    }

    private void UpdateUI()
    {
        inventorySlotsParent.UpdateSlots(inventory.itemList);
        UpdateEquipmentSlots();
    }

    private void UpdateEquipmentSlots()
    {
        List<Inventory_Equipped> playerEquipList = inventory.equipList;

        for (int i = 0; i < uiEquipSlots.Length; i++)
        {
            var playerEquipSlot = playerEquipList[i];

            if (playerEquipSlot.HasItem() == false)
                uiEquipSlots[i].UpdateSlot(null);
            else
                uiEquipSlots[i].UpdateSlot(playerEquipSlot.equipedItem);
        }
    }

    
}
