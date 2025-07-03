using UnityEngine;
using System.Collections.Generic;

public class UI_ItemSlotParent : MonoBehaviour
{
    private UI_ItemSlot[] slots;

    private void Awake()
    {
        slots = GetComponentsInChildren<UI_ItemSlot>(true);
    }

    public void UpdateSlots(List<Inventory_Item> itemList)
    {
        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning($"[UI_ItemSlotParent] No slots found under: {gameObject.name}");
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < itemList.Count)
                slots[i].UpdateSlot(itemList[i]);
            else
                slots[i].UpdateSlot(null);
        }
    }
}
