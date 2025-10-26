using UnityEngine;
using UnityEngine.Events;
using PixelCrushers.DialogueSystem; // optional, harmless if not installed

public class RestPoint : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("If empty, will try to rest the first Player found in scene.")]
    public Transform explicitActor;

    [Header("Options")]
    [Tooltip("Hide/lock UI during the brief rest (nice with fades).")]
    public bool briefFreeze = true;
    [Tooltip("How long to freeze, if enabled.")]
    public float freezeSeconds = 0.25f;

    [Tooltip("Play a small particle/sound when resting.")]
    public UnityEvent onRested;

    // ---------------- Pixel Crushers Usable hooks ----------------
    // These method names match what your InteractionTooltipTrigger2D sends
    public void OnUse(Transform actor)
    {
        RestNow(actor);
    }

    public void OnSelect(Transform actor)
    {
        // optional: show “Rest” specific prompt, highlight, etc.
    }

    public void OnDeselect()
    {
        // optional: remove highlight, etc.
    }

    // ---------------- Public API for Dialogue / scripts ----------------
    /// <summary>
    /// Call this from Dialogue System (e.g., Sequencer: SendMessage(RestNow))
    /// or from your own code to perform a full rest.
    /// </summary>
    public void RestNow()
    {
        RestNow(null);
    }

    /// <summary>
    /// Variant with actor. Dialogue System Usable passes the actor Transform here.
    /// </summary>
    public void RestNow(Transform actor)
    {
        var who = ResolveActor(actor);
        if (!who)
        {
            Debug.LogWarning($"[RestPoint] No actor found to restore.");
            return;
        }

        if (briefFreeze) StartCoroutine(FreezeCo());

        bool ok = RestService.RestoreActorFull(who.gameObject);
        if (ok)
        {
            onRested?.Invoke();
            // Optional: hook your UI to flash bars, play SFX, etc.
            // e.g., UI_FloatingText.Spawn("Fully Restored!", who.position);
        }
    }

    // ---------------- Internals ----------------
    private Transform ResolveActor(Transform passed)
    {
        if (passed) return passed;
        if (explicitActor) return explicitActor;

        // Try to locate the Player in your project
        var player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        return player ? player.transform : null;
    }

    private System.Collections.IEnumerator FreezeCo()
    {
        // If you have your own GameManager/UI to lock inputs, hook here:
        // GameManager.Instance?.SetInputEnabled(false);
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, freezeSeconds));
        Time.timeScale = 1f;
        // GameManager.Instance?.SetInputEnabled(true);
    }
}
