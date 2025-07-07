using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_QuickItemSlot : MonoBehaviour, IPointerDownHandler
{
    [Header("References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemStackSize;
    [SerializeField] private Sprite defaultSprite;

    [Header("Slot Number")]
    [SerializeField] private int slotNumber = 1; // 1 or 2

    private UI_InGame ui;

    private void Awake()
    {
        ui = FindFirstObjectByType<UI_InGame>();

        if (itemIcon == null)
            Debug.LogWarning("[UI_QuickItemSlot] No itemIcon assigned!");

        if (itemStackSize == null)
            Debug.LogWarning("[UI_QuickItemSlot] No itemStackSize assigned!");
    }

    /// <summary>
    /// Called by UI_InGame to update icon + stack.
    /// </summary>
    public void UpdateQuickSlotUI(Inventory_Player.QuickSlot quickSlot)
    {
        if (quickSlot.item == null || quickSlot.slotStack <= 0)
        {
            itemIcon.sprite = defaultSprite;
            itemStackSize.text = "";
            return;
        }

        itemIcon.sprite = quickSlot.item.itemData.itemIcon;
        itemStackSize.text = $"x{quickSlot.slotStack}";
    }

    /// <summary>
    /// Called when player clicks this slot in-game.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[UI_QuickItemSlot] Clicked Slot {slotNumber} → Using item");
        ui.playerInventory.TryUseQuickItemInSlot(slotNumber);
    }
}
