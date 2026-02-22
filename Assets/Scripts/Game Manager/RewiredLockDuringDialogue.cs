using UnityEngine;
using PixelCrushers.DialogueSystem;
using Rewired;
using RwPlayer = Rewired.Player;

public class RewiredLockDuringDialogue : MonoBehaviour
{
    [Header("Rewired")]
    [SerializeField] private int rewiredPlayerId = 0;

    [Header("Map Categories")]
    [SerializeField] private string gameplayCategory = "Gameplay";
    [SerializeField] private string uiCategory = "UI";

    [Header("Behavior")]
    [SerializeField] private bool keepUIEnabled = true;

    [Header("Optional hard-freeze (your Player script)")]
    [SerializeField] private Player player;

    [Header("Optional physics stop")]
    [SerializeField] private Rigidbody2D rb2D;

    private RwPlayer rwPlayer;

    void Awake()
    {
        if (!player) player = GetComponent<Player>();
        if (!rb2D) rb2D = GetComponent<Rigidbody2D>();
        CacheRewired();
    }

    void OnEnable()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.conversationStarted += OnConversationStarted;
        DialogueManager.Instance.conversationEnded += OnConversationEnded;
    }

    void OnDisable()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.conversationStarted -= OnConversationStarted;
        DialogueManager.Instance.conversationEnded -= OnConversationEnded;
    }

    private void OnConversationStarted(Transform actor) => SetLocked(true);
    private void OnConversationEnded(Transform actor) => SetLocked(false);

    private void CacheRewired()
    {
        try { rwPlayer = ReInput.players.GetPlayer(rewiredPlayerId); }
        catch { rwPlayer = null; }
    }

    private void SetLocked(bool locked)
    {
        if (rwPlayer == null) CacheRewired();
        if (rwPlayer == null) return;

        // During dialogue: disable ONLY gameplay maps.
        // Do NOT touch SexyTime maps here (SexyTimeLogic owns them).
        if (locked)
        {
            rwPlayer.controllers.maps.SetMapsEnabled(false, gameplayCategory);
        }
        else
        {
            // Only restore gameplay if SexyTime isn't active.
            if (SexyTimeLogic.isSexyTimeGoingOn == false)
                rwPlayer.controllers.maps.SetMapsEnabled(true, gameplayCategory);
        }

        // Keep UI enabled (or disable if you want)
        rwPlayer.controllers.maps.SetMapsEnabled(keepUIEnabled, uiCategory);

        if (locked && rb2D)
        {
            rb2D.velocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }

        // Freeze/unfreeze player logic:
        if (player)
        {
            if (locked) player.SetInputEnabled(false);
            else
            {
                // Only unfreeze if SexyTime isn't active.
                if (SexyTimeLogic.isSexyTimeGoingOn == false)
                    player.SetInputEnabled(true);
            }
        }
    }
}