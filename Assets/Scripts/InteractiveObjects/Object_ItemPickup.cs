using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Object_ItemPickup : MonoBehaviour
{
    private SpriteRenderer sr;

    [SerializeField] private ItemDataSO itemData;

    private void OnValidate()
    {
        if (itemData == null) return;

        sr = GetComponent<SpriteRenderer>();
        sr.sprite = itemData.itemIcon;
        gameObject.name = "Object_ItemPickup - " + itemData.itemName;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var inventoryPlayer = collision.GetComponent<Inventory_Player>();
        if (inventoryPlayer == null) return;

        Inventory_Item itemToAdd = new Inventory_Item(itemData);

        if (itemData.itemType == ItemType.Material)
        {
            inventoryPlayer.storage.AddMaterialToStash(itemToAdd);
            Destroy(gameObject);
            return;
        }

        bool added = false;

        if (itemData.itemType == ItemType.Weapon ||
            itemData.itemType == ItemType.Armor ||
            itemData.itemType == ItemType.trinket)
        {
            if (inventoryPlayer.equipmentInventory.CanAddItem(itemToAdd))
            {
                inventoryPlayer.equipmentInventory.AddItem(itemToAdd);
                added = true;
            }
        }
        else
        {
            if (inventoryPlayer.CanAddItem(itemToAdd))
            {
                Debug.Log($"[Pickup] Adding {itemToAdd.itemData.itemName} to player inventory.");
                inventoryPlayer.AddItem(itemToAdd);
                added = true;
            }
            else
            {
                Debug.Log("[Pickup] Player cannot accept item, inventory full.");
            }
        }

        if (added)
        {
            Debug.Log($"[Pickup] Final: Added {itemToAdd.itemData.itemName}.");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("No space in inventories. Item not picked up.");
        }
    }


    private bool IsEquipment(ItemDataSO data)
    {
        return data.itemType == ItemType.Weapon ||
               data.itemType == ItemType.Armor ||
               data.itemType == ItemType.trinket;
    }
}
