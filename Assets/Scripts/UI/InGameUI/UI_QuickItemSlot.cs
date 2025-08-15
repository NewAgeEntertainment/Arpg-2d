using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_QuickItemSlot : MonoBehaviour, IPointerDownHandler
{
    [Header("Visuals")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemStackSize;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color filledIconColor = Color.white;

    [Header("Slot Number (1–4)")]
    [SerializeField, Range(1, 4)] private int slotNumber = 1;

    private UI_InGame ui;

    public int SlotNumber => slotNumber;

    private void Reset()
    {
        // Convenience when adding the component
        if (!itemIcon) itemIcon = GetComponentInChildren<Image>(true);
        if (!itemStackSize) itemStackSize = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void Awake()
    {
        // Find UI even if it’s in DDOL
        ui = FindFirstObjectByType<UI_InGame>(FindObjectsInactive.Include);

#if UNITY_EDITOR
        if (!itemIcon) Debug.LogWarning("[UI_QuickItemSlot] No itemIcon assigned.", this);
        if (!itemStackSize) Debug.LogWarning("[UI_QuickItemSlot] No itemStackSize assigned.", this);
#endif
        // Ensure we start empty
        SetEmpty();
    }

    /// <summary>
    /// Called by UI_InGame to paint this quick slot.
    /// </summary>
    public void UpdateQuickSlotUI(Inventory_Player.QuickSlot quickSlot)
    {
        if (quickSlot.item == null || quickSlot.slotStack <= 0)
        {
            SetEmpty();
            return;
        }

        var sprite = quickSlot.item.itemData != null ? quickSlot.item.itemData.itemIcon : null;
        SetIcon(sprite, quickSlot.slotStack);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (ui == null || ui.playerInventory == null) return;
        ui.playerInventory.TryUseQuickItemInSlot(slotNumber); // 1-based index as in your UI_InGame
    }

    // --------------------- helpers ---------------------

    private void SetEmpty()
    {
        if (itemIcon)
        {
            itemIcon.sprite = emptySprite;          // can be null
            itemIcon.color = emptyIconColor;       // dimmed
            itemIcon.enabled = true;                // <-- keep visible even with null sprite
        }
        if (itemStackSize) itemStackSize.text = string.Empty;
    }


    private void SetIcon(Sprite sprite, int stack)
    {
        if (itemIcon)
        {
            itemIcon.sprite = sprite ? sprite : emptySprite;
            itemIcon.color = sprite ? filledIconColor : emptyIconColor;
            itemIcon.enabled = (sprite != null || emptySprite != null);
        }
        if (itemStackSize)
            itemStackSize.text = (stack > 1) ? $"x{stack}" : string.Empty;
    }

    // If you ever assign this from code:
    public void SetSlotNumber(int n) => slotNumber = Mathf.Clamp(n, 1, 4);
}
