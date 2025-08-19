using UnityEngine;
using TMPro;

public class UI_PlaytimeAndLocationBinder : MonoBehaviour
{
    [Header("Assign TMP text fields")]
    [SerializeField] private TMP_Text playtimeText;      // shows 00:00:00 or 00:00
    [SerializeField] private TMP_Text locationText;      // shows pretty scene name

    [Header("Formatting")]
    [SerializeField] private bool showSeconds = true;    // toggle to 00:00:00 vs 00:00

    private void OnEnable()
    {
        // Subscribe
        PlayTimeTracker.OnSecondChanged -= HandleSecondTick;
        PlayTimeTracker.OnSecondChanged += HandleSecondTick;

        PlayTimeTracker.OnSceneChanged -= HandleSceneChanged;
        PlayTimeTracker.OnSceneChanged += HandleSceneChanged;

        // Push current values right away:
        HandleSecondTick(PlayTimeTracker.TotalSecondsInt);
        HandleSceneChanged(PlayTimeTracker.CurrentSceneDisplay);
    }

    private void OnDisable()
    {
        PlayTimeTracker.OnSecondChanged -= HandleSecondTick;
        PlayTimeTracker.OnSceneChanged -= HandleSceneChanged;
    }

    private void HandleSecondTick(int totalSeconds)
    {
        if (!playtimeText) return;
        playtimeText.text = FormatTime(totalSeconds, showSeconds);
    }

    private void HandleSceneChanged(string display)
    {
        if (!locationText) return;
        locationText.text = display ?? string.Empty;
    }

    private static string FormatTime(int seconds, bool includeSeconds)
    {
        var ts = System.TimeSpan.FromSeconds(seconds);
        int hours = (int)ts.TotalHours; // collapse days into hours
        if (includeSeconds)
            return $"{hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
        else
            return $"{hours:00}:{ts.Minutes:00}";
    }
}
