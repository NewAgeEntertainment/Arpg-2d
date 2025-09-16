// SendQuestMessageOnDeath.cs
using UnityEngine;
using PixelCrushers; // Quest Machine / Dialogue System MessageSystem

/// Attach to the same GameObject as Entity_Health (or a child).
/// On death, sends a PixelCrushers.MessageSystem message that
/// Message/Counter quest conditions can listen for.
[AddComponentMenu("Integration/Quest Machine/Send Message On Death")]
[DisallowMultipleComponent]
public class SendQuestMessageOnDeath : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Entity_Health health; // auto-found if left null

    [Header("Quest Machine Message")]
    [SerializeField] private string message = "killed";     // What your quest listens for
    [SerializeField] private string parameter = "Bandit";    // Who/what was killed
    [SerializeField] private bool useGameObjectNameIfEmpty = true;
    [Tooltip("Optional MessageSystem target. Leave empty to broadcast.")]
    [SerializeField] private Transform messageTarget;

    [Header("Safety")]
    [SerializeField] private bool sendOnlyOnce = true;
    private bool _sent;

    private void Reset()
    {
        health = GetComponentInParent<Entity_Health>();
    }

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<Entity_Health>();
    }

    private void OnEnable()
    {
        if (health != null) health.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (sendOnlyOnce && _sent) return;
        _sent = true;

        var param = string.IsNullOrEmpty(parameter) && useGameObjectNameIfEmpty
            ? gameObject.name
            : parameter;

        // Fires Quest Machine/Dialogue System message:
        //   sender = this component
        //   message = e.g., "killed"
        //   parameter = e.g., "Bandit"
        //   target = optional (usually null)
        MessageSystem.SendMessage(this, message, param, messageTarget);
        // Example: A Message Quest Condition with Message="killed" & Parameter="Bandit"
        // will pass when this fires (Counter conditions will increment each time).
    }
}
