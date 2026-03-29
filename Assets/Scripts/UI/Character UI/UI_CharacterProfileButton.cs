using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class UI_CharacterProfileButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Linked Character (optional)")]
    [SerializeField] public Component linkedCharacter;

    [Header("Core")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Portrait")]
    [SerializeField] private Image portraitImage;

    [Header("Bars")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Highlight")]
    [SerializeField] private GameObject highlighter;

    private Component character;
    private Entity_Health playerHealth;
    private Entity_Mana playerMana;
    private Entity_Stats entityStats;

    private Action<Component> onClickCallback;

    private Inventory_Item pendingItem;
    private Inventory_Player inventory;
    private Action afterUse;

    private void OnDisable()
    {
        UnsubscribeVitals();
    }

    private void OnDestroy()
    {
        UnsubscribeVitals();

        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    private void UnsubscribeVitals()
    {
        if (playerHealth != null)
            playerHealth.OnHealthUpdate -= UpdateHealthBar;

        if (playerMana != null)
            playerMana.OnManaUpdate -= UpdateManaBar;

        playerHealth = null;
        playerMana = null;
        entityStats = null;
    }

    public void Setup(Component target, Action<Component> onClick)
    {
        UnsubscribeVitals();

        if (target == null || !target.gameObject.scene.IsValid())
        {
            Debug.LogWarning("[ProfileButton] No valid runtime character found.");
            return;
        }

        character = target;
        linkedCharacter = target;
        onClickCallback = onClick;

        CacheCharacterRefs();
        UpdateName();
        UpdateHealthBar();
        UpdateManaBar();

        if (playerHealth != null)
            playerHealth.OnHealthUpdate += UpdateHealthBar;

        if (playerMana != null)
            playerMana.OnManaUpdate += UpdateManaBar;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        HighlightOff();
    }

    public void Setup(Action<Component> onClick)
    {
        if (linkedCharacter == null)
        {
            Debug.LogError("[ProfileButton] linkedCharacter is not assigned!");
            return;
        }

        Setup(linkedCharacter, onClick);
    }

    public void SetUseItemContext(Inventory_Item pendingItem, Inventory_Player inventory, Action afterUse = null)
    {
        this.pendingItem = pendingItem;
        this.inventory = inventory;
        this.afterUse = afterUse;
    }

    private void HandleClick()
    {
        if (TryUsePendingItem())
            return;

        onClickCallback?.Invoke(character);
    }

    private bool TryUsePendingItem()
    {
        if (pendingItem == null || pendingItem.itemData == null || inventory == null)
            return false;

        if (character == null || !character.gameObject.scene.IsValid())
            character = linkedCharacter;

        if (character == null)
        {
            Debug.LogError("[ProfileButton] No runtime character to use the item on.");
            return false;
        }

        if (pendingItem.itemData.itemType != ItemType.Consumable || !pendingItem.itemData.isUsable)
            return false;

        Inventory_Item instance =
            FindSameItemInstance(inventory, pendingItem) ??
            inventory.itemList.Find(it => it != null && it.itemData == pendingItem.itemData);

        if (instance == null)
        {
            Debug.LogWarning("[ProfileButton] No matching item instance in inventory.");
            return false;
        }

        bool used = inventory.TryUseItemChecked(instance, character);
        if (!used)
            return false;

        RefreshBars();
        afterUse?.Invoke();
        return true;
    }

    private Inventory_Item FindSameItemInstance(Inventory_Player inv, Inventory_Item sample)
    {
        foreach (var it in inv.itemList)
        {
            if (ReferenceEquals(it, sample))
                return it;
        }

        foreach (var it in inv.itemList)
        {
            if (it == null || it.itemData != sample.itemData)
                continue;

            return it;
        }

        return null;
    }

    private void CacheCharacterRefs()
    {
        if (character == null)
            return;

        playerHealth = character.GetComponent<Entity_Health>();
        playerMana = character.GetComponent<Entity_Mana>();
        entityStats = character.GetComponent<Entity_Stats>();
    }

    private void UpdateName()
    {
        if (nameText == null || character == null)
            return;

        string displayName = character.name;

        Player player = character.GetComponent<Player>();
        if (player != null && !string.IsNullOrWhiteSpace(player.name))
        {
            displayName = player.name;
        }
        else
        {
            Companion companion = character.GetComponent<Companion>();
            if (companion != null && !string.IsNullOrWhiteSpace(companion.name))
                displayName = companion.name;
        }

        nameText.text = string.IsNullOrWhiteSpace(displayName) ? "Character" : displayName;
    }

    public void SetPortrait(Sprite sprite)
    {
        if (portraitImage == null)
            return;

        portraitImage.sprite = sprite;
        portraitImage.enabled = sprite != null;
    }

    public void ClearPortrait()
    {
        SetPortrait(null);
    }

    public void RefreshBars()
    {
        CacheCharacterRefs();
        UpdateName();
        UpdateHealthBar();
        UpdateManaBar();
    }

    private void UpdateHealthBar()
    {
        if (character == null)
            return;

        playerHealth ??= character.GetComponent<Entity_Health>();
        entityStats ??= character.GetComponent<Entity_Stats>();

        if (playerHealth == null || entityStats == null || healthSlider == null)
            return;

        float max = Mathf.Max(1f, entityStats.GetMaxHealth());
        float cur = Mathf.RoundToInt(playerHealth.GetCurrentHealth());

        healthSlider.value = Mathf.Clamp01(cur / max);

        if (healthText != null)
            healthText.text = $"{cur} / {max}";
    }

    private void UpdateManaBar()
    {
        if (character == null)
            return;

        playerMana ??= character.GetComponent<Entity_Mana>();
        entityStats ??= character.GetComponent<Entity_Stats>();

        if (playerMana == null || entityStats == null || manaSlider == null)
            return;

        float max = Mathf.Max(1f, entityStats.GetMaxMana());
        float cur = Mathf.RoundToInt(playerMana.GetCurrentMana());

        manaSlider.value = Mathf.Clamp01(cur / max);

        if (manaText != null)
            manaText.text = $"{cur} / {max}";
    }

    public void HighlightOn()
    {
        highlighter?.SetActive(true);
    }

    public void HighlightOff()
    {
        highlighter?.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        HighlightOn();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HighlightOff();
    }

    public void SetSelected(bool selected)
    {
        if (selected) HighlightOn();
        else HighlightOff();
    }
}