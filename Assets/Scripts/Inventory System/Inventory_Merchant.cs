using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Merchant : Inventory_Base
{
    private Inventory_Player inventory;

    [SerializeField] private ItemListDataSO shopData;
    [SerializeField] private int minItemsAmount = 4;
    

    protected override void Awake()
    {
        base.Awake();
        FillShopList();
    }

    public void TryBuyItem(Inventory_Item itemToBuy, bool buyFullStack)
    {
        int amountToBuy = buyFullStack ? itemToBuy.stackSize : 1;

        for (int i = 0; i < amountToBuy; i++)
        {
            if (inventory.gold < itemToBuy.buyPrice)
            {
                Debug.Log("Not enough money!");
                return;

            }

            if (itemToBuy.itemData.itemType == ItemType.Material)
            {
                inventory.storage.AddMaterialToStash(itemToBuy);
            }
            else
            {
                if (inventory.CanAddItem(itemToBuy))
                {
                    var itemToAdd = new Inventory_Item(itemToBuy.itemData);
                    inventory.AddItem(itemToAdd);
                }
            }

            inventory.gold = inventory.gold - itemToBuy.buyPrice;
            RemoveOneItem(itemToBuy); // Remove the item from the merchant's inventory
        }

        NotifyInventoryChanged();
    }

    public void TrySellItem(Inventory_Item itemToSell, bool sellFullStack)
    {
        int amountToSell = sellFullStack ? itemToSell.stackSize : 1;

        for (int i = 0; i < amountToSell; i++)
        {
            int sellPrice = Mathf.FloorToInt(itemToSell.sellPrice);
            inventory.gold += sellPrice;

            // 👇 If the item is equipped, unequip it first!
            if (PlayerHasEquipped(itemToSell))
            {
                Debug.Log($"[Merchant] Unequipping {itemToSell.itemData.itemName} before selling");
                PlayerUnequip(itemToSell);
            }

            // Remove from backpack OR equipment inventory
            if (inventory.FindItem(itemToSell.itemData) != null)
            {
                inventory.RemoveOneItem(itemToSell);
            }
            else if (inventory.equipmentInventory.FindItem(itemToSell.itemData) != null)
            {
                inventory.equipmentInventory.RemoveOneItem(itemToSell);
            }
            else
            {
                Debug.LogWarning($"[Merchant] Tried to sell {itemToSell.itemData.itemName} but couldn't find it!");
            }

            // Add to merchant
            AddItem(new Inventory_Item(itemToSell.itemData));
        }

        NotifyInventoryChanged();
        inventory.NotifyInventoryChanged();
    }


    public void FillShopList()
    {
        itemList.Clear();
        List<Inventory_Item> possibleItems = new List<Inventory_Item>();

        foreach (var itemData in shopData.itemList)
        {
            int randmoziedStack = Random.Range(itemData.minStackSizeAtShop, itemData.maxStackSizeAtShop + 1);
            int finalStack = Mathf.Clamp(randmoziedStack, 1, itemData.maxStackSize);

            Inventory_Item itemToAdd = new Inventory_Item(itemData);
            itemToAdd.stackSize = finalStack; // Set the randomized stack size

            possibleItems.Add(itemToAdd);
        }

        int randomItemAmount = Random.Range(minItemsAmount, maxInventorySize + 1);
        int finalAmount = Mathf.Clamp(randomItemAmount, 1, possibleItems.Count);
   
        for (int i = 0; i < finalAmount; i++)
        {
            var randomIndex = Random.Range(0, possibleItems.Count);
            var item = possibleItems[randomIndex];

            if (CanAddItem(item)) 
            {
                possibleItems.Remove(item);
                AddItem(item);
            }
        }

        NotifyInventoryChanged();
        //TriggerUpdateUI();
    }

    public void SetInventory(Inventory_Player inventory) => this.inventory = inventory;

    public bool PlayerHasEquipped(Inventory_Item item)
    {
        return inventory.equipList.Exists(e => e.equipedItem == item);
    }

    public void PlayerUnequip(Inventory_Item item)
    {
        inventory.UnequipItem(item, true);
    }


}
