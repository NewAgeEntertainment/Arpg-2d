using System.Text;
using TMPro;
using UnityEngine;

public class UI_ItemToolTip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI itemInfo;

    [SerializeField] private TextMeshProUGUI itemPrice;
    [SerializeField] private Transform merchantInfo;
    [SerializeField] private Transform inventoryInfo;

    public void ShowToolTip(bool show, Inventory_Item itemToShow, bool buyPrice = false, bool showMerchantInfo = false)
    {
        if (show && itemToShow != null && itemToShow.itemData != null)
        {

            merchantInfo.gameObject.SetActive(showMerchantInfo);
            inventoryInfo.gameObject.SetActive(!showMerchantInfo);

            int price = buyPrice ? itemToShow.buyPrice : Mathf.FloorToInt(itemToShow.sellPrice);
            int totalPrice = price * itemToShow.stackSize;

            string fullStackPrice = ($"Price:{price}x{itemToShow.stackSize} - {totalPrice}g.");
            string singleStackPrice = ($"Price:{price}g.");


            itemName.text = itemToShow.itemData.itemName;
            itemType.text = itemToShow.itemData.itemType.ToString();
            itemInfo.text = itemToShow.GetItemInfo();

            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    
}
