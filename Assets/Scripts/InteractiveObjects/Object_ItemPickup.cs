using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Object_ItemPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private ItemDataSO itemData;
    [SerializeField] private Collider2D pickupCollider;
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("Floating Settings")]
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatFrequency = 2f;

    [Header("Attraction Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float flySpeed = 4f;

    private Vector3 basePosition;
    private Transform playerTransform;
    private bool isFlyingToPlayer = false;

    private void Awake()
    {
        if (iconRenderer == null)
            iconRenderer = GetComponent<SpriteRenderer>();

        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider2D>();

        if (pickupCollider != null)
            pickupCollider.isTrigger = true;

        if (itemData != null && iconRenderer != null)
            iconRenderer.sprite = itemData.itemIcon;

        basePosition = transform.position;
    }

    private void Update()
    {
        if (isFlyingToPlayer)
        {
            FlyTowardPlayer();
        }
        else
        {
            FloatMotion();
            CheckPlayerDistance();
        }
    }

    private void FloatMotion()
    {
        Vector3 floatOffset = Vector3.up * Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = basePosition + floatOffset;
    }

    private void CheckPlayerDistance()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < pickupRange)
            {
                isFlyingToPlayer = true;
            }
        }
    }

    private void FlyTowardPlayer()
    {
        if (playerTransform == null) return;

        // Disable float, just move toward player
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, flySpeed * Time.deltaTime);
    }

    public void SetupItem(ItemDataSO data)
    {
        itemData = data;

        if (iconRenderer != null && itemData != null)
            iconRenderer.sprite = itemData.itemIcon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Inventory_Player playerInventory = other.GetComponent<Inventory_Player>();
        if (playerInventory == null || itemData == null) return;

        if (itemData.itemType == ItemType.Gold)
        {
            playerInventory.AddGold(itemData.itemPtice);
        }
        else
        {
            playerInventory.AddItem(new Inventory_Item(itemData));
        }

        Destroy(gameObject);
    }
}
