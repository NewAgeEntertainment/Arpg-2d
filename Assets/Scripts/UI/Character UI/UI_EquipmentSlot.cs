using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class UI_EquipmentSlot : UI_ItemSlot, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip")]
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI itemNameText;   // ← assign in Inspector

    private bool isSelectable = true;
    private Coroutine blinkCoroutine;

    private void OnValidate()
    {
        // Helps you find it in hierarchy while editing
        if (itemInSlot != null && itemInSlot.itemData != null)
            gameObject.name = $"UI_EquipmentSlot - {itemInSlot.itemData.itemName}";
        else
            gameObject.name = "UI_EquipmentSlot - Empty";
    }

    public void SetItem(Inventory_Item item)
    {
        UpdateSlot(item);
    }

    public void SetEquipmentToolTip(UI_EquipmentToolTip toolTip)
    {
        equipmentToolTip = toolTip;
    }

    /// <summary>
    /// Option 1 (restored): allow EquipmentInventory to toggle interactability.
    /// </summary>
    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
    }

    // --------------------------
    // UI_ItemSlot overrides
    // --------------------------

    public override void UpdateSlot(Inventory_Item item)
    {
        base.UpdateSlot(item);

        // Update the name label
        if (itemNameText != null)
        {
            if (item == null || item.itemData == null)
                itemNameText.text = string.Empty; // clear when slot is empty
            else
                itemNameText.text = item.itemData.itemName;
        }
    }

    public override void Clear()
    {
        base.Clear();

        // Force clear label text when slot is cleared
        if (itemNameText != null)
            itemNameText.text = string.Empty;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!isSelectable) return;

        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI == null) return;

        // Attempt to equip from unequipped inventory list
        Debug.Log($"[UI_EquipmentSlot] Attempting to equip: {itemInSlot?.itemData?.itemName}");
        equipmentUI.SwapEquippedItem(itemInSlot);

        StopBlinkingHighlight();
        SetHighlightSolid(true);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelectable) return;

        StartBlinkingHighlight();

        if (itemInSlot != null && equipmentToolTip != null)
        {
            equipmentToolTip.ShowEquipmentToolTip(true, itemInSlot);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelectable) return;

        StopBlinkingHighlight();
        SetHighlightSolid(false);

        equipmentToolTip?.ShowEquipmentToolTip(false, null);
    }

    // --------------------------
    // Highlight helpers
    // --------------------------

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
    }

    private void SetHighlightSolid(bool on)
    {
        if (highlighter != null)
            highlighter.SetActive(on);
    }

    private IEnumerator BlinkHighlight()
    {
        while (true)
        {
            highlighter.SetActive(!highlighter.activeSelf);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void ResetHighlight()
    {
        StopBlinkingHighlight();
        SetHighlightSolid(false);
    }
}
