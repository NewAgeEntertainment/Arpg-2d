using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers;

public class UI_SaveMenu : MonoBehaviour
{
    [Header("Slots")]
    [Tooltip("Buttons for each save slot, in order (Slot 1, Slot 2, ...).")]
    [SerializeField] private Button[] slotButtons;

    [Tooltip("Optional labels for each slot (shows Empty or timestamp).")]
    [SerializeField] private TextMeshProUGUI[] slotLabels;

    [Header("Overwrite Panel")]
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private TextMeshProUGUI overwriteQuestionText; // e.g., “Overwrite this file?”
    [SerializeField] private Button overwriteYesButton;
    [SerializeField] private Button overwriteNoButton;

    [Header("Saving Visual")]
    [Tooltip("Root panel containing the Saving... UI.")]
    [SerializeField] private GameObject savingVisualRoot;

    [Tooltip("Text that shows 'Saving.' / 'Saving..' / 'Saving...' while saving.")]
    [SerializeField] private TextMeshProUGUI savingText;

    [Tooltip("Minimum time (seconds) to show the saving visual so it feels responsive).")]
    [SerializeField] private float minimumShowTime = 0.35f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip openDialogSfx;
    [SerializeField] private AudioClip confirmSfx;
    [SerializeField] private AudioClip cancelSfx;
    [SerializeField] private AudioClip saveStartSfx;
    [SerializeField] private AudioClip saveDoneSfx;

    private int pendingSlotIndex = -1;
    private Coroutine dotsRoutine;

    private void Awake()
    {
        // Wire slot buttons
        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int captured = i;
                if (slotButtons[i] != null)
                    slotButtons[i].onClick.AddListener(() => OnClickSlot(captured));
            }
        }

        if (overwriteYesButton != null)
        {
            overwriteYesButton.onClick.RemoveAllListeners();
            overwriteYesButton.onClick.AddListener(OnClickOverwriteYes);
        }

        if (overwriteNoButton != null)
        {
            overwriteNoButton.onClick.RemoveAllListeners();
            overwriteNoButton.onClick.AddListener(OnClickOverwriteNo);
        }

        if (overwritePanel != null) overwritePanel.SetActive(false);
        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);

        // UI audio that still plays if gameplay pauses audio
        if (sfx != null) sfx.ignoreListenerPause = true;

        RefreshSlotLabels();
    }

    // Call this when opening the Save menu.
    public void Open()
    {
        gameObject.SetActive(true);
        if (overwritePanel != null) overwritePanel.SetActive(false);
        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);
        RefreshSlotLabels();
    }

    // Call this to close the Save menu.
    public void Close()
    {
        if (overwritePanel != null) overwritePanel.SetActive(false);
        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);
        gameObject.SetActive(false);
    }

    // =========================
    // Slot click flow
    // =========================
    private void OnClickSlot(int slotIndex)
    {
        pendingSlotIndex = slotIndex;

        if (SlotHasData(slotIndex))
        {
            // Ask to overwrite
            Play(openDialogSfx);
            ShowOverwritePanel(slotIndex);
        }
        else
        {
            // Save immediately
            StartCoroutine(SaveToSlotFlow(slotIndex));
        }
    }

    private void ShowOverwritePanel(int slotIndex)
    {
        if (overwriteQuestionText != null)
            overwriteQuestionText.text = "Overwrite this file?";
        if (overwritePanel != null) overwritePanel.SetActive(true);
    }

    private void OnClickOverwriteYes()
    {
        Play(confirmSfx);
        if (overwritePanel != null) overwritePanel.SetActive(false);
        if (pendingSlotIndex >= 0)
            StartCoroutine(SaveToSlotFlow(pendingSlotIndex));
    }

    private void OnClickOverwriteNo()
    {
        Play(cancelSfx);
        if (overwritePanel != null) overwritePanel.SetActive(false);
        pendingSlotIndex = -1;
        // Just return to slot list
    }

    // =========================
    // Saving flow + animated dots
    // =========================
    private IEnumerator SaveToSlotFlow(int slotIndex)
    {
        // Show "Saving..." UI
        if (savingVisualRoot != null) savingVisualRoot.SetActive(true);
        if (savingText != null) savingText.text = "Saving.";
        Play(saveStartSfx);

        // Start animated dots
        if (savingText != null)
        {
            // make sure only one routine is running
            if (dotsRoutine != null) StopCoroutine(dotsRoutine);
            dotsRoutine = StartCoroutine(AnimateSavingDots());
        }

        float startTime = Time.unscaledTime;

        // Perform the save
        try
        {
            SaveSystem.SaveToSlot(slotIndex);
            // Fallback metadata so we know the slot isn't empty next time:
            MarkSlotHasData(slotIndex, true);
            SetSlotTimestamp(slotIndex, DateTime.Now);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UI_SaveMenu] Save failed for slot {slotIndex}: {ex}");
        }

        // Ensure the “Saving...” visual sticks around briefly
        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < minimumShowTime)
            yield return new WaitForSecondsRealtime(minimumShowTime - elapsed);

        // Stop dots animation
        if (dotsRoutine != null)
        {
            StopCoroutine(dotsRoutine);
            dotsRoutine = null;
        }

        Play(saveDoneSfx);

        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);

        RefreshSlotLabels();
    }

    private IEnumerator AnimateSavingDots()
    {
        string baseText = "Saving";
        int dotCount = 1;

        while (true)
        {
            if (savingText != null)
                savingText.text = baseText + new string('.', dotCount);
            dotCount++;
            if (dotCount > 3) dotCount = 1;
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    // =========================
    // Slot state helpers
    // =========================
    private bool SlotHasData(int slotIndex)
    {
        // Preferred: if your PixelCrushers version has this API, use it:
        try
        {
            // Some versions support this directly:
            // return SaveSystem.HasSavedGameInSlot(slotIndex);
            // If not, catching will drop to fallback.
        }
        catch { /* ignore and use fallback */ }

        // Fallback: PlayerPrefs flag we set after saving
        return PlayerPrefs.GetInt(SlotKey(slotIndex, "exists"), 0) == 1;
    }

    private void MarkSlotHasData(int slotIndex, bool hasData)
    {
        PlayerPrefs.SetInt(SlotKey(slotIndex, "exists"), hasData ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetSlotTimestamp(int slotIndex, DateTime time)
    {
        PlayerPrefs.SetString(SlotKey(slotIndex, "time"), time.ToString("yyyy-MM-dd HH:mm"));
        PlayerPrefs.Save();
    }

    private DateTime? GetSlotTimestamp(int slotIndex)
    {
        var s = PlayerPrefs.GetString(SlotKey(slotIndex, "time"), string.Empty);
        if (string.IsNullOrEmpty(s)) return null;
        if (DateTime.TryParse(s, out var t)) return t;
        return null;
    }

    private string SlotKey(int slotIndex, string suffix) => $"SaveSlot_{slotIndex}_{suffix}";

    // =========================
    // UI refresh
    // =========================
    private void RefreshSlotLabels()
    {
        if (slotLabels == null) return;

        for (int i = 0; i < slotLabels.Length; i++)
        {
            var label = slotLabels[i];
            if (label == null) continue;

            if (SlotHasData(i))
            {
                var t = GetSlotTimestamp(i);
                label.text = t.HasValue
                    ? $"Slot {i + 1}\n{t.Value:yyyy-MM-dd HH:mm}"
                    : $"Slot {i + 1}\n(Has Data)";
            }
            else
            {
                label.text = $"Slot {i + 1}\n<color=#888888>Empty</color>";
            }
        }
    }

    // =========================
    // Audio helper
    // =========================
    private void Play(AudioClip clip)
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip);
    }
}
