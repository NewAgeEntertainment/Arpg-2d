using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using PixelCrushers;   // SaveSystem
// NOTE: Works with 2D or 3D; just make sure the collider has "Is Trigger" checked.

[AddComponentMenu("Gameplay/Auto Save Trigger")]
public class AutoSaveTrigger : MonoBehaviour
{
    public enum SaveTarget
    {
        DedicatedAutosaveSlot,     // e.g., -2
        LastManualSlotOrDefault,   // use last manual save, else fallback to a slot
        SpecificSlot               // a fixed slot index
    }

    [Header("Who can trigger it?")]
    [Tooltip("If set, only GameObjects with this tag will trigger the save (e.g. 'Player'). Leave empty to accept anything that has a Player component.")]
    [SerializeField] private string requiredTag = "Player";

    [Header("Save Destination")]
    [SerializeField] private SaveTarget target = SaveTarget.DedicatedAutosaveSlot;

    [Tooltip("Used when SaveTarget = DedicatedAutosaveSlot.")]
    [SerializeField] private int autosaveSlot = -2;

    [Tooltip("Used when SaveTarget = SpecificSlot.")]
    [SerializeField] private int specificSlot = 0;

    [Tooltip("Fallback if there is no last manual slot saved yet.")]
    [SerializeField] private int defaultManualSlot = 0;

    [Tooltip("Allow PixelCrushers negative slot numbers (needed for autosave/suspend-style slots).")]
    [SerializeField] private bool allowNegativeSlotNumbers = true;

    [Header("Throttle")]
    [Tooltip("If true, this trigger saves only once.")]
    [SerializeField] private bool oneShot = true;

    [Tooltip("Minimum unscaled seconds between saves if re-entered.")]
    [SerializeField, Min(0f)] private float cooldown = 1.0f;

    [Header("Feedback (optional)")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip savedSfx;
    [SerializeField] private UnityEvent onSaved;

    private const string TimeFormat = "yyyy-MM-dd HH:mm";
    private float _lastSaveTime = -999f;
    private bool _used;

    // ---- Trigger hooks (2D & 3D) ----
    private void OnTriggerEnter2D(Collider2D other) => TrySaveFrom(other.gameObject);
    private void OnTriggerEnter(Collider other) => TrySaveFrom(other.gameObject);

    private void TrySaveFrom(GameObject go)
    {
        if (oneShot && _used) return;
        if (Time.unscaledTime - _lastSaveTime < cooldown) return;

        // Filter: tag or Player component
        if (!string.IsNullOrEmpty(requiredTag))
        {
            if (!go.CompareTag(requiredTag))
            {
                if (go.GetComponent<Player>() == null) return; // accept Player component as a fallback
            }
        }

        DoSave();
    }

    private void DoSave()
    {
        // Enable negative slots if desired (e.g., autosave = -2)
        if (allowNegativeSlotNumbers && SaveSystem.hasInstance)
            SaveSystem.instance.allowNegativeSlotNumbers = true;

        int slotIndex = ResolveSlot();
        try
        {
            SaveSystem.SaveToSlotImmediate(slotIndex);   // silent, immediate autosave
            WriteSlotMetadata(slotIndex);                // keep your UI slot timestamps in sync
            _lastSaveTime = Time.unscaledTime;
            _used = true;

            if (sfx && savedSfx) sfx.PlayOneShot(savedSfx);
            onSaved?.Invoke();

            Debug.Log($"[AutoSaveTrigger] Saved to slot {slotIndex} at {DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoSaveTrigger] Save failed for slot {slotIndex}: {ex}");
        }
    }

    private int ResolveSlot()
    {
        switch (target)
        {
            case SaveTarget.SpecificSlot:
                return specificSlot;

            case SaveTarget.LastManualSlotOrDefault:
                {
                    // PixelCrushers stores last used slot in PlayerPrefs
                    int last = PlayerPrefs.GetInt(SaveSystem.LastSavedGameSlotPlayerPrefsKey, int.MinValue);
                    return (last == int.MinValue) ? defaultManualSlot : last;
                }

            default: // DedicatedAutosaveSlot
                return autosaveSlot;
        }
    }

    private void WriteSlotMetadata(int slotIndex)
    {
        // Mirrors what your UI_SaveLoadPanel writes so slots show scene/time/playtime correctly.
        string sceneName;
        try { sceneName = SaveSystem.GetCurrentSceneName(); }
        catch { sceneName = SceneManager.GetActiveScene().name; }

        PlayerPrefs.SetString($"SaveSlot_{slotIndex}_scene", sceneName);
        PlayerPrefs.SetInt($"SaveSlot_{slotIndex}_playSeconds", PlayTimeTracker.TotalSecondsInt);
        PlayerPrefs.SetString($"SaveSlot_{slotIndex}_time", DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture));
        PlayerPrefs.SetInt($"SaveSlot_{slotIndex}_exists", 1);
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
    }
#endif
}
