using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Entity_DropManager : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private GameObject itemPickupPrefab; // Your item prefab with Object_ItemPickup
    [SerializeField] private ItemListDataSO dropData;
    [SerializeField] private int maxRarityAmount = 1200;
    [SerializeField] private int maxItemsToDrop = 3;
    [SerializeField] private float dropSpreadRadius = 1.5f;

    public void DropItems()
    {
        if (itemPickupPrefab == null || dropData == null || dropData.itemList == null)
        {
            Debug.LogWarning("[DropManager] Missing drop prefab or data.");
            return;
        }

        List<ItemDataSO> itemsToDrop = RollDrops();
        int amountToDrop = Mathf.Min(itemsToDrop.Count, maxItemsToDrop);

        for (int i = 0; i < amountToDrop; i++)
        {
            Vector3 dropPosition = transform.position + (Vector3)(Random.insideUnitCircle * dropSpreadRadius);
            GameObject itemObj = Instantiate(itemPickupPrefab, dropPosition, Quaternion.identity);

            Object_ItemPickup pickup = itemObj.GetComponent<Object_ItemPickup>();
            if (pickup != null)
            {
                pickup.SetupItem(itemsToDrop[i]);
            }
        }
    }

    private List<ItemDataSO> RollDrops()
    {
        List<ItemDataSO> possibleDrops = new List<ItemDataSO>();
        List<ItemDataSO> finalDrops = new List<ItemDataSO>();
        float currentRarity = maxRarityAmount;

        foreach (var item in dropData.itemList)
        {
            float dropChance = item.GetDropCance(); // Make sure this method exists in ItemDataSO

            if (Random.Range(0f, 100f) <= dropChance)
                possibleDrops.Add(item);
        }

        // Sort by rarity descending (optional)
        possibleDrops = possibleDrops.OrderByDescending(item => item.itemRarity).ToList();

        foreach (var item in possibleDrops)
        {
            if (currentRarity >= item.itemRarity)
            {
                finalDrops.Add(item);
                currentRarity -= item.itemRarity;
            }
        }

        return finalDrops;
    }
}
