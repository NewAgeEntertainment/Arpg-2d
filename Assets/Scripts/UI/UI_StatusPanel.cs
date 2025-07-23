using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rewired;

public class UI_StatusPanel : UI_Panel
{
    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "Cancel";
    private Rewired.Player rPlayer;

    [Header("Basic Info")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI classText;
    public Image portraitImage;
    public TextMeshProUGUI levelText, currentExpText, nextLevelExpText;

    [Header("Stats")]
    public TextMeshProUGUI hpText, mpText;
    public TextMeshProUGUI bioText;

    private void Start()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);
    }

    private void Update()
    {
        if (rPlayer != null && rPlayer.GetButtonDown(cancelAction))
        {
            HandleCancel();
        }
    }

    public void UpdateStatus(Player player)
    {
        nameText.text = player.name;
        portraitImage.sprite = player.Portrait;
        levelText.text = "Lv " + player.stats.CurrentLevel;
        currentExpText.text = $"{player.stats.CurrentEXP}";
        nextLevelExpText.text = $"Next in: {player.stats.GetNextLevelRequirement()}";
        hpText.text = $"{player.health.GetCurrentHealth()} / {player.stats.GetMaxHealth()}";
        mpText.text = $"{player.mana.GetCurrentMana()} / {player.stats.GetMaxMana()}";
        bioText.text = player.Bio;
    }

    public override bool HandleCancel()
    {
        ClosePanel();
        return true;
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        var ui = FindObjectOfType<UI>();
        ui?.OpenMainMenuDirect();
    }
}
