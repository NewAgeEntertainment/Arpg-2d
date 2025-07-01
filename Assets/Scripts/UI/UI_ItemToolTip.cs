using System.Text;
using TMPro;
using UnityEngine;

public class UI_ItemToolTip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI itemInfo;

    public void ShowToolTip(bool show, Inventory_Item itemToShow)
    {
        if (show && itemToShow != null && itemToShow.itemData != null)
        {
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
