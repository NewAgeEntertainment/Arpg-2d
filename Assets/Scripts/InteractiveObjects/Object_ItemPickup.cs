using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class Object_ItemPickup : MonoBehaviour
{
    [Header("Assign this if you want to spawn an item directly")]
    public ItemDataSO itemToAssign;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Sprite goldSprite;

    [Header("Pickup Settings")]
    [SerializeField] private bool isGold = false;
    [SerializeField] private int goldAmount = 0;
    [SerializeField] private float flyToPlayerDistance = 2f;
    [SerializeField] private float flySpeed = 5f;
    [SerializeField] private float hoverHeight = 0.2f;
    [SerializeField] private float hoverSpeed = 2f;

    [Header("Shadow")]
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private float maxShadowScale = 1f;
    [SerializeField] private float minShadowScale = 0.3f;

    [Header("Hop Animation")]
    [SerializeField] private AnimationCurve hopArc;
    [SerializeField] private float hopDuration = 0.4f;
    [SerializeField] private float hopHeight = 1f;

    [Header("Burst Settings")]
    [SerializeField] private float burstDistance = 3f;

    private Transform player;
    private ItemDataSO itemData;
    private Vector3 startPos;
    private Vector3 desiredSpawnOffset;
    private bool isBursting = false;
    private bool hasSettledFromBurst = false;

    private void Start()
    {
        if (iconRenderer == null)
            iconRenderer = GetComponent<SpriteRenderer>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        player = FindAnyObjectByType<Player>()?.transform;
        startPos = transform.position;

        if (itemToAssign != null)
        {
            SetupItem(itemToAssign);
        }
    }

    void Update()
    {
        if (!isBursting)
        {
            // Hover effect
            float hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            transform.position = new Vector3(startPos.x, startPos.y + hoverOffset, startPos.z);

            // Shadow scaling based on hover height
            if (shadowTransform != null)
            {
                float normalizedHeight = (hoverOffset + hoverHeight) / (2 * hoverHeight);
                float shadowScale = Mathf.Lerp(minShadowScale, maxShadowScale, 1f - normalizedHeight);
                shadowTransform.localScale = new Vector3(shadowScale, shadowScale, 1);
            }
        }

        // Fly to player logic (if nearby)
        if (player != null && Vector2.Distance(transform.position, player.position) < flyToPlayerDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, flySpeed * Time.deltaTime);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (itemToAssign != null && iconRenderer != null)
        {
            iconRenderer.sprite = itemToAssign.itemIcon;
        }
    }
#endif


    public void ApplyBurst(Vector2 direction)
    {
        Vector3 endPoint = transform.position + (Vector3)(direction.normalized * burstDistance);
        StartCoroutine(HopToPosition(endPoint));
    }

    private IEnumerator HopToPosition(Vector3 end)
    {
        isBursting = true;
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < hopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hopDuration);
            float arc = hopArc.Evaluate(t) * hopHeight;

            Vector3 flatPos = Vector3.Lerp(start, end, t);
            transform.position = new Vector3(flatPos.x, flatPos.y + arc, flatPos.z);

            if (shadowTransform != null)
            {
                float shadowScale = Mathf.Lerp(minShadowScale, maxShadowScale, 1 - (arc / hopHeight));
                shadowTransform.localScale = new Vector3(shadowScale, shadowScale, 1);
            }

            yield return null;
        }

        transform.position = end;
        startPos = end;
        isBursting = false;

        // Reset shadow scale
        if (shadowTransform != null)
            shadowTransform.localScale = new Vector3(maxShadowScale, maxShadowScale, 1);
    }

    public void SetupItem(ItemDataSO item)
    {
        isGold = false;
        itemData = item;
        if (iconRenderer != null && itemData != null)
        {
            if (itemData.itemIcon != null)
                iconRenderer.sprite = itemData.itemIcon;
            else
                Debug.LogWarning($"[Pickup] Item {itemData.name} is missing an icon!");
        }

    }

    public void SetupGold(int amount)
    {
        isGold = true;
        goldAmount = amount;
        if (iconRenderer != null && goldSprite != null)
            iconRenderer.sprite = goldSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Inventory_Player inv = other.GetComponent<Inventory_Player>();
        if (inv == null) return;

        if (isGold)
            inv.AddGold(goldAmount);
        else if (itemData != null)
            inv.AddItem(new Inventory_Item(itemData));

        Destroy(gameObject);
    }
}
