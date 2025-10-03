using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;
using TMPro;

// ✅ NEW
using System.Collections.Generic;
using Rewired;

public class UI_EquippedSlot : UI_ItemSlot,
    IPointerEnterHandler, IPointerExitHandler,
    // ✅ NEW: keyboard/controller focus + submit
    ISelectHandler, IDeselectHandler, ISubmitHandler, IUpdateSelectedHandler
{
    public ItemType slotType;

    [Header("Equipment Tooltip")]
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;
    [SerializeField] private Button button;

    [Header("Slot Type Label")]
    [SerializeField] private TextMeshProUGUI slotTypeLabel; // ✅ Assign this in the inspector

    private bool isInteractable = true;
    private Coroutine blinkCoroutine;

    // ---------- NEW: Keyboard/controller plumbing ----------
    [Header("Keyboard/Controller")]
    [Tooltip("Create a Selectable on this GO at runtime if none exists so it can be focused by EventSystem.")]
    [SerializeField] private bool ensureSelectableOnThisGO = true;

    [Tooltip("Also hook any child Buttons so their onClick triggers the same action.")]
    [SerializeField] private bool hookChildButtons = true;

    [Tooltip("Enable polling an alternate Rewired action while this slot is focused (optional).")]
    [SerializeField] private bool enableAltSubmit = false;
    [SerializeField] private int rewiredPlayerId = 0;
    [SerializeField] private string altSubmitAction = "AssignPopup";

    private Rewired.Player rPlayer;
    private Selectable selfSelectable;                  // Selectable on *this* GO
    private readonly List<Button> hookedChildButtons = new();

    private void OnValidate()
    {
        gameObject.name = "UI_EquippedSlot - " + slotType.ToString();

#if UNITY_EDITOR
        if (!Application.isPlaying && slotTypeLabel != null)
        {
            slotTypeLabel.text = slotType.ToString();
        }
#endif
    }

    private void Start()
    {
        UpdateSlotTypeLabel();
    }

    private void OnEnable()
    {
        UpdateSlotTypeLabel();

        // --- NEW: ensure EventSystem can select THIS object ---
        if (ensureSelectableOnThisGO)
        {
            selfSelectable = GetComponent<Selectable>();
            if (selfSelectable == null)
            {
                selfSelectable = gameObject.AddComponent<Selectable>();
                selfSelectable.transition = Selectable.Transition.None;
            }
            selfSelectable.interactable = isInteractable;
        }

        // Hook this slot's assigned Button (if any) to our submit
        if (button != null)
        {
            button.onClick.RemoveListener(SubmitFromKeyboard);
            button.onClick.AddListener(SubmitFromKeyboard);
            // Keep its interactable in sync
            button.interactable = isInteractable;
        }

        // Optionally hook any child buttons so A/Enter there calls the same submit
        if (hookChildButtons)
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn == null || btn == button) continue;
                if (!hookedChildButtons.Contains(btn))
                {
                    btn.onClick.AddListener(SubmitFromKeyboard);
                    hookedChildButtons.Add(btn);
                }
            }
        }

        TryGetRewired();
    }

    private void OnDisable()
    {
        // Unhook child buttons we added listeners to
        foreach (var btn in hookedChildButtons)
            if (btn != null) btn.onClick.RemoveListener(SubmitFromKeyboard);
        hookedChildButtons.Clear();

        if (button != null)
            button.onClick.RemoveListener(SubmitFromKeyboard);
    }

    private void TryGetRewired()
    {
        if (!enableAltSubmit) { rPlayer = null; return; }
        try { rPlayer = ReInput.players.GetPlayer(rewiredPlayerId); }
        catch { rPlayer = null; }
    }

    private void UpdateSlotTypeLabel()
    {
        if (slotTypeLabel != null)
        {
            slotTypeLabel.text = slotType.ToString();
        }
    }

    public override void UpdateSlot(Inventory_Item item)
    {
        base.UpdateSlot(item);

        // If the slot is empty (no item), show "Empty"
        if (item == null || item.itemData == null)
        {
            if (itemNameText != null) // from base UI_ItemSlot
                itemNameText.text = "Empty";
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!isInteractable) return;

        OpenEquipPanelForThisSlot();
        StopBlinkingHighlight();
        SetHighlightSolid(true);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;

        StartBlinkingHighlight();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;

        StopBlinkingHighlight();
        SetHighlightSolid(false);
        equipmentToolTip?.ShowEquipmentToolTip(false, null); // This will call ShowBaseStats()
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        if (button != null) button.interactable = interactable;
        if (selfSelectable != null) selfSelectable.interactable = interactable;
    }

    public void SetEquipmentToolTip(UI_EquipmentToolTip toolTip)
    {
        equipmentToolTip = toolTip;
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
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void ResetHighlight()
    {
        StopBlinkingHighlight();
        SetHighlightSolid(false);
    }

    private void Update()
    {
        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI != null && equipmentUI.IsOpen && Rewired.ReInput.players.GetPlayer(0).GetButtonDown("UICancel"))
        {
            equipmentUI.HandleCancel();
        }
    }

    // ================================
    // ✅ NEW: Keyboard/controller hooks
    // ================================

    // Called when this GameObject becomes the EventSystem's selected object
    public void OnSelect(BaseEventData eventData)
    {
        if (!isInteractable) return;

        StartBlinkingHighlight();
        SetHighlightSolid(true);

        // Show equipped item tooltip (if any)
        if (itemInSlot != null && equipmentToolTip != null)
            equipmentToolTip.ShowEquipmentToolTip(true, itemInSlot);
    }

    // Called when selection moves away from this object
    public void OnDeselect(BaseEventData eventData)
    {
        if (!isInteractable) return;

        StopBlinkingHighlight();
        SetHighlightSolid(false);

        equipmentToolTip?.ShowEquipmentToolTip(false, null);
    }

    // Fired by Standalone/Rewired Input Module when user presses Submit (A/Enter) on this selected object
    public void OnSubmit(BaseEventData eventData)
    {
        SubmitFromKeyboard();
    }

    // Called every frame while this object is selected — can poll alternate actions here
    public void OnUpdateSelected(BaseEventData eventData)
    {
        if (!enableAltSubmit) return;
        if (rPlayer == null) TryGetRewired();
        if (rPlayer != null && rPlayer.GetButtonDown(altSubmitAction))
        {
            SubmitFromKeyboard();
        }
    }

    // Same action path as mouse click
    public void SubmitFromKeyboard()
    {
        if (!isInteractable) return;

        OpenEquipPanelForThisSlot();
        StopBlinkingHighlight();
        SetHighlightSolid(true);
    }

    // ---------------------
    // Shared action
    // ---------------------
    private void OpenEquipPanelForThisSlot()
    {
        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI != null)
        {
            equipmentUI.ShowEquipmentInventoryPanel(slotType);
        }
    }
}
