using System.Collections.Generic;
using UnityEngine;

public class UI_EquipSlotParent : MonoBehaviour
{
    private UI_EquippedSlot[] equipSlots;

    public void UpdateEquipmentSlots(List<Inventory_Equipped> equipList)
    {
        if (equipSlots == null)
            equipSlots = GetComponentsInChildren<UI_EquippedSlot>();

        for (int i = 0; i < equipSlots.Length; i++)
        {
            var playerEquipSlot = equipList[i];

            if (playerEquipSlot.HasItem() == false)
                equipSlots[i].UpdateSlot(null);
            else
                equipSlots[i].UpdateSlot(playerEquipSlot.equipedItem);
        }
    }
}
