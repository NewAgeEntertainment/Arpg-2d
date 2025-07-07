using UnityEngine;

public class UI_ItemSlotParent : MonoBehaviour
{
    [SerializeField] private UI_ItemSlot[] slots;

    private void Awake()
    {
        slots = GetComponentsInChildren<UI_ItemSlot>(true);
    }

    public void UpdateSlots(System.Collections.Generic.List<Inventory_Item> items)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < items.Count)
                slots[i].UpdateSlot(items[i]);
            else
                slots[i].Clear();
        }
    }
}
