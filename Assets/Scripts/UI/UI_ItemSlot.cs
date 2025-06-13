using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ItemSlot : MonoBehaviour
{
    public Inventory_Item itemInSlot { get; private set; }

    [Header("UI Slot Setup")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemStackSize;
    [SerializeField] private Sprite defaultIconSprite; // <-- This is the default UI sprite

    public void UpdateSlot(Inventory_Item item)
    {
        itemInSlot = item;

        if (itemInSlot == null)
        {
            itemStackSize.text = "";
            itemIcon.sprite = defaultIconSprite;
            return;
        }

        itemIcon.sprite = itemInSlot.itemData.itemIcon;
        itemStackSize.text = item.stackSize.ToString();
    }
}
