using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_AssignPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Inventory_Item item;

    private void Awake()
    {
        // Hook up button events
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
    }

    /// <summary>
    /// Setup the popup with item info.
    /// </summary>
    public void Setup(Inventory_Item newItem)
    {
        item = newItem;

        if (itemNameText != null)
            itemNameText.text = item.itemData.itemName;

        if (amountInput != null)
            amountInput.text = "1"; // default amount
    }

    private void OnConfirm()
    {
        int amount = 1;

        if (amountInput != null && int.TryParse(amountInput.text, out int parsedAmount))
            amount = Mathf.Max(1, parsedAmount);

        Debug.Log($"[AssignPopup] Confirmed. Assign {amount}x {item.itemData.itemName}.");

        // Do your assign logic here (eg: call back to Inventory)
        // Example: FindFirstObjectByType<UI_Inventory>().AssignItem(item, amount);

        Close();
    }

    private void OnCancel()
    {
        Debug.Log("[AssignPopup] Cancelled.");
        Close();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
