using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    private Player player;

    [SerializeField] private RectTransform healthRect;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [SerializeField] private RectTransform manaRect;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI manaText;

    private void Start()
    {
        player = FindObjectOfType<Player>();
        player.health.OnHealthUpdate += UpdateHealthBar; // Subscribe to health update event
        player.mana.OnManaUpdate += UpdateManaBar; // Subscribe to mana update event
    }

    private void UpdateHealthBar()
    {
        float currentHealth = Mathf.RoundToInt (player.health.GetCurrentHealth());
        float maxHealth = player.stats.GetMaxHealth();

        healthText.text = currentHealth + " / " + maxHealth; // display current health and max health text
        healthSlider.value = player.health.GetHealthPercent(); // update health slider value
    }

    private void UpdateManaBar()
    {
        float currentMana = Mathf.RoundToInt(player.mana.GetCurrentMana());
        float maxMana = player.stats.GetMaxMana();
        manaText.text = currentMana + " / " + maxMana; // display current mana and max mana text
        manaSlider.value = player.mana.GetManaPercent(); // update mana slider value
    }
}

