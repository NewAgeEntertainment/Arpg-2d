using UnityEngine;
using PixelCrushers.DialogueSystem; // optional; used for a friendly message

public class SexyTimeStarter : MonoBehaviour
{
    [Header("Assign the object that has SexyTimeLogic")]
    [SerializeField] private GameObject sexyTimeLogicObject;

    [Header("Optional partner override (leave empty to use this GO's Entity_Stats)")]
    [SerializeField] private Entity_Stats partnerOverride;

    [Header("UI feedback")]
    [SerializeField] private string notEnoughGoldMessage = "You need {0} gold.";

    // (kept from your version; handy for debugging)
    [Header("Fallback key (debug)")]
    [SerializeField] private bool allowKeyboardFallback = false;
    [SerializeField] private KeyCode fallbackKey = KeyCode.L;

    private SexyTimeLogic logic;
    private SexyTimeInputRouter router;

    void Awake()
    {
        if (sexyTimeLogicObject == null)
        {
            Debug.LogError("[SexyTimeStarter] 'sexyTimeLogicObject' is not assigned.");
            return;
        }

        logic = sexyTimeLogicObject.GetComponent<SexyTimeLogic>();
        router = sexyTimeLogicObject.GetComponent<SexyTimeInputRouter>();

        if (!logic) Debug.LogError("[SexyTimeStarter] No SexyTimeLogic found on assigned object.");
        if (!router) Debug.Log("[SexyTimeStarter] No SexyTimeInputRouter found (ok).");
    }

    void Update()
    {
        if (!allowKeyboardFallback || logic == null) return;
        if (Input.GetKeyDown(fallbackKey)) StartSexyTimePaid(0); // debug free start
    }

    // -------------------- Dialogue-friendly API --------------------
    // Sequencer: SendMessage(StartSexyTimeIfPaid,800,listener)
    public void StartSexyTimeIfPaid(string costStr)
    {
        int cost = 0;
        int.TryParse(costStr, out cost);
        StartSexyTimePaid(cost);
    }

    // You can also call this directly from code/UnityEvent
    public bool StartSexyTimePaid(int cost)
    {
        if (logic == null) return false;

        var inv = FindFirstObjectByType<Inventory_Player>(FindObjectsInactive.Include);
        if (inv == null)
        {
            Debug.LogWarning("[SexyTimeStarter] No Inventory_Player found.");
            return false;
        }

        if (inv.gold < cost)
        {
            // friendly feedback
            if (DialogueManager.instance != null && !string.IsNullOrEmpty(notEnoughGoldMessage))
                DialogueManager.ShowAlert(string.Format(notEnoughGoldMessage, cost));
            else
                Debug.Log($"[SexyTimeStarter] Not enough gold. Need {cost}, have {inv.gold}.");

            return false;
        }

        // Take payment and refresh HUD
        if (cost > 0)
        {
            inv.gold -= cost;
            UI.Instance?.UpdateGoldUI(inv.gold);
        }

        // Bind partner (this NPC) if not already set
        if (logic.partnerStats == null)
        {
            var partner = partnerOverride
                       ?? GetComponentInParent<Entity_Stats>(true)
                       ?? GetComponent<Entity_Stats>();
            if (partner) logic.partnerStats = partner;
        }

        // Bind player if not already set
        if (logic.playerStats == null)
        {
            var playerStats = FindFirstObjectByType<Player_Stats>(FindObjectsInactive.Include);
            if (playerStats) logic.playerStats = playerStats;
        }

        if (!sexyTimeLogicObject.activeSelf) sexyTimeLogicObject.SetActive(true);
        logic.StartSexyTime();
        return true;
    }
}
