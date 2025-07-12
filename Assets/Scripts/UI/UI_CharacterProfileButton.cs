using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class UI_CharacterProfileButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Linked Player")]
    [SerializeField] public Player linkedPlayer; // made public for inventory access

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

    private Entity_Health playerHealth;
    private Entity_Mana playerMana;
    private Player player;
    private Action<Player> onClickCallback;

    public void Setup(Player player, Action<Player> onClick)
    {
        if (this.playerHealth != null) this.playerHealth.OnHealthUpdate -= UpdateHealthBar;
        if (this.playerMana != null) this.playerMana.OnManaUpdate -= UpdateManaBar;

        if (nameText == null)
        {
            Debug.LogError("[ProfileButton] nameText is not assigned in the inspector.");
            return;
        }

        this.player = player;
        this.onClickCallback = onClick;

        playerHealth = player.health;
        playerMana = player.mana;

        nameText.text = player.name;

        UpdateHealthBar();
        UpdateManaBar();

        playerHealth.OnHealthUpdate += UpdateHealthBar;
        playerMana.OnManaUpdate += UpdateManaBar;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(player));
        Debug.Log("working");
        HighlightOff();
    }

    // Overload for when linkedPlayer is already assigned
    public void Setup(Action<Player> onClick)
    {
        if (linkedPlayer == null)
        {
            Debug.LogError("[ProfileButton] linkedPlayer is not assigned!");
            return;
        }

        Setup(linkedPlayer, onClick);
    }

    public void RefreshBars()
    {
        UpdateHealthBar();
        UpdateManaBar();
    }

    private void UpdateHealthBar()
    {
        if (playerHealth == null || player?.stats == null) return;

        float current = Mathf.RoundToInt(playerHealth.GetCurrentHealth());
        float max = player.stats.GetMaxHealth();
        healthSlider.value = current / max;

        if (healthText != null)
            healthText.text = $"{current} / {max}";
    }

    private void UpdateManaBar()
    {
        if (playerMana == null || player?.stats == null) return;

        float current = Mathf.RoundToInt(playerMana.GetCurrentMana());
        float max = player.stats.GetMaxMana();
        manaSlider.value = current / max;

        if (manaText != null)
            manaText.text = $"{current} / {max}";
    }

    public void HighlightOn() => highlighter?.SetActive(true);
    public void HighlightOff() => highlighter?.SetActive(false);

    public void OnPointerEnter(PointerEventData eventData) => HighlightOn();
    public void OnPointerExit(PointerEventData eventData) => HighlightOff();

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthUpdate -= UpdateHealthBar;
        if (playerMana != null) playerMana.OnManaUpdate -= UpdateManaBar;
    }
}
