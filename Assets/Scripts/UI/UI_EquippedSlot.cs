using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EquippedSlot : UI_ItemSlot
{
    public ItemType slotType;

    private void OnValidate()
    {
        gameObject.name = "UI_EquipmentSlot - " + slotType.ToString();
    }

    public override void UpdateSlot(Inventory_Item item)
    {
        base.UpdateSlot(item); // ✅ ensures the icon and text are updated
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null)
            return;

        inventory.UnequipItem(itemInSlot); // ✅ handles unequip logic

        //// Optional: force UI refresh
        //ui.TriggerInventoryUIUpdate?.Invoke(); // if you’ve set it up
    }
}
