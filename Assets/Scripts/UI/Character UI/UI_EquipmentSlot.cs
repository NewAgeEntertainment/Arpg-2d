using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

// ✅ NEW
using UnityEngine.UI;
using Rewired;

public class UI_EquipmentSlot : UI_ItemSlot,
    IPointerEnterHandler, IPointerExitHandler,
    // ✅ NEW: focus + submit via keyboard/controller
    ISelectHandler, IDeselectHandler, ISubmitHandler, IUpdateSelectedHandler
{
    [Header("Tooltip")]
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI itemNameText;   // ← assign in Inspector

    private bool isSelectable = true;
    private Coroutine blinkCoroutine;

    // ---------- NEW: Keyboard/controller plumbing ----------
    [Header("Rewired (optional)")]
    [SerializeField] private bool enableAltSubmit = true;
    [SerializeField] private int rewiredPlayerId = 0;
    [SerializeField] private string altSubmitAction = "AssignPopup"; // map to your alt action (optional)

    // If you tend to put Buttons on children instead of the same GO as this component,
    // keep this ON to auto-wire their onClick to Equip() too.
    [Header("Submit Wiring")]
    [SerializeField] private bool hookChildButtons = true;

    private Rewired.Player rPlayer;
    private Selectable selfSelectable;     // on this GO (added at runtime if needed)
    private readonly System.Collections.Generic.List<Button> hookedChildButtons = new();

    private void OnValidate()
    {
        // Helps you find it in hierarchy while editing
        if (itemInSlot != null && itemInSlot.itemData != null)
            gameObject.name = $"UI_EquipmentSlot - {itemInSlot.itemData.itemName}";
        else
            gameObject.name = "UI_EquipmentSlot - Empty";
    }

    // ---------- lifecycle ----------
    private void OnEnable()
    {
        TryGetRewired();

        // Ensure we have a Selectable on THIS GO so it can be focused by EventSystem/nav
        selfSelectable = GetComponent<Selectable>();
        if (selfSelectable == null)
        {
            // Add a lightweight Selectable so the EventSystem can select this GO.
            selfSelectable = gameObject.AddComponent<Selectable>();
            // optional: tweak transition if you want visuals
            selfSelectable.transition = Selectable.Transition.None;
        }

        // If there’s a Button on this GO, wire it too so its onClick calls our equip
        var selfBtn = GetComponent<Button>();
        if (selfBtn != null)
        {
            selfBtn.onClick.RemoveListener(SubmitFromKeyboard);
            selfBtn.onClick.AddListener(SubmitFromKeyboard);
        }

        // Optionally wire child buttons (common when your visuals sit under a child)
        if (hookChildButtons)
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn == selfBtn) continue; // already wired
                if (hookedChildButtons.Contains(btn)) continue;
                btn.onClick.AddListener(SubmitFromKeyboard);
                hookedChildButtons.Add(btn);
            }
        }
    }

    private void OnDisable()
    {
        // Unhook child buttons we added listeners to
        foreach (var btn in hookedChildButtons)
            if (btn != null) btn.onClick.RemoveListener(SubmitFromKeyboard);
        hookedChildButtons.Clear();
    }

    private void TryGetRewired()
    {
        if (!enableAltSubmit) return;
        try { rPlayer = ReInput.players.GetPlayer(rewiredPlayerId); } catch { rPlayer = null; }
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
    public void SetSelectable(bool selectableFlag)
    {
        isSelectable = selectableFlag;
        if (selfSelectable != null) selfSelectable.interactable = selectableFlag;
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
        EquipOrSwap();
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
    // ✅ NEW: Keyboard/Controller support
    // --------------------------

    // Focus in via keyboard/controller
    public void OnSelect(BaseEventData eventData)
    {
        if (!isSelectable) return;
        StartBlinkingHighlight();
        SetHighlightSolid(true);

        if (itemInSlot != null && equipmentToolTip != null)
            equipmentToolTip.ShowEquipmentToolTip(true, itemInSlot);
    }

    // Focus out via keyboard/controller
    public void OnDeselect(BaseEventData eventData)
    {
        if (!isSelectable) return;
        StopBlinkingHighlight();
        SetHighlightSolid(false);
        equipmentToolTip?.ShowEquipmentToolTip(false, null);
    }

    // Pressing "Submit" (A / Enter) while focused
    public void OnSubmit(BaseEventData eventData)
    {
        SubmitFromKeyboard();
    }

    // Called each frame while selected — useful to poll an alternate action
    public void OnUpdateSelected(BaseEventData eventData)
    {
        if (!enableAltSubmit) return;
        if (rPlayer == null) TryGetRewired();
        if (rPlayer != null && rPlayer.GetButtonDown(altSubmitAction))
            RightClickFromKeyboard();
    }

    // Same action as mouse left-click
    public void SubmitFromKeyboard()
    {
        if (!isSelectable) return;
        EquipOrSwap();
    }

    // Optional secondary action (map however you want)
    public void RightClickFromKeyboard()
    {
        if (!isSelectable) return;
        EquipOrSwap();
    }

    private void EquipOrSwap()
    {
        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI == null) return;

        Debug.Log($"[UI_EquipmentSlot] Equipping/Swapping: {itemInSlot?.itemData?.itemName}");
        equipmentUI.SwapEquippedItem(itemInSlot);

        StopBlinkingHighlight();
        SetHighlightSolid(true);
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
