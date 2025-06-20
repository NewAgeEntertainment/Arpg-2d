using UnityEngine;

public class Object_ItemPickup : MonoBehaviour
{
    private SpriteRenderer sr;

    [SerializeField] private ItemDataSO itemData;

    private Inventory_Item itemToAdd;// // The item to add to the inventory when picked up
    private Inventory_Base inventory;// // Reference to the player's inventory

    private void Awake()
    {
        itemToAdd = new Inventory_Item(itemData);
    }

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

        inventory = collision.GetComponent<Inventory_Base>();

        bool canAddItem = inventory.CanAddItem() || inventory.FindStackable(itemToAdd) != null;

        if (inventory != null && canAddItem)
        {
            inventory.AddItem(itemToAdd);
        }
            Destroy(gameObject);
        //}
    }
}
