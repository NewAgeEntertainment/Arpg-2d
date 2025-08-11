using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers;

public class UI_SaveLoadPanel : MonoBehaviour
{
    [Header("Slots")]
    public SaveSlotUI[] saveSlots;      // Each slot has a Button; wire the button to call OnClickSlot(index)
    public GameObject panel;

    [Header("Overwrite Panel")]
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private TextMeshProUGUI overwriteQuestionText; // set default to: "Overwrite this file?"
    [SerializeField] private Button overwriteYesButton;
    [SerializeField] private Button overwriteNoButton;

    [Header("Saving Visual")]
    [SerializeField] private GameObject savingVisualRoot;   // panel with spinner/text
    [SerializeField] private TextMeshProUGUI savingText;    // shows "Saving.", "Saving..", "Saving..."
    [SerializeField] private float minimumShowTime = 0.35f; // keep visual up briefly

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip openDialogSfx;
    [SerializeField] private AudioClip confirmSfx;
    [SerializeField] private AudioClip cancelSfx;
    [SerializeField] private AudioClip saveStartSfx;
    [SerializeField] private AudioClip saveDoneSfx;

    private int _pendingSlotIndex = -1;
    private Coroutine _dotsRoutine;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (overwritePanel != null) overwritePanel.SetActive(false);
        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);

        if (sfx != null) sfx.ignoreListenerPause = true; // play UI SFX even if game is paused

        // Wire overwrite buttons
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
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    // Optional: expose whether the overwrite dialog is up
    public bool IsOverwriteOpen => overwritePanel != null && overwritePanel.activeSelf;

    // Let UI.cs route Cancel here:
    public bool HandleCancel()
    {
        // If overwrite dialog is up, close it first
        if (overwritePanel != null && overwritePanel.activeSelf)
        {
            overwritePanel.SetActive(false);
            return true;
        }

        // If main save panel is open, close it
        if (panel != null && panel.activeSelf)
        {
            ClosePanel();
            return true;
        }

        return false;
    }

    public void OpenPanel()
    {
        panel.SetActive(true);
        overwritePanel?.SetActive(false);
        savingVisualRoot?.SetActive(false);
        RefreshAllSlots();
    }

    public void ClosePanel()
    {
        overwritePanel?.SetActive(false);
        savingVisualRoot?.SetActive(false);
        panel.SetActive(false);
    }

    public void RefreshAllSlots()
    {
        if (saveSlots == null) return;
        foreach (var slot in saveSlots)
        {
            if (slot != null) slot.UpdateSlotUI();
        }
    }

    // Hook this from each Save Slot button, passing its index (0-based)
    public void OnClickSlot(int slotIndex)
    {
        _pendingSlotIndex = slotIndex;

        if (SlotHasData(slotIndex))
        {
            Play(openDialogSfx);
            ShowOverwritePanel();
        }
        else
        {
            StartCoroutine(SaveToSlotFlow(slotIndex));
        }
    }

    private void ShowOverwritePanel()
    {
        if (overwriteQuestionText != null)
            overwriteQuestionText.text = "Overwrite this file?";
        if (overwritePanel != null)
            overwritePanel.SetActive(true);
    }

    private void OnClickOverwriteYes()
    {
        Play(confirmSfx);
        if (overwritePanel != null) overwritePanel.SetActive(false);
        if (_pendingSlotIndex >= 0)
            StartCoroutine(SaveToSlotFlow(_pendingSlotIndex));
    }

    private void OnClickOverwriteNo()
    {
        Play(cancelSfx);
        if (overwritePanel != null) overwritePanel.SetActive(false);
        _pendingSlotIndex = -1;
        // back to slots
    }

    private IEnumerator SaveToSlotFlow(int slotIndex)
    {
        // Show visual + start dots
        if (savingVisualRoot != null) savingVisualRoot.SetActive(true);
        if (savingText != null) savingText.text = "Saving.";
        Play(saveStartSfx);

        if (savingText != null)
        {
            if (_dotsRoutine != null) StopCoroutine(_dotsRoutine);
            _dotsRoutine = StartCoroutine(AnimateSavingDots());
        }

        float startTime = Time.unscaledTime;

        // Do the actual save
        try
        {
            SaveSystem.SaveToSlot(slotIndex);

            // Mark metadata so we know this slot is occupied next time
            MarkSlotHasData(slotIndex, true);
            SetSlotTimestamp(slotIndex, DateTime.Now);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UI_SaveLoadPanel] Save failed for slot {slotIndex}: {ex}");
        }

        // Keep the visual up for a minimum time (feels responsive)
        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < minimumShowTime)
            yield return new WaitForSecondsRealtime(minimumShowTime - elapsed);

        // Stop dots
        if (_dotsRoutine != null)
        {
            StopCoroutine(_dotsRoutine);
            _dotsRoutine = null;
        }

        Play(saveDoneSfx);

        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);

        RefreshAllSlots();
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

    // -------- Slot state helpers --------

    private bool SlotHasData(int slotIndex)
    {
        // If your PixelCrushers version has a direct API, use it:
        // try { return SaveSystem.HasSavedGameInSlot(slotIndex); } catch { }

        // Fallback: PlayerPrefs flag set after saving
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

    public static DateTime? GetSlotTimestampStatic(int slotIndex)
    {
        var s = PlayerPrefs.GetString($"SaveSlot_{slotIndex}_time", string.Empty);
        if (string.IsNullOrEmpty(s)) return null;
        if (DateTime.TryParse(s, out var t)) return t;
        return null;
    }

    private string SlotKey(int slotIndex, string suffix) => $"SaveSlot_{slotIndex}_{suffix}";

    // -------- Audio helper --------

    private void Play(AudioClip clip)
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip);
    }
}
