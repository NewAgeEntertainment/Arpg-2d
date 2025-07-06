using System.Text;
using TMPro;
using UnityEngine;

public class UI_EquipmentToolTip : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject tooltipPanel; // ✅ Just the visual box

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI statInfo;

    public void ShowEquipmentToolTip(bool show, Inventory_Item item)
    {
        if (tooltipPanel == null)
        {
            Debug.LogWarning("[UI_EquipmentToolTip] Tooltip panel not assigned!");
            return;
        }

        if (show && item != null && item.itemData != null)
        {
            if (item.itemData.itemType != ItemType.Weapon &&
                item.itemData.itemType != ItemType.Armor &&
                item.itemData.itemType != ItemType.trinket)
            {
                Debug.Log("[UI_EquipmentToolTip] Not equipment, hiding.");
                tooltipPanel.SetActive(false);
                return;
            }

            itemName.text = item.itemData.itemName;
            itemType.text = item.itemData.itemType.ToString();
            statInfo.text = GetEquipmentInfo(item);

            tooltipPanel.SetActive(true);
        }
        else
        {
            tooltipPanel.SetActive(false);
        }
    }

    private string GetEquipmentInfo(Inventory_Item item)
    {
        StringBuilder sb = new StringBuilder();

        if (item.modifiers != null && item.modifiers.Length > 0)
        {
            sb.AppendLine("<b>Stats:</b>");
            foreach (var mod in item.modifiers)
            {
                string modType = mod.statType.ToString();
                string modValue = mod.value > 0 ? $"+{mod.value}" : mod.value.ToString();
                sb.AppendLine($"<color=#FFD700>{modValue}</color> <color=#FFFFFF>{modType}</color>");
            }
        }

        if (item.itemEffect != null)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Unique Effect:</b>");
            sb.AppendLine($"<color=#00FFFF>{item.itemEffect.effectDescription}</color>");
        }

        if (sb.Length == 0)
            sb.AppendLine("<i>No additional stats.</i>");

        return sb.ToString();
    }
}
