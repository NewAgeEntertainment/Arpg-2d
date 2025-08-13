using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers;

public class UI_SaveLoadPanel : MonoBehaviour
{
    public enum Mode { Save, Load }

    [Header("Mode UI")]
    [SerializeField] private TextMeshProUGUI headerText;   // optional, shows "Save Game" / "Load Game"

    [Header("Slots")]
    public SaveSlotUI[] saveSlots;      // Each slot has a Button; wire to OnClickSlot(index)
    public GameObject panel;

    [Header("Overwrite Panel (Save mode only)")]
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private TextMeshProUGUI overwriteQuestionText; // default: "Overwrite this file?"
    [SerializeField] private Button overwriteYesButton;
    [SerializeField] private Button overwriteNoButton;

    [Header("Saving Visual (Save mode only)")]
    [SerializeField] private GameObject savingVisualRoot;   // panel with spinner/text
    [SerializeField] private TextMeshProUGUI savingText;    // "Saving.", "Saving..", "Saving..."
    [SerializeField] private float minimumShowTime = 0.35f; // keep visual up briefly

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip openDialogSfx;
    [SerializeField] private AudioClip confirmSfx;
    [SerializeField] private AudioClip cancelSfx;
    [SerializeField] private AudioClip saveStartSfx;
    [SerializeField] private AudioClip saveDoneSfx;
    [SerializeField] private AudioClip loadClickSfx;
    [SerializeField] private AudioClip loadErrorSfx;

    private int _pendingSlotIndex = -1;
    private Coroutine _dotsRoutine;
    private Mode _mode = Mode.Save;

    public bool IsOpen => panel != null && panel.activeSelf;
    public bool IsOverwriteOpen => overwritePanel != null && overwritePanel.activeSelf;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (overwritePanel != null) overwritePanel.SetActive(false);
        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);
        if (sfx != null) sfx.ignoreListenerPause = true;

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

    // ------------------ Public API ------------------

    public void OpenForSave()
    {
        _mode = Mode.Save;
        OpenPanelInternal();
        if (headerText != null) headerText.text = "Save Game";
    }

    public void OpenForLoad()
    {
        _mode = Mode.Load;
        OpenPanelInternal();
        if (headerText != null) headerText.text = "Load Game";
    }

    public void ClosePanel()
    {
        overwritePanel?.SetActive(false);
        savingVisualRoot?.SetActive(false);
        panel?.SetActive(false);
    }

    /// Let UI.cs or TitleMenuManager route Esc here
    public bool HandleCancel()
    {
        if (overwritePanel != null && overwritePanel.activeSelf)
        {
            overwritePanel.SetActive(false);
            return true;
        }
        if (panel != null && panel.activeSelf)
        {
            ClosePanel();
            return true;
        }
        return false;
    }

    public void RefreshAllSlots()
    {
        if (saveSlots == null) return;
        foreach (var slot in saveSlots)
        {
            if (slot != null) slot.UpdateSlotUI(); // keep your existing slot UI logic
        }
    }

    // Hook this from each Save Slot button, passing its index (0-based)
    public void OnClickSlot(int slotIndex)
    {
        _pendingSlotIndex = slotIndex;

        if (_mode == Mode.Save)
        {
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
        else // Load mode
        {
            if (!SlotHasData(slotIndex))
            {
                Play(loadErrorSfx);
                return;
            }
            Play(loadClickSfx);
            SaveSystem.LoadFromSlot(slotIndex);
            // optional: hide UI immediately to avoid double input
            ClosePanel();
        }
    }

    // ------------------ Internals ------------------

    private void OpenPanelInternal()
    {
        if (panel == null) return;
        Play(openDialogSfx);

        // Only Save mode uses these visuals:
        if (_mode == Mode.Save)
        {
            overwritePanel?.SetActive(false);
            savingVisualRoot?.SetActive(false);
        }
        else
        {
            overwritePanel?.SetActive(false);
            savingVisualRoot?.SetActive(false);
        }

        panel.SetActive(true);
        RefreshAllSlots();
    }

    private void ShowOverwritePanel()
    {
        if (_mode != Mode.Save) return;

        if (overwriteQuestionText != null)
            overwriteQuestionText.text = "Overwrite this file?";
        if (overwritePanel != null)
            overwritePanel.SetActive(true);
    }

    private void OnClickOverwriteYes()
    {
        if (_mode != Mode.Save) return;

        Play(confirmSfx);
        overwritePanel?.SetActive(false);
        if (_pendingSlotIndex >= 0)
            StartCoroutine(SaveToSlotFlow(_pendingSlotIndex));
    }

    private void OnClickOverwriteNo()
    {
        Play(cancelSfx);
        overwritePanel?.SetActive(false);
        _pendingSlotIndex = -1;
    }

    private IEnumerator SaveToSlotFlow(int slotIndex)
    {
        if (_mode != Mode.Save) yield break;

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
        // Prefer PixelCrushers API:
        try { return SaveSystem.HasSavedGameInSlot(slotIndex); } catch { }

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
