using System.Collections.Generic;

public class Inventory_Equipment : Inventory_Base
{
    private readonly HashSet<ItemType> allowedEquipmentTypes = new()
    {
        ItemType.Weapon,
        ItemType.Armor,
        ItemType.trinket
    };

    // ✅ Remove hard limit logic
    public override void AddItem(Inventory_Item itemToAdd)
    {
        if (!IsEquipment(itemToAdd))
        {
            
            return;
        }

        base.AddItem(itemToAdd); // Will just add to the list, no limit
    }

    private bool IsEquipment(Inventory_Item item)
    {
        return item != null && allowedEquipmentTypes.Contains(item.itemData.itemType);
    }



    // Optional helper method
    public List<Inventory_Item> GetAllEquippedItems()
    {
            return new List<Inventory_Item>(itemList);
    }
}
