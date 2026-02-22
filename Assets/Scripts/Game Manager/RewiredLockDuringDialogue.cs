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
    [SerializeField] private string sexyTimeCategory = "SexyTime";
    [SerializeField] private string uiCategory = "UI";

    [Header("Behavior")]
    [SerializeField] private bool keepUIEnabled = true;

    [Header("Optional hard-freeze (your Player script)")]
    [SerializeField] private Player player; // your Player class

    [Header("Optional physics stop")]
    [SerializeField] private Rigidbody2D rb2D;

    private RwPlayer rwPlayer;

    void Awake()
    {
        if (!player) player = GetComponent<Player>();
        if (!rb2D) rb2D = GetComponent<Rigidbody2D>();
        CacheRewired(); // <-- this method exists below
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

        // Disable gameplay + sexytime maps during dialogue:
        rwPlayer.controllers.maps.SetMapsEnabled(!locked, gameplayCategory);
        rwPlayer.controllers.maps.SetMapsEnabled(!locked, sexyTimeCategory);

        // Keep UI enabled so dialogue navigation works:
        rwPlayer.controllers.maps.SetMapsEnabled(!(locked && !keepUIEnabled), uiCategory);

        // Optional: stop physics drift immediately:
        if (locked && rb2D)
        {
            rb2D.velocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }

        // Freeze/unfreeze your gameplay logic (this already prevents stuck movement):
        if (player) player.SetInputEnabled(!locked);
    }
}