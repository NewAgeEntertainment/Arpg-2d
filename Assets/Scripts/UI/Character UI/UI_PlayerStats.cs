using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PlayerStats : MonoBehaviour
{
    private UI_StatSlot[] uiStatSlots;
    private Inventory_Player inventory;


    private void Awake()
    {
        uiStatSlots = GetComponentsInChildren<UI_StatSlot>();
        inventory = FindFirstObjectByType<Inventory_Player>();

        var playerStats = FindFirstObjectByType<Player_Stats>();
        foreach (var slot in uiStatSlots)
        {
            slot.Setup(playerStats);
        }

        inventory.OnInventoryChange += UpdateStatsUI;
    }


    private void Start()
    {
        UpdateStatsUI();
    }

    private void UpdateStatsUI()
    {
        foreach (var statSlot in uiStatSlots)
        {
            statSlot.UpdateStatValue();
        }
    }
}
