using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class UI_EquippedSlot : UI_ItemSlot, IPointerEnterHandler, IPointerExitHandler
{
    public ItemType slotType;

    [Header("Equipment Tooltip")]
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;
    [SerializeField] private Button button;

    [Header("Slot Type Label")]
    [SerializeField] private TextMeshProUGUI slotTypeLabel; // ✅ Assign this in the inspector

    private bool isInteractable = true;
    private Coroutine blinkCoroutine;

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
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!isInteractable) return;

        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI != null)
        {
            equipmentUI.ShowEquipmentInventoryPanel(slotType);
        }

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
        if (button != null)
            button.interactable = interactable;
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
}
