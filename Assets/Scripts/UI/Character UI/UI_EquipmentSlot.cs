using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EquipmentSlot : UI_ItemSlot, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UI_EquipmentToolTip equipmentToolTip;
    [SerializeField] private UnityEngine.UI.Button button;

    private bool isSelectable = false;

    private void Awake()
    {
        SetSelectable(false); // Default to not selectable until explicitly enabled
    }

    public void SetItem(Inventory_Item item)
    {
        UpdateSlot(item);
    }

    public void SetEquipmentToolTip(UI_EquipmentToolTip toolTip)
    {
        equipmentToolTip = toolTip;
    }

    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
        if (button != null)
            button.interactable = selectable;

        Debug.Log($"[UI_EquipmentSlot] SetSelectable: {selectable} on {gameObject.name}");
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!isSelectable) return;

        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI == null) return;

        Debug.Log($"[UI_EquipmentSlot] Attempting to equip: {itemInSlot?.itemData?.itemName}");

        if (equipmentUI.IsInSelectionMode() && equipmentUI.IsSelectionEnabled())
        {
            Debug.Log($"[UI_EquipmentSlot] Equipping: {itemInSlot.itemData.itemName}");
            equipmentUI.SwapEquippedItem(itemInSlot);
        }
        else
        {
            Debug.Log("[UI_EquipmentSlot] Not in selection mode or selection not enabled.");
        }
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelectable) return;

        if (itemInSlot != null && equipmentToolTip != null)
        {
            equipmentToolTip.ShowEquipmentToolTip(true, itemInSlot);
        }

        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI != null)
        {
            equipmentUI.PlayerStatsPanel?.PreviewItem(itemInSlot);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelectable) return;

        equipmentToolTip?.ShowEquipmentToolTip(false, null);

        var equipmentUI = FindObjectOfType<UI_EquipmentInventory>();
        if (equipmentUI != null)
        {
            equipmentUI.PlayerStatsPanel?.ClearPreview();
        }
    }

}
