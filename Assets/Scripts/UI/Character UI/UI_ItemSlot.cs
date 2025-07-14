using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

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
            StopBlinkingHighlight();
            return;
        }

        itemIcon.sprite = itemInSlot.itemData.itemIcon;
        itemStackSize.text = itemInSlot.stackSize > 1 ? $"x{itemInSlot.stackSize}" : "";
        StopBlinkingHighlight();
    }

    public virtual void OnPointerDown(PointerEventData eventData) { }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        StartBlinkingHighlight();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        StopBlinkingHighlight();
    }

    public virtual void Clear()
    {
        itemInSlot = null;
        itemIcon.sprite = defaultIconSprite;
        itemStackSize.text = "";
        StopBlinkingHighlight();
    }

    private void StartBlinkingHighlight()
    {
        if (highlighter == null) return;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkHighlight());
    }

    private void StopBlinkingHighlight()
    {
        if (highlighter == null) return;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = null;
        highlighter.SetActive(false);
    }

    private IEnumerator BlinkHighlight()
    {
        while (true)
        {
            highlighter.SetActive(!highlighter.activeSelf);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public virtual void HighlightOn()
    {
        StartBlinkingHighlight();
    }

    public virtual void HighlightOff()
    {
        StopBlinkingHighlight();
    }
}
