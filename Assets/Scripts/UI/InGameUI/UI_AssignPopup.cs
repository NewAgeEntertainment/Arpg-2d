// Assets/Scripts/UI/Inventory/UI_AssignPopup.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UI_AssignPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Button slot1Button;
    [SerializeField] private Button slot2Button;
    [SerializeField] private Button slot3Button;
    [SerializeField] private Button slot4Button;
    [SerializeField] private Button cancelButton;

    [Header("SFX (optional)")]
    [SerializeField] private AudioSource audioSrc;
    [SerializeField] private AudioClip confirmSfx;
    [SerializeField] private AudioClip cancelSfx;

    private Inventory_Item item;
    // onConfirm(item, amount, slotIndex1Based)
    private Action<Inventory_Item, int, int> onConfirm;

    private void Awake()
    {
        if (audioSrc) audioSrc.ignoreListenerPause = true;
        if (cancelButton) cancelButton.onClick.AddListener(OnCancel);
        gameObject.SetActive(false);
    }

    public void Open(Inventory_Item newItem, Action<Inventory_Item, int, int> onConfirm)
    {
        this.item = newItem;
        this.onConfirm = onConfirm;

        if (itemNameText) itemNameText.text = (item?.itemData != null) ? item.itemData.itemName : "(null)";
        if (amountInput) amountInput.text = "1";

        // Clear old listeners, then wire slot buttons
        WireSlotButton(slot1Button, 1);
        WireSlotButton(slot2Button, 2);
        WireSlotButton(slot3Button, 3);
        WireSlotButton(slot4Button, 4);

        gameObject.SetActive(true);
    }

    private void WireSlotButton(Button btn, int slotIndex1Based)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnPickSlot(slotIndex1Based));
    }

    private void OnPickSlot(int slotIndex1Based)
    {
        int amount = 1;
        if (amountInput && int.TryParse(amountInput.text, out var parsed))
            amount = Mathf.Max(1, parsed);

        if (audioSrc && confirmSfx) audioSrc.PlayOneShot(confirmSfx);
        onConfirm?.Invoke(item, amount, slotIndex1Based);
        Close();
    }

    private void OnCancel()
    {
        if (audioSrc && cancelSfx) audioSrc.PlayOneShot(cancelSfx);
        Close();
    }

    public void Close() => gameObject.SetActive(false);
}
