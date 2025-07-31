using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rewired;

public class UI_StatusPanel : UI_Panel
{
    [Header("Rewired Input (match UI_EquipmentInventory.cs)")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "UICancel";
    private Rewired.Player rPlayer;

    private float cancelCooldown = 0f;
    private const float cancelCooldownDuration = 0.2f;

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    [Header("Basic Info")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI classText;
    public Image portraitImage;

    [Header("Normal EXP")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI currentExpText;
    public TextMeshProUGUI nextLevelExpText;

    [Header("Sex EXP")]
    public TextMeshProUGUI sexLevelText;
    public TextMeshProUGUI currentSexExpText;
    public TextMeshProUGUI nextSexExpText;

    [Header("Stats")]
    public TextMeshProUGUI hpText, mpText;
    public TextMeshProUGUI bioText;

    private void Awake()
    {
        rPlayer = ReInput.players.GetPlayer(playerID);
    }

    private void OnEnable()
    {
        isOpen = true;
    }

    private void OnDisable()
    {
        isOpen = false;
    }

    private void Update()
    {
        if (!isOpen || rPlayer == null) return;

        if (cancelCooldown > 0f)
            cancelCooldown -= Time.deltaTime;

        if (cancelCooldown <= 0f && rPlayer.GetButtonDown(cancelAction))
        {
            HandleCancel();
            cancelCooldown = cancelCooldownDuration;
        }
    }

    public void OpenPanel(Player player)
    {
        gameObject.SetActive(true);
        isOpen = true;
        UpdateStatus(player);
    }

    public void ClosePanel()
    {
        isOpen = false;
        gameObject.SetActive(false);
        var ui = FindObjectOfType<UI>();
        ui?.OpenMainMenuDirect();
    }

    public void UpdateStatus(Player player)
    {
        if (player == null) return;

        nameText.text = player.name;
        portraitImage.sprite = player.Portrait;
        levelText.text = "Lv " + player.stats.CurrentLevel;

        // Normal EXP
        int curExp = Mathf.FloorToInt(player.stats.CurrentEXP);
        int nextReq = Mathf.CeilToInt(player.stats.GetNextLevelRequirement());
        currentExpText.text = curExp.ToString("N0");
        nextLevelExpText.text = nextReq.ToString("N0");

        // Sex EXP
        // Sex EXP
        int sexLevel = player.SexLevel;
        int curSexExp = Mathf.FloorToInt(player.CurrentSexExp);
        int nextSexReq = Mathf.CeilToInt(player.GetNextSexLevelRequirementSex());

        sexLevelText.text = "Sex Lv " + sexLevel;
        currentSexExpText.text = curSexExp.ToString("N0");
        nextSexExpText.text = nextSexReq.ToString("N0");

        // Other stats
        hpText.text = $"{player.health.GetCurrentHealth()} / {player.stats.GetMaxHealth()}";
        mpText.text = $"{player.mana.GetCurrentMana()} / {player.stats.GetMaxMana()}";
        bioText.text = player.Bio;
    }

    public override bool HandleCancel()
    {
        ClosePanel();
        return true;
    }
}
