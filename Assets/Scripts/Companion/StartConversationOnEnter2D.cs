using UnityEngine;
using PixelCrushers.DialogueSystem;

[RequireComponent(typeof(Collider2D))]
public class StartConversationOnEnter2D : MonoBehaviour
{
    public string conversation;            // e.g., "New Conversation 112"
    public Transform actor;                // player (optional; if null, uses entering object)
    public Transform conversant;           // NPC (optional; if null, uses this trigger)
    public string requiredTag = "Player";  // set to your player tag
    public bool once = true;

    bool used;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used && once) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
        if (string.IsNullOrEmpty(conversation)) { Debug.LogWarning("No conversation set"); return; }

        var a = actor != null ? actor : other.transform;
        var c = conversant != null ? conversant : transform;
        DialogueManager.StartConversation(conversation, a, c);
        used = true;
    }
}
