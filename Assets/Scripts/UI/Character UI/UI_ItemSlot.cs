using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ItemSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Inventory_Item itemInSlot { get; protected set; }
    protected Inventory_Player inventory;
    protected UI ui;
    protected RectTransform rect;

    [Header("UI Slot Setup")]
    [SerializeField] protected UnityEngine.UI.Image itemIcon;
    [SerializeField] protected TMPro.TextMeshProUGUI itemStackSize;
    [SerializeField] protected Sprite defaultIconSprite;

    protected virtual void Awake()
    {
        ui = FindFirstObjectByType<UI>();
        inventory = FindFirstObjectByType<Inventory_Player>();
        rect = GetComponent<RectTransform>();
    }

    public virtual void UpdateSlot(Inventory_Item item)
    {
        itemInSlot = item;

        if (itemInSlot == null || itemInSlot.itemData == null)
        {
            itemIcon.sprite = defaultIconSprite;
            itemStackSize.text = "";
            return;
        }

        itemIcon.sprite = itemInSlot.itemData.itemIcon;
        itemStackSize.text = itemInSlot.stackSize > 1 ? $"x{itemInSlot.stackSize}" : "";
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null) return;

        if (itemInSlot.itemData.itemType == ItemType.Consumable)
        {
            // ✅ Open the new character selector popup
            ui.inGameUI.OpenCharacterProfilePopup(itemInSlot);
        }
        else
        {
            Debug.Log($"[{itemInSlot.itemData.itemType}] cannot be used here.");
        }

        ui.itemToolTip.ShowToolTip(false, null);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInSlot == null) return;
        ui.itemToolTip.ShowToolTip(true, itemInSlot);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
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
