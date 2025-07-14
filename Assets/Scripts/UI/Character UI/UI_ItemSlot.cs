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
    [SerializeField] protected GameObject highlighter; // For visual highlight

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
            HighlightOff();
            return;
        }

        itemIcon.sprite = itemInSlot.itemData.itemIcon;
        itemStackSize.text = itemInSlot.stackSize > 1 ? $"x{itemInSlot.stackSize}" : "";
        HighlightOff();
    }

    public virtual void OnPointerDown(PointerEventData eventData) { }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        HighlightOn();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        HighlightOff();
    }

    public virtual void Clear()
    {
        itemInSlot = null;
        itemIcon.sprite = defaultIconSprite;
        itemStackSize.text = "";
        HighlightOff();
    }

    public virtual void HighlightOn()
    {
        if (highlighter != null)
            highlighter.SetActive(true);
    }

    public virtual void HighlightOff()
    {
        if (highlighter != null)
            highlighter.SetActive(false);
    }
}
