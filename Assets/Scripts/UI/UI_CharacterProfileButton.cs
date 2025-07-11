using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UI_CharacterProfileButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
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

    public void Setup(Player player, System.Action onClick)
    {
        // Unsubscribe old listeners if needed
        if (this.playerHealth != null) this.playerHealth.OnHealthUpdate -= UpdateHealthBar;
        if (this.playerMana != null) this.playerMana.OnManaUpdate -= UpdateManaBar;

        this.player = player;
        playerHealth = player.health;
        playerMana = player.mana;

        nameText.text = player.name;

        // Initial update
        UpdateHealthBar();
        UpdateManaBar();

        // Subscribe to live updates
        playerHealth.OnHealthUpdate += UpdateHealthBar;
        playerMana.OnManaUpdate += UpdateManaBar;

        // Setup click
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());

        // Always start highlight off for popup
        HighlightOff();
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
        // When hiding, unsubscribe so you don't leak listeners
        if (playerHealth != null) playerHealth.OnHealthUpdate -= UpdateHealthBar;
        if (playerMana != null) playerMana.OnManaUpdate -= UpdateManaBar;
    }
}
