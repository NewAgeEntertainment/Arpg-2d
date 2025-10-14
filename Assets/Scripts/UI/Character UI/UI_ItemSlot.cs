using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using Rewired;

[DisallowMultipleComponent]
public class UI_ItemSlot :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler,
    IUpdateSelectedHandler
{
    // ====== Inventory data ======
    public Inventory_Item itemInSlot { get; protected set; }
    protected Inventory_Player inventory;
    protected UI ui;
    protected RectTransform rect;

    // Events
    public event Action<Inventory_Item> OnSlotSubmit;      // primary (left click / Submit)
    public event Action<Inventory_Item> OnSlotRightClick;  // secondary (right click / alt)

    // NEW: focus events to drive tooltip
    public event Action<Inventory_Item> OnSlotFocus;       // when this slot is hovered/selected
    public event Action OnSlotBlur;                        // when hover/selection leaves

    [Header("UI Slot Setup")]
    [SerializeField] protected TMPro.TextMeshProUGUI itemNameText;
    [SerializeField] protected Image itemIcon;
    [SerializeField] protected TMPro.TextMeshProUGUI itemStackSize;
    [SerializeField] protected Sprite defaultIconSprite;
    [SerializeField] protected GameObject highlighter;

    // ====== Rewired (optional alt-submit) ======
    [Header("Rewired (optional alt-submit)")]
    [SerializeField] private bool enableAltSubmit = true;
    [SerializeField] private int rewiredPlayerId = 0;
    [SerializeField] private string altSubmitAction = "AssignPopup";

    private Rewired.Player rPlayer;

    // ====== Internal ======
    private Coroutine blinkCoroutine;
    private bool isSelected = false;
    private Selectable selectable;

    protected virtual void Awake()
    {
        ui = FindFirstObjectByType<UI>();
        inventory = FindFirstObjectByType<Inventory_Player>();
        rect = GetComponent<RectTransform>();

        EnsureSelectable();
        TryGetRewired();
    }

    private void EnsureSelectable()
    {
        selectable = GetComponent<Selectable>();
        if (selectable == null)
        {
            var btn = gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            selectable = btn;
        }
    }

    private void TryGetRewired()
    {
        if (!enableAltSubmit) return;
        try { rPlayer = ReInput.players.GetPlayer(rewiredPlayerId); } catch { rPlayer = null; }
    }

    // ================== Data/UI refresh ==================

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

        if (itemNameText) itemNameText.text = "";
        ApplyStackSizeText();
        StopBlinkingHighlight();
        SetSelected(false);

        OnSlotBlur?.Invoke(); // ensure tooltip hides
    }

    protected virtual void ApplyStackSizeText()
    {
        if (!itemStackSize) return;

        if (itemInSlot != null && itemInSlot.itemData != null && itemInSlot.stackSize > 1)
            itemStackSize.text = $"x{itemInSlot.stackSize}";
        else
            itemStackSize.text = "";
    }

    // ================== Mouse (Pointer) ==================

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (itemInSlot == null || itemInSlot.itemData == null) return;

        var data = itemInSlot.itemData;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnSlotSubmit?.Invoke(itemInSlot);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnSlotRightClick?.Invoke(itemInSlot);

            if (data.itemType == ItemType.Consumable)
            {
                var uiInventory = FindObjectOfType<UI_Inventory>();
                uiInventory?.OpenAssignPopup(itemInSlot);
            }
        }

        SetSelected(true);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        StartBlinkingHighlight();
        if (itemInSlot != null) OnSlotFocus?.Invoke(itemInSlot);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        StopBlinkingHighlight();
        OnSlotBlur?.Invoke();
    }

    // ================== Keyboard/Controller (EventSystem) ==================

    public void OnSelect(BaseEventData eventData)
    {
        SetSelected(true);
        HighlightOn();
        if (itemInSlot != null) OnSlotFocus?.Invoke(itemInSlot);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetSelected(false);
        HighlightOff();
        OnSlotBlur?.Invoke();
    }

    // ISubmitHandler — fires on “Submit” while focused
    public void OnSubmit(BaseEventData eventData)
    {
        SubmitFromKeyboard();
    }

    // Called every frame while selected — poll Rewired alt action here
    public void OnUpdateSelected(BaseEventData eventData)
    {
        if (!enableAltSubmit) return;
        if (rPlayer == null) TryGetRewired();
        if (rPlayer != null && rPlayer.GetButtonDown(altSubmitAction))
            RightClickFromKeyboard();
    }

    // Keyboard/controller helpers
    public void SubmitFromKeyboard()
    {
        if (itemInSlot != null)
            OnSlotSubmit?.Invoke(itemInSlot);
    }

    public void RightClickFromKeyboard()
    {
        if (itemInSlot == null) return;

        OnSlotRightClick?.Invoke(itemInSlot);

        var data = itemInSlot.itemData;
        if (data != null && data.itemType == ItemType.Consumable)
        {
            var uiInventory = FindObjectOfType<UI_Inventory>();
            uiInventory?.OpenAssignPopup(itemInSlot);
        }
    }

    // ================== Highlight visuals ==================

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

    // ================== Selection state ==================

    public bool IsSelected() => isSelected;

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (highlighter != null)
            highlighter.SetActive(selected);
    }
}
