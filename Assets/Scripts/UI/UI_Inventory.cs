using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    [SerializeField] private Inventory_Player inventory;
    [SerializeField] private UI_ItemSlotParent inventorySlotsParent;
    [SerializeField] private UI_EquippedSlot[] uiEquipSlots;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory_Player>();

        if (uiEquipSlots == null || uiEquipSlots.Length == 0)
            uiEquipSlots = GetComponentsInChildren<UI_EquippedSlot>();

        if (inventory != null)
            inventory.OnInventoryChange += UpdateUI;

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChange -= UpdateUI;
    }

    private void UpdateUI()
    {
        if (inventory == null) return;

        inventorySlotsParent.UpdateSlots(inventory.itemList);
        UpdateEquipmentSlots();
    }

    private void UpdateEquipmentSlots()
    {
        if (inventory == null || inventory.equipList == null) return;

        for (int i = 0; i < uiEquipSlots.Length; i++)
        {
            if (i >= inventory.equipList.Count) break;

            var slot = inventory.equipList[i];
            uiEquipSlots[i].UpdateSlot(slot.HasItem() ? slot.equipedItem : null);
        }
    }
}