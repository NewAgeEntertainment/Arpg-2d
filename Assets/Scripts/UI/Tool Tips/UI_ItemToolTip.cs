using TMPro;
using UnityEngine;

public class UI_ItemToolTip : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private GameObject tooltipPanel;           // The visible box container (child under your Inventory)
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI itemInfo;

    [Header("Sell Price")]
    [SerializeField] private TextMeshProUGUI itemSellPrice;     // Shown inside the tooltip

    private void Awake()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// Show/hide the tooltip for a given item (Inventory context).
    /// Displays sell price only.
    /// </summary>
    public void ShowToolTip(bool show, Inventory_Item itemToShow)
    {
        if (tooltipPanel == null)
        {
            Debug.LogWarning("[UI_ItemToolTip] Tooltip panel is missing!");
            return;
        }

        if (!show || itemToShow == null || itemToShow.itemData == null)
        {
            tooltipPanel.SetActive(false);
            return;
        }

        // Basic fields
        if (itemName) itemName.text = itemToShow.itemData.itemName;
        if (itemType) itemType.text = itemToShow.itemData.itemType.ToString();

        // Info (uses your formatted helper; fallback so it’s never empty)
        if (itemInfo)
        {
            string info = itemToShow.GetItemInfo();
            if (string.IsNullOrWhiteSpace(info)) info = "No additional details.";
            itemInfo.text = info;
        }

        // Sell price only
        if (itemSellPrice)
        {
            int price = Mathf.FloorToInt(itemToShow.sellPrice);
            int count = Mathf.Max(1, itemToShow.stackSize);
            int total = price * count;

            itemSellPrice.text = (count > 1)
                ? $"Sell: {price} x{count} = {total}g"
                : $"Sell: {price}g";
        }

        tooltipPanel.SetActive(true);
    }

    public void Hide()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    // Optional: assign refs via code if needed
    public void SetRefs(GameObject panel, TextMeshProUGUI name, TextMeshProUGUI type, TextMeshProUGUI info, TextMeshProUGUI sellPrice)
    {
        tooltipPanel = panel;
        itemName = name;
        itemType = type;
        itemInfo = info;
        itemSellPrice = sellPrice;
    }
}
