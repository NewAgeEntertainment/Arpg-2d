using UnityEngine;

public class Object_ItemPickup : MonoBehaviour
{
    private SpriteRenderer sr;

    [SerializeField] private ItemDataSO itemData;


   

    private void OnValidate()
    {

        if (itemData == null)
        {
            return;
        }

        sr = GetComponent<SpriteRenderer>();
        sr.sprite = itemData.itemIcon;
        gameObject.name = "Object_ItemPickup - " + itemData.itemName;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Inventory_Item itemToAdd = new Inventory_Item(itemData);
        Inventory_Player inventory = collision.GetComponent<Inventory_Player>();
        Inventory_Storage storage = inventory.storage;

        if (itemData.itemType == ItemType.Material)
        {
            storage.AddMaterialToStash(itemToAdd);
            Destroy(gameObject);
            return;
        }


        if (inventory.CanAddItem(itemToAdd))
        {
            inventory.AddItem(itemToAdd);
            Destroy(gameObject);
        }
        
    }
}
