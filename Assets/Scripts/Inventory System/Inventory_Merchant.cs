using System.Collections.Generic;
using UnityEngine;

public enum MerchantType
{
    All,
    Weapons,
    Armor,
    Items,
    Trinkets
}

public class Inventory_Merchant : Inventory_Base
{
    [Header("Merchant Settings")]
    public MerchantType merchantType = MerchantType.All;

    private Inventory_Player playerInventory;

    [SerializeField] private ItemListDataSO shopData;
    [SerializeField] private int minItemsAmount = 4;

    protected override void Awake()
    {
        base.Awake();
        FillShopList();
    }

    public void SetInventory(Inventory_Player inventory) => playerInventory = inventory;

    public int GetItemPrice(Inventory_Item item, bool isBuying)
    {
        if (item == null) return 0;
        return isBuying ? item.buyPrice : Mathf.FloorToInt(item.sellPrice);
    }

    public void TryBuyItem(Inventory_Item itemToBuy, bool buyFullStack)
    {
        if (playerInventory == null)
        {
            Debug.LogError("[Merchant] No player inventory linked!");
            return;
        }

        int amountToBuy = buyFullStack ? itemToBuy.stackSize : 1;

        for (int i = 0; i < amountToBuy; i++)
        {
            if (playerInventory.gold < itemToBuy.buyPrice)
            {
                Debug.Log("Not enough money!");
                return;
            }

            bool added = false;

            if (itemToBuy.itemData.itemType == ItemType.Material)
            {
                if (playerInventory == null)
                {
                    Debug.LogError("[Merchant] playerInventory is null.");
                    return;
                }
                if (playerInventory.storage == null)
                {
                    Debug.LogError("[Merchant] playerInventory.storage is null.");
                    return;
                }
                if (itemToBuy == null)
                {
                    Debug.LogError("[Merchant] itemToBuy is null.");
                    return;
                }
                if (itemToBuy.itemData == null)
                {
                    Debug.LogError("[Merchant] itemToBuy.itemData is null.");
                    return;
                }

                added = playerInventory.storage.AddMaterialToStash(itemToBuy);
            }
            else
            {
                if (playerInventory.CanAddItem(itemToBuy))
                {
                    var itemToAdd = new Inventory_Item(itemToBuy.itemData);
                    added = playerInventory.AddItem(itemToAdd);
                }
            }

            if (added)
            {
                playerInventory.gold -= itemToBuy.buyPrice;
                RemoveOneItem(itemToBuy);
                playerInventory.TriggerUpdateUI();
            }
            else
            {
                Debug.LogWarning($"[Merchant] Could not add {itemToBuy.itemData.itemName} to inventory or storage.");
                return;
            }
        }

        NotifyInventoryChanged();
    }

    public void TrySellItem(Inventory_Item itemToSell, bool sellFullStack)
    {
        if (playerInventory == null) return;

        int amountToSell = sellFullStack ? itemToSell.stackSize : 1;

        for (int i = 0; i < amountToSell; i++)
        {
            int sellPrice = Mathf.FloorToInt(itemToSell.sellPrice);
            playerInventory.gold += sellPrice;

            if (playerInventory.equipList.Exists(e => e.equipedItem == itemToSell))
            {
                Debug.Log($"[Merchant] Unequipping {itemToSell.itemData.itemName} before selling");
                playerInventory.UnequipItem(itemToSell, true);
            }

            if (playerInventory.FindItem(itemToSell.itemData) != null)
            {
                playerInventory.RemoveOneItem(itemToSell);
            }
            else if (playerInventory.equipmentInventory.FindItem(itemToSell.itemData) != null)
            {
                playerInventory.equipmentInventory.RemoveOneItem(itemToSell);
            }
            else
            {
                Debug.LogWarning($"[Merchant] Tried to sell {itemToSell.itemData.itemName} but couldn't find it!");
            }

            AddItem(new Inventory_Item(itemToSell.itemData));
        }

        NotifyInventoryChanged();
        playerInventory.TriggerUpdateUI();
    }

    public void FillShopList()
    {
        itemList.Clear();
        var possibleItems = new List<Inventory_Item>();

        foreach (var itemData in shopData.itemList)
        {
            // Filter by merchant type
            if (!IsItemAllowed(itemData.itemType))
                continue;

            int stackSize = Random.Range(itemData.minStackSizeAtShop, itemData.maxStackSizeAtShop + 1);
            stackSize = Mathf.Clamp(stackSize, 1, itemData.maxStackSize);

            var item = new Inventory_Item(itemData) { stackSize = stackSize };
            possibleItems.Add(item);
        }

        int randomAmount = Random.Range(minItemsAmount, maxInventorySize + 1);
        int finalAmount = Mathf.Clamp(randomAmount, 1, possibleItems.Count);

        for (int i = 0; i < finalAmount; i++)
        {
            int index = Random.Range(0, possibleItems.Count);
            AddItem(possibleItems[index]);
            possibleItems.RemoveAt(index);
        }

        NotifyInventoryChanged();
    }

    private bool IsItemAllowed(ItemType itemType)
    {
        switch (merchantType)
        {
            case MerchantType.Weapons:
                return itemType == ItemType.Weapon;
            case MerchantType.Armor:
                return itemType == ItemType.Armor;
            case MerchantType.Items:
                return itemType == ItemType.Consumable || itemType == ItemType.Material;
            case MerchantType.Trinkets:
                return itemType == ItemType.trinket;
            default:
                return true;
        }
    }
}
