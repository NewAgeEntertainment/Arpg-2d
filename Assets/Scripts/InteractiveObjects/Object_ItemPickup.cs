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

        // Handle Material → Storage
        if (itemData.itemType == ItemType.Material)
        {
            inventoryPlayer.storage.AddMaterialToStash(itemToAdd);
            Destroy(gameObject);
            return; // exit here
        }

        // Handle Equipment → Equipment Inventory
        if (inventoryPlayer.equipmentInventory.CanAddItem(itemToAdd))
        {
            inventoryPlayer.equipmentInventory.AddItem(itemToAdd);
            Debug.Log($"Added {itemToAdd.itemData.itemName} to Equipment Inventory.");
            Destroy(gameObject);
            return; // exit here!
        }
        else
        {
            Debug.LogWarning("Cannot add item to equipment inventory (full or invalid type).");
        }

        // Fallback: Add to base player inventory (e.g., Consumable)
        if (inventoryPlayer.CanAddItem(itemToAdd))
        {
            inventoryPlayer.AddItem(itemToAdd);
            Destroy(gameObject);
            return; // exit here
        }
        else
        {
            Debug.Log("No space in inventory.");
        }
    }


    private bool IsEquipment(ItemDataSO data)
    {
        return data.itemType == ItemType.Weapon ||
               data.itemType == ItemType.Armor ||
               data.itemType == ItemType.trinket;
    }
}
