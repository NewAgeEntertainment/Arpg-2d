// ScenePortalReentryGuard.cs
using System.Collections;
using UnityEngine;
using PixelCrushers.DialogueSystem;

[DisallowMultipleComponent]
public class ScenePortalReentryGuard : MonoBehaviour
{
    [Header("Startup Lockout")]
    [Tooltip("Disable portal use for this many seconds after the scene enables it.")]
    public float disableOnSceneLoadSeconds = 0.35f;

    [Header("Require Exit Before Reuse")]
    [Tooltip("After it becomes usable, the actor must leave the trigger once before it can be used again.")]
    public bool requireExitBeforeReuse = true;

    [Header("Debug")]
    public bool logState = false;

    private Usable usable;
    private Collider col3D;
    private Collider2D col2D;

    private bool blockedByExitGate = false;
    private float enableAtTime = 0f;

    void Awake()
    {
        usable = GetComponent<Usable>();
        col3D = GetComponent<Collider>();
        col2D = GetComponent<Collider2D>();
        if (usable == null)
        {
            Debug.LogWarning($"[ScenePortalReentryGuard] No Usable on {name}. Guard will only gate via triggers.");
        }
    }

    void OnEnable()
    {
        // Lock it out briefly after this scene loads/enables the portal.
        enableAtTime = Time.unscaledTime + Mathf.Max(0f, disableOnSceneLoadSeconds);
        blockedByExitGate = requireExitBeforeReuse;  // start blocked until we observe an Exit
        ApplyBlockState();
        StartCoroutine(StartupUnlockCo());
    }

    IEnumerator StartupUnlockCo()
    {
        // In case OnEnable timing varies, wait until time passes, then update.
        while (Time.unscaledTime < enableAtTime) yield return null;
        ApplyBlockState();
        if (logState) Debug.Log($"[ScenePortalReentryGuard] Startup lockout finished on {name}");
    }

    void Update()
    {
        // Keep the Usable disabled if we’re still blocked.
        ApplyBlockState();
    }

    void OnTriggerExit(Collider other)
    {
        if (!requireExitBeforeReuse) return;
        if (!IsPlayer(other.gameObject)) return;

        blockedByExitGate = false;
        ApplyBlockState();
        if (logState) Debug.Log($"[ScenePortalReentryGuard] Exit observed (3D) on {name}, portal armed.");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!requireExitBeforeReuse) return;
        if (!IsPlayer(other.gameObject)) return;

        blockedByExitGate = false;
        ApplyBlockState();
        if (logState) Debug.Log($"[ScenePortalReentryGuard] Exit observed (2D) on {name}, portal armed.");
    }

    private void ApplyBlockState()
    {
        bool timeBlocked = (Time.unscaledTime < enableAtTime);
        bool globalBlock = PortalCooldownFlag.IsBlockedNow;
        bool blocked = timeBlocked || blockedByExitGate;
        

        if (usable) usable.enabled = !blocked;

        // (Optional) you could also disable the collider itself while blocked,
        // but keeping collider ON lets us detect the required Exit cleanly.
        // If you must stop all overlap, uncomment:
        // if (col3D) col3D.enabled = !blocked;
        // if (col2D) col2D.enabled = !blocked;
    }

    // Heuristic: treat objects tagged "Player" as the actor; edit if you use a different tag.
    private bool IsPlayer(GameObject go) => go.CompareTag("Player");
}
