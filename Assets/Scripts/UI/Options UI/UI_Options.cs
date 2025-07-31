using UnityEngine;
using UnityEngine.UI;
using Rewired;

public class UI_Options : UI_Panel
{
    [Header("Rewired Input")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private string cancelAction = "Cancel";
    private Rewired.Player rPlayer;

    [Header("Option Toggles")]
    [SerializeField] private Toggle healthBarToggle;
    [SerializeField] private Toggle manaBarToggle;

    private Player player;

    private void Awake()
    {
        player = FindFirstObjectByType<Player>();

        if (healthBarToggle != null)
            healthBarToggle.onValueChanged.AddListener(OnHealthToggleChanged);

        if (manaBarToggle != null)
            manaBarToggle.onValueChanged.AddListener(OnManaToggleChanged);
    }

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

    public void OpenOptions()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        transform.SetAsLastSibling(); // Force to front if overlapped
        Debug.Log("[UI_Options] Options panel opened.");
    }

    private void OnHealthToggleChanged(bool isOn)
    {
        if (player != null && player.health != null)
            player.health.EnableHealthBar(isOn);
    }

    private void OnManaToggleChanged(bool isOn)
    {
        if (player != null && player.mana != null)
            player.mana.EnableManaBar(isOn);
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

        Debug.Log("[UI_Options] Closed and returned to Main Menu.");
    }
}
