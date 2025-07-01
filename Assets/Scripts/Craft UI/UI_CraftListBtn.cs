using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_CraftListBtn : MonoBehaviour
{
    [SerializeField] private ItemListDataSO craftData;
    private UI_CraftSlot[] craftSlots;

    public void setCraftSlots(UI_CraftSlot[] craftSlots) => this.craftSlots = craftSlots;


    public void UpdateCraftSlots()
    {
        if (craftSlots == null)
        {
            Debug.LogWarning("Craft slots not set. Please set the craft slots before updating.");
            return;
        }

        foreach (var slot in craftSlots)
        {
            slot.gameObject.SetActive(false);
        }

        for (int i = 0; i < craftData.itemList.Length; i++)
        {
            ItemDataSO itemData = craftData.itemList[i];

            craftSlots[i].gameObject.SetActive(true);
            craftSlots[i].SetupButton(itemData);
        }
    }
}
