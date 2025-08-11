using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class UI_CharacterProfileButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Linked Player")]
    [SerializeField] public Player linkedPlayer;   // assign in Inspector if desired

    [Header("Core")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Bars")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Highlight")]
    [SerializeField] private GameObject highlighter;

    // Live references
    private Entity_Health playerHealth;
    private Entity_Mana playerMana;
    private Player player;

    // Old callback (still supported)
    private Action<Player> onClickCallback;

    // >>> NEW: Use-item context <<<
    private Inventory_Item _pendingItem;                 // item to use on click
    private Inventory_Player _inventory;                 // where we remove the item from
    private Action _afterUse;                            // UI nudge (e.g., refresh/close) after use

    private void OnDisable() => UnsubscribeVitals();

    private void OnDestroy()
    {
        UnsubscribeVitals();
        if (button != null) button.onClick.RemoveAllListeners();
    }

    private void UnsubscribeVitals()
    {
        if (playerHealth != null) playerHealth.OnHealthUpdate -= UpdateHealthBar;
        if (playerMana != null) playerMana.OnManaUpdate -= UpdateManaBar;
        playerHealth = null;
        playerMana = null;
    }

    /// <summary>
    /// Full setup when you know the player explicitly. (Legacy path; keeps your old code working.)
    /// </summary>
    public void Setup(Player player, Action<Player> onClick)
    {
        UnsubscribeVitals();

        if (player == null)
        {
            Debug.LogWarning("[ProfileButton] Setup called with null player.");
            return;
        }

        this.player = player;
        this.onClickCallback = onClick;

        playerHealth = player.health;
        playerMana = player.mana;

        if (nameText != null) nameText.text = string.IsNullOrEmpty(player.name) ? "Player" : player.name;

        // initial draw + subscribe
        UpdateHealthBar();
        UpdateManaBar();

        if (playerHealth != null) playerHealth.OnHealthUpdate += UpdateHealthBar;
        if (playerMana != null) playerMana.OnManaUpdate += UpdateManaBar;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        HighlightOff();
    }

    /// <summary>
    /// Setup using the serialized linkedPlayer (assign in Inspector).
    /// </summary>
    public void Setup(Action<Player> onClick)
    {
        if (linkedPlayer == null)
        {
            Debug.LogError("[ProfileButton] linkedPlayer is not assigned!");
            return;
        }
        Setup(linkedPlayer, onClick);
    }

    /// <summary>
    /// >>> NEW: Provide an item + inventory so clicking this profile will USE the item on this player. <<<
    /// Optionally provide an afterUse callback (e.g., to refresh UI/close panel).
    /// </summary>
    public void SetUseItemContext(Inventory_Item pendingItem, Inventory_Player inventory, Action afterUse = null)
    {
        _pendingItem = pendingItem;
        _inventory = inventory;
        _afterUse = afterUse;
    }

    private void HandleClick()
    {
        // If we have a pending item, try to use it directly.
        if (TryUsePendingItem()) return;

        // Fallback to legacy callback behavior.
        onClickCallback?.Invoke(player);
    }

    private bool TryUsePendingItem()
    {
        if (_pendingItem == null || _pendingItem.itemData == null || _inventory == null || player == null)
            return false;

        // Usable only?
        if (_pendingItem.itemData.itemType != ItemType.Consumable || !_pendingItem.itemData.isUsable)
            return false;

        // Find the actual instance in inventory (so we can decrement/remove)
        var instance = _inventory.FindSameItem(_pendingItem);
        if (instance == null) return false;

        // Validate & execute effect
        if (instance.itemEffect != null && instance.itemEffect.CanBeUsed(player))
        {
            instance.itemEffect.Subscribe(player);
            instance.itemEffect.ExecuteEffect(player);

            // Remove one from inventory and notify
            _inventory.RemoveOneItem(instance);
            _inventory.TriggerUpdateUI();

            // Refresh our bars immediately after effect
            RefreshBars();

            // Let caller update other UI bits if they want
            _afterUse?.Invoke();
            return true;
        }

        return false;
    }

    public void RefreshBars()
    {
        UpdateHealthBar();
        UpdateManaBar();
    }

    private void UpdateHealthBar()
    {
        if (player == null || player.stats == null || playerHealth == null || healthSlider == null) return;

        float max = Mathf.Max(1f, player.stats.GetMaxHealth());
        float current = Mathf.RoundToInt(playerHealth.GetCurrentHealth());

        healthSlider.value = Mathf.Clamp01(current / max);

        if (healthText != null)
            healthText.text = $"{current} / {max}";
    }

    private void UpdateManaBar()
    {
        if (player == null || player.stats == null || playerMana == null || manaSlider == null) return;

        float max = Mathf.Max(1f, player.stats.GetMaxMana());
        float current = Mathf.RoundToInt(playerMana.GetCurrentMana());

        manaSlider.value = Mathf.Clamp01(current / max);

        if (manaText != null)
            manaText.text = $"{current} / {max}";
    }

    public void HighlightOn() => highlighter?.SetActive(true);
    public void HighlightOff() => highlighter?.SetActive(false);

    public void OnPointerEnter(PointerEventData eventData) => HighlightOn();
    public void OnPointerExit(PointerEventData eventData) => HighlightOff();

    // Optional helper for selection state from outside.
    public void SetSelected(bool selected)
    {
        if (selected) HighlightOn(); else HighlightOff();
    }
}
