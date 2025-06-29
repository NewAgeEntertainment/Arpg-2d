using UnityEngine;
using System.Collections.Generic;

public class UI_EquipmentInventory : MonoBehaviour
{
    [SerializeField] private Inventory_Equipment equipmentInventory;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private UI_EquipmentSlot slotPrefab;
    [SerializeField] private GameObject panelRoot; // drag your Panel root here!
    private List<UI_EquipmentSlot> uiSlots = new List<UI_EquipmentSlot>();
    private bool isVisible = false;

    private void Awake()
    {
        if (equipmentInventory == null)
            equipmentInventory = FindFirstObjectByType<Inventory_Equipment>();

        if (equipmentInventory == null)
        {
            Debug.LogError("[UI_EquipmentInventory] equipmentInventory is NULL! Assign it in the Inspector.");
            return; // Stop running the rest — prevents NullReference
        }

        equipmentInventory.OnInventoryChange += UpdateUI;

        if (slotContainer == null)
        {
            Debug.LogError("[UI_EquipmentInventory] slotContainer is NULL! Assign it in the Inspector.");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("[UI_EquipmentInventory] slotPrefab is NULL! Assign it in the Inspector.");
            return;
        }

        uiSlots.AddRange(slotContainer.GetComponentsInChildren<UI_EquipmentSlot>(true));
        Debug.Log($"[UI_EquipmentInventory] Initialized with {uiSlots.Count} slots.");

        gameObject.SetActive(isVisible); // start hidden
        UpdateUI();
    }


    public void Toggle()
    {
        isVisible = !isVisible;

        if (isVisible)
        {
            Debug.Log("[UI_EquipmentInventory] Opening → updating slots");
            UpdateUI();
        }

        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
        else
            gameObject.SetActive(isVisible);
    }

    public void UpdateUI()
    {
        var items = equipmentInventory.itemList;

        if (items.Count > uiSlots.Count)
        {
            int toAdd = items.Count - uiSlots.Count;
            Debug.Log($"[UI_EquipmentInventory] Adding {toAdd} new slots dynamically.");
            for (int i = 0; i < toAdd; i++)
                uiSlots.Add(Instantiate(slotPrefab, slotContainer));
        }

        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < items.Count)
                uiSlots[i].UpdateSlot(items[i]);
            else
                uiSlots[i].Clear();
        }
    }

    private void OnDestroy()
    {
        if (equipmentInventory != null)
            equipmentInventory.OnInventoryChange -= UpdateUI;
    }
}
