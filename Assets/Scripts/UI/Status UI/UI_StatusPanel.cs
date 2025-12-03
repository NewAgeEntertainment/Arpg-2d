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

    [Header("Stat Slots (right side panel)")]
    [SerializeField] private UI_StatSlot[] statSlots;   // drag all your slots here in the Inspector

    private Player player;
    private Player_Stats stats;

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
        UnsubscribeFromStats();
    }

    private void OnDestroy()
    {
        UnsubscribeFromStats();
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

    // --------- Open / Close ---------

    public void OpenPanel(Player p)
    {
        gameObject.SetActive(true);
        isOpen = true;
        UpdateStatus(p);
    }

    public void ClosePanel()
    {
        isOpen = false;
        gameObject.SetActive(false);
        var ui = FindObjectOfType<UI>();
        ui?.OpenMainMenuDirect();
    }

    // --------- Binding & refresh ---------

    public void UpdateStatus(Player p)
    {
        if (p == null) return;

        BindPlayer(p);

        RefreshHeader();
        RefreshExpTexts();
        RefreshSexTexts();
        RefreshHPMP();
        RefreshAllStatSlots();
    }

    private void BindPlayer(Player p)
    {
        if (player == p && stats == (p != null ? p.stats : null))
            return;

        UnsubscribeFromStats();

        player = p;
        stats = p != null ? p.stats : null;

        if (stats != null)
        {
            stats.OnExpChanged += HandleExpChanged;
            stats.OnSexExpChanged += HandleSexExpChanged;
            stats.OnLevelChanged += HandleLevelChanged;
            // optional but recommended, as described above:
            stats.OnStatsChanged += HandleStatsChanged;
        }

        // Make sure all slots know which stats to use
        if (stats != null && statSlots != null)
        {
            foreach (var slot in statSlots)
            {
                if (slot == null) continue;
                slot.Setup(stats);
            }
        }
    }

    private void UnsubscribeFromStats()
    {
        if (stats == null) return;

        stats.OnExpChanged -= HandleExpChanged;
        stats.OnSexExpChanged -= HandleSexExpChanged;
        stats.OnLevelChanged -= HandleLevelChanged;
        stats.OnStatsChanged -= HandleStatsChanged;

        stats = null;
    }

    private void RefreshHeader()
    {
        if (player == null || stats == null) return;

        if (nameText != null)
            nameText.text = player.name;

        if (portraitImage != null)
            portraitImage.sprite = player.Portrait;

        if (levelText != null)
            levelText.text = "Lv " + stats.CurrentLevel;

        if (sexLevelText != null)
            sexLevelText.text = "Sex Lv " + stats.CurrentSexLevel;

        if (bioText != null)
            bioText.text = player.Bio;
    }

    private void RefreshExpTexts()
    {
        if (stats == null) return;

        int curExp = Mathf.FloorToInt(stats.CurrentEXP);
        int nextReq = Mathf.CeilToInt(stats.GetNextLevelRequirement());

        if (currentExpText != null)
            currentExpText.text = curExp.ToString("N0");

        if (nextLevelExpText != null)
            nextLevelExpText.text = nextReq.ToString("N0");
    }

    private void RefreshSexTexts()
    {
        if (stats == null) return;

        int sexLevel = stats.CurrentSexLevel;
        int curSexExp = Mathf.FloorToInt(stats.CurrentSexEXP);
        int nextSexReq = Mathf.CeilToInt(stats.GetNextSexLevelRequirement());

        if (sexLevelText != null)
            sexLevelText.text = "Sex Lv " + sexLevel;

        if (currentSexExpText != null)
            currentSexExpText.text = curSexExp.ToString("N0");

        if (nextSexExpText != null)
            nextSexExpText.text = nextSexReq.ToString("N0");
    }

    private void RefreshHPMP()
    {
        if (player == null || stats == null) return;

        if (hpText != null)
            hpText.text = $"{player.health.GetCurrentHealth()} / {stats.GetMaxHealth()}";

        if (mpText != null)
            mpText.text = $"{player.mana.GetCurrentMana()} / {stats.GetMaxMana()}";
    }

    private void RefreshAllStatSlots()
    {
        if (stats == null || statSlots == null) return;

        foreach (var slot in statSlots)
        {
            if (slot == null) continue;
            slot.UpdateStatValue();   // this uses GetBaseDamage / GetBaseSexDamage etc.
        }
    }

    // --------- Event handlers from Player_Stats ---------

    private void HandleExpChanged(float cur, float next)
    {
        if (!isOpen) return;

        if (currentExpText != null)
            currentExpText.text = Mathf.FloorToInt(cur).ToString("N0");
        if (nextLevelExpText != null)
            nextLevelExpText.text = Mathf.CeilToInt(next).ToString("N0");
    }

    private void HandleSexExpChanged(float cur, float next, int level)
    {
        if (!isOpen) return;

        if (sexLevelText != null)
            sexLevelText.text = "Sex Lv " + level;

        if (currentSexExpText != null)
            currentSexExpText.text = Mathf.FloorToInt(cur).ToString("N0");
        if (nextSexExpText != null)
            nextSexExpText.text = Mathf.CeilToInt(next).ToString("N0");
    }

    private void HandleLevelChanged(int newLevel)
    {
        if (!isOpen) return;

        if (levelText != null)
            levelText.text = "Lv " + newLevel;

        RefreshHPMP();          // max HP/MP changed
        RefreshAllStatSlots();  // damage, crit, etc. may have changed
    }

    private void HandleStatsChanged()
    {
        if (!isOpen) return;

        RefreshHPMP();
        RefreshAllStatSlots();  // 🔹 Strength / Stroke changed → Damage & Sexual Damage update
    }

    // --------- Cancel from UI_Panel ---------

    public override bool HandleCancel()
    {
        ClosePanel();
        return true;
    }
}
