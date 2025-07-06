using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UI_ItemSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Inventory_Item itemInSlot { get; private set; }
    protected Inventory_Player inventory;
    protected UI ui;
    protected RectTransform rect;

    [Header("UI Slot Setup")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] protected Image itemIcon;
    [SerializeField] protected TextMeshProUGUI itemStackSize;
    [SerializeField] protected Sprite defaultIconSprite; // <-- This is the default UI sprite  

    protected virtual void Awake()
    {
        ui = FindFirstObjectByType<UI>();
        rect = GetComponent<RectTransform>();
        inventory = FindAnyObjectByType<Inventory_Player>();
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null || itemInSlot.itemData.itemType == ItemType.Material)
            return;

        bool alternativeInput = Input.GetKey(KeyCode.LeftControl);

        if (alternativeInput)
        {
            inventory.RemoveOneItem(itemInSlot); // Call the method to remove one item from the slot  
        }
        else
        {
            if (itemInSlot.itemData.itemType == ItemType.Consumable)
            {
                inventory.TryUseItem(itemInSlot); // Call the method to try to use the item  
            }
            else
                inventory.TryEquipItem(itemInSlot); // Call the method to try to equip the item  
        }

        if (itemInSlot == null)
            ui.itemToolTip.ShowToolTip(false, null);
    }

    public virtual void UpdateSlot(Inventory_Item item) // Removed 'override' keyword  
    {
        itemInSlot = item;

        if (itemInSlot == null || itemInSlot.itemData == null)
        {
            itemIcon.sprite = defaultIconSprite;
            itemIcon.enabled = defaultIconSprite != null;
            itemNameText.text = "";
            itemStackSize.text = "";
            return;
        }

        itemIcon.sprite = itemInSlot.itemData.itemIcon;
        itemIcon.enabled = true; // ✅ force icon visible  

        itemNameText.text = itemInSlot.itemData.itemName;
        itemStackSize.text = itemInSlot.stackSize > 1 ? "x" + itemInSlot.stackSize : "";
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (ui != null && ui.itemToolTip != null && itemInSlot != null)
            ui.itemToolTip.ShowToolTip(true, itemInSlot);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (ui != null && ui.itemToolTip != null)
            ui.itemToolTip.ShowToolTip(false, null);
    }

    public virtual void Clear()
    {
        Debug.Log("Clear called on slot");
        itemInSlot = null;
        itemStackSize.text = "";
        itemIcon.sprite = defaultIconSprite;
        itemIcon.enabled = defaultIconSprite != null;
    }
}

