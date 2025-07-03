using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Object_ItemPickup : MonoBehaviour
{
    [SerializeField] private Vector2 dropForce = new Vector2(3, 10);
    [SerializeField] private ItemDataSO itemData;

    [Space]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;


    private void OnValidate()
    {
        if (itemData == null) return;

        sr = GetComponent<SpriteRenderer>();
        
        SetupVisuals();
    }

    public void SetupItem(ItemDataSO itemData)
    {
        this.itemData = itemData;
        SetupVisuals();
        
        float xDropForce = Random.Range(-dropForce.x, dropForce.x);
        rb.velocity = new Vector2(xDropForce, dropForce.y);
        col.isTrigger = false;
    }

    private void SetupVisuals()
    {
        sr.sprite = itemData.itemIcon;
        gameObject.name = "Object_ItemPickup - " + itemData.itemName;

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && col.isTrigger == false) { 
        }
        {
            col.isTrigger = true; // Switch to trigger mode
            rb.constraints = RigidbodyConstraints2D.FreezeAll; // Freeze movement
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        Inventory_Item itemToAdd = new Inventory_Item(itemData);
        
        var inventoryPlayer = collision.GetComponent<Inventory_Player>();
        if (inventoryPlayer == null) return;


        // ✅ Let Inventory_Player decide where it goes
        if (inventoryPlayer.CanAddItem(itemToAdd))
        {
            if (inventoryPlayer == null)
                return;
            Debug.Log($"[Pickup] Adding {itemToAdd.itemData.itemName} using Inventory_Player.AddItem.");
            inventoryPlayer.AddItem(itemToAdd);
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"[Pickup] No space for {itemToAdd.itemData.itemName}.");
        }
    }
}
