using System.Collections.Generic;
using UnityEngine;

public class UI_EquipSlotParent : MonoBehaviour
{
    private UI_EquippedSlot[] equipSlots;

    private void CacheSlots()
    {
        if (equipSlots == null || equipSlots.Length == 0)
            equipSlots = GetComponentsInChildren<UI_EquippedSlot>(true);
    }

    public void UpdateEquipmentSlots(List<Inventory_Equipped> equipList)
    {
        CacheSlots();

        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (i >= equipList.Count || equipList[i] == null)
            {
                equipSlots[i].UpdateSlot(null);
                equipSlots[i].HighlightOff();
                continue;
            }

            var playerEquipSlot = equipList[i];

            if (!playerEquipSlot.HasItem())
                equipSlots[i].UpdateSlot(null);
            else
                equipSlots[i].UpdateSlot(playerEquipSlot.equipedItem);

            equipSlots[i].HighlightOff();
        }
    }

    public UI_EquippedSlot[] GetEquippedSlots()
    {
        CacheSlots();
        return equipSlots;
    }

    public void SetEquippedSlotInteractable(UI_EquippedSlot selectedSlot)
    {
        CacheSlots();

        foreach (var slot in equipSlots)
        {
            bool isSelected = slot == selectedSlot;
            slot.SetInteractable(isSelected);
        }
    }
}