using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class UI_CharacterProfileButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Linked Player (optional)")]
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

    // Live refs
    private Entity_Health playerHealth;
    private Entity_Mana playerMana;
    private Player player;

    // Legacy callback (still supported)
    private Action<Player> onClickCallback;

    // Use-item context
    private Inventory_Item _pendingItem;
    private Inventory_Player _inventory;
    private Action _afterUse;

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

    /// <summary>Full setup with explicit player (initial draw + live subscriptions).</summary>
    public void Setup(Player player, Action<Player> onClick)
    {
        UnsubscribeVitals();

        // If a prefab or invalid scene object was passed, grab the live Player:
        if (player == null || !player.gameObject.scene.IsValid())
            player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (player == null)
        {
            Debug.LogWarning("[ProfileButton] No runtime Player found.");
            return;
        }

        this.player = player;
        this.onClickCallback = onClick;

        playerHealth = player.health ?? player.GetComponent<Entity_Health>();
        playerMana = player.mana ?? player.GetComponent<Entity_Mana>();

        if (nameText != null) nameText.text = string.IsNullOrEmpty(player.name) ? "Player" : player.name;

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


    /// <summary>Setup using serialized linkedPlayer.</summary>
    public void Setup(Action<Player> onClick)
    {
        if (linkedPlayer == null)
        {
            Debug.LogError("[ProfileButton] linkedPlayer is not assigned!");
            return;
        }
        Setup(linkedPlayer, onClick);
    }

    /// <summary>Provide an item + inventory so clicking this profile uses the item on this player.</summary>
    public void SetUseItemContext(Inventory_Item pendingItem, Inventory_Player inventory, Action afterUse = null)
    {
        _pendingItem = pendingItem;
        _inventory = inventory;
        _afterUse = afterUse;
    }

    private void HandleClick()
    {
        // Prefer use-item context, else use legacy delegate:
        if (TryUsePendingItem()) return;
        onClickCallback?.Invoke(player);
    }

    private bool TryUsePendingItem()
    {
        if (_pendingItem == null || _pendingItem.itemData == null || _inventory == null)
            return false;

        // Resolve a live player if needed
        if (player == null || !player.gameObject.scene.IsValid())
            player = linkedPlayer && linkedPlayer.gameObject.scene.IsValid()
                ? linkedPlayer
                : FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (player == null)
        {
            Debug.LogError("[ProfileButton] No runtime Player to use the item on.");
            return false;
        }

        // Only usable consumables here
        if (_pendingItem.itemData.itemType != ItemType.Consumable || !_pendingItem.itemData.isUsable)
            return false;

        // Find a concrete inventory instance to consume
        var instance = FindSameItemInstance(_inventory, _pendingItem)
                       ?? _inventory.itemList.Find(it => it != null && it.itemData == _pendingItem.itemData);

        if (instance == null)
        {
            Debug.LogWarning("[ProfileButton] No matching item instance in inventory.");
            return false;
        }

        // ✅ Use the same pipeline as quickslots
        _inventory.TryUseItem(instance, player);

        // Refresh local bars + outer UI if provided
        RefreshBars();
        _afterUse?.Invoke();
        return true;
    }



    /// <summary>Try to match an inventory instance by data and (if present) instance modifiers.</summary>
    private Inventory_Item FindSameItemInstance(Inventory_Player inv, Inventory_Item sample)
    {
        // Prefer an exact instance reference if the same object is in the list:
        foreach (var it in inv.itemList)
            if (ReferenceEquals(it, sample)) return it;

        // Otherwise, match by ItemData (and optionally modifiers if you rely on them):
        foreach (var it in inv.itemList)
        {
            if (it == null || it.itemData != sample.itemData) continue;
            // If you track per-instance modifiers, you could compare them here.
            return it;
        }
        return null;
    }

    public void RefreshBars()
    {
        UpdateHealthBar();
        UpdateManaBar();
    }

    private void UpdateHealthBar()
    {
        if (player == null) return;
        playerHealth ??= player.GetComponent<Entity_Health>();
        var stats = player.stats ?? player.GetComponent<Player_Stats>();
        if (playerHealth == null || stats == null || healthSlider == null) return;

        float max = Mathf.Max(1f, stats.GetMaxHealth());
        float cur = Mathf.RoundToInt(playerHealth.GetCurrentHealth());
        healthSlider.value = Mathf.Clamp01(cur / max);
        if (healthText) healthText.text = $"{cur} / {max}";
    }

    private void UpdateManaBar()
    {
        if (player == null) return;
        playerMana ??= player.GetComponent<Entity_Mana>();
        var stats = player.stats ?? player.GetComponent<Player_Stats>();
        if (playerMana == null || stats == null || manaSlider == null) return;

        float max = Mathf.Max(1f, stats.GetMaxMana());
        float cur = Mathf.RoundToInt(playerMana.GetCurrentMana());
        manaSlider.value = Mathf.Clamp01(cur / max);
        if (manaText) manaText.text = $"{cur} / {max}";
    }


    public void HighlightOn() => highlighter?.SetActive(true);
    public void HighlightOff() => highlighter?.SetActive(false);

    public void OnPointerEnter(PointerEventData eventData) => HighlightOn();
    public void OnPointerExit(PointerEventData eventData) => HighlightOff();

    public void SetSelected(bool selected)
    {
        if (selected) HighlightOn();
        else HighlightOff();
    }

}
