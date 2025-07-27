using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class UI_ItemSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Inventory_Item itemInSlot { get; protected set; }
    protected Inventory_Player inventory;
    protected UI ui;
    protected RectTransform rect;

    public event Action<Inventory_Item> OnSubmit;         // Left click
    public event Action<Inventory_Item> OnRightClick;     // Right click

    [Header("UI Slot Setup")]
    [SerializeField] private TMPro.TextMeshProUGUI itemNameText;
    [SerializeField] protected UnityEngine.UI.Image itemIcon;
    [SerializeField] protected TMPro.TextMeshProUGUI itemStackSize;
    [SerializeField] protected Sprite defaultIconSprite;
    [SerializeField] protected GameObject highlighter;

    private Coroutine blinkCoroutine;
    private bool isSelected = false;

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
            if (itemIcon) itemIcon.sprite = defaultIconSprite;
            if (itemNameText) itemNameText.text = "";
            ApplyStackSizeText();
            StopBlinkingHighlight();
            return;
        }

        if (itemIcon)
        {
            itemIcon.enabled = true;
            itemIcon.sprite = itemInSlot.itemData.itemIcon;
        }

        if (itemNameText)
            itemNameText.text = itemInSlot.itemData.itemName;

        ApplyStackSizeText();
        StopBlinkingHighlight();
    }

    public virtual void Clear()
    {
        itemInSlot = null;
        if (itemIcon)
        {
            itemIcon.sprite = defaultIconSprite;
            itemIcon.enabled = defaultIconSprite != null;
        }

        if (itemNameText)
            itemNameText.text = "";

        ApplyStackSizeText(); // will clear it by default
        StopBlinkingHighlight();
        SetSelected(false);
    }

    /// <summary>
    /// Override this in derived classes (e.g., Merchant) to suppress stack display.
    /// </summary>
    protected virtual void ApplyStackSizeText()
    {
        if (!itemStackSize) return;

        if (itemInSlot != null && itemInSlot.itemData != null && itemInSlot.stackSize > 1)
            itemStackSize.text = $"x{itemInSlot.stackSize}";
        else
            itemStackSize.text = "";
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null || itemInSlot.itemData == null) return;

        var data = itemInSlot.itemData;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnSubmit?.Invoke(itemInSlot);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick?.Invoke(itemInSlot);

            if (data.itemType == ItemType.Consumable)
            {
                var uiInventory = FindObjectOfType<UI_Inventory>();
                uiInventory?.OpenAssignPopup(itemInSlot);
            }
            else
            {
                Debug.Log($"[ItemSlot] {data.itemName} cannot be assigned. Only consumables are assignable.");
            }
        }

        SetSelected(true);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        StartBlinkingHighlight();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        StopBlinkingHighlight();
    }

    public virtual void HighlightOn() => StartBlinkingHighlight();
    public virtual void HighlightOff() => StopBlinkingHighlight();

    protected void StartBlinkingHighlight()
    {
        if (highlighter == null) return;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkHighlight());
    }

    protected void StopBlinkingHighlight()
    {
        if (highlighter == null) return;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = null;
        highlighter.SetActive(false);
    }

    protected IEnumerator BlinkHighlight()
    {
        while (true)
        {
            highlighter.SetActive(!highlighter.activeSelf);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public bool IsSelected() => isSelected;

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (highlighter != null)
            highlighter.SetActive(selected);
    }
}
