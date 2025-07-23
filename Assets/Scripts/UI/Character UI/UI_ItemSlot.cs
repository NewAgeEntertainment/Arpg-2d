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
    [SerializeField] private TMPro.TextMeshProUGUI itemNameText; // Add this field
    [SerializeField] protected UnityEngine.UI.Image itemIcon;
    [SerializeField] protected TMPro.TextMeshProUGUI itemStackSize;
    [SerializeField] protected Sprite defaultIconSprite;
    [SerializeField] protected GameObject highlighter;

    private Coroutine blinkCoroutine;

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
            if (itemNameText != null)
                itemNameText.text = "";
            StopBlinkingHighlight();
            return;
        }

        itemIcon.sprite = itemInSlot.itemData.itemIcon;
        itemStackSize.text = itemInSlot.stackSize > 1 ? $"x{itemInSlot.stackSize}" : "";
        if (itemNameText != null)
            itemNameText.text = itemInSlot.itemData.itemName;
        StopBlinkingHighlight();
    }

    public virtual void Clear()
    {
        itemInSlot = null;
        itemIcon.sprite = defaultIconSprite;
        itemStackSize.text = "";
        StopBlinkingHighlight();
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null || itemInSlot.itemData == null) return;

        var data = itemInSlot.itemData;

        // Left click → Use item (e.g., open Actor Select Panel if usable)
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnSubmit?.Invoke(itemInSlot);
        }

        // Right click → Open Assign Popup (only if item is a Consumable)
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
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

        SetSelected(true); // Optional: mark slot as selected
    }





    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        StartBlinkingHighlight();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        StopBlinkingHighlight();
    }

    public virtual void HighlightOn()
    {
        StartBlinkingHighlight();
    }

    public virtual void HighlightOff()
    {
        StopBlinkingHighlight();
    }

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

    private bool isSelected = false;

    public bool IsSelected() => isSelected;

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (highlighter != null)
            highlighter.SetActive(selected);
    }

}
