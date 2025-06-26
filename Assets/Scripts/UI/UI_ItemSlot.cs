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
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemStackSize;
    [SerializeField] private Sprite defaultIconSprite; // <-- This is the default UI sprite


    protected void Awake()
    {
        ui = GetComponentInParent<UI>();
        rect = GetComponent<RectTransform>();
        inventory = FindAnyObjectByType<Inventory_Player>();
        
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null || itemInSlot.itemData.itemType == ItemType.Material)
            return;

        if (itemInSlot.itemData.itemType == ItemType.Consumable)
        {
            if (itemInSlot.itemEffect.CanBeUsed() == false)
            {
                return;
            }
            inventory.TryUseItem(itemInSlot); // Call the method to try to use the item
        }
        else
            inventory.TryEquipItem(itemInSlot); // Call the method to try to equip the item
            

        if (itemInSlot == null)
            ui.itemToolTip.ShowToolTip(false, null);
        
        //if (inventory != null && item != null)
        //{
            
        //    inventory.TryUseItem(item, player);
        //}


    }

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(itemInSlot == null) return;

        ui.itemToolTip.ShowToolTip(true, rect, itemInSlot);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.itemToolTip.ShowToolTip(false, null);
    }

    public virtual void Clear()
    {
        itemInSlot = null;
        itemIcon.sprite = defaultIconSprite;
        itemStackSize.text = "";
    }
}

