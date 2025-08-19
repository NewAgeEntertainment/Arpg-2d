// UI_SaveLoadPanel.cs
using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers;

public class UI_SaveLoadPanel : MonoBehaviour
{
    public enum Mode { Save, Load }

    [Header("Basic UI")]
    [SerializeField] private TMP_Text headerText;

    [Tooltip("Root object that contains the visible panel (can be this object).")]
    [SerializeField] private GameObject contentRoot;

    [Tooltip("Optional CanvasGroup on the visible panel.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Slots")]
    [Tooltip("Hook up your SaveSlotUI components in order (0..N-1).")]
    [SerializeField] private SaveSlotUI[] saveSlots;

    [Header("Overwrite Panel (Save mode only)")]
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private TMP_Text overwriteQuestionText;       // e.g., "Overwrite this file?"
    [SerializeField] private Button overwriteYesButton;
    [SerializeField] private Button overwriteNoButton;

    [Header("Saving Visual (Save mode only)")]
    [SerializeField] private GameObject savingVisualRoot;
    [SerializeField] private TMP_Text savingText;                  // "Saving.", "Saving..", "Saving..."
    [SerializeField] private float minimumShowTime = 0.35f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip openDialogSfx;
    [SerializeField] private AudioClip confirmSfx;
    [SerializeField] private AudioClip cancelSfx;
    [SerializeField] private AudioClip saveStartSfx;
    [SerializeField] private AudioClip saveDoneSfx;
    [SerializeField] private AudioClip loadClickSfx;
    [SerializeField] private AudioClip loadErrorSfx;

    // ---- runtime ----
    private Mode _mode = Mode.Save;
    private int _pendingSlotIndex = -1;
    private Coroutine _dotsRoutine;

    public bool IsOpen =>
        (contentRoot ? contentRoot.activeSelf : gameObject.activeSelf);

    public bool IsOverwriteOpen =>
        overwritePanel != null && overwritePanel.activeSelf;

    private const string TimeFormat = "yyyy-MM-dd HH:mm"; // one place for timestamp format

    private void Awake()
    {
        // default assignments
        if (!contentRoot) contentRoot = gameObject;

        // start hidden until opened
        SetContentVisible(false);
        if (overwritePanel) overwritePanel.SetActive(false);
        if (savingVisualRoot) savingVisualRoot.SetActive(false);

        // ensure audio won’t be paused with game
        if (sfx) sfx.ignoreListenerPause = true;

        // wire overwrite buttons
        if (overwriteYesButton)
        {
            overwriteYesButton.onClick.RemoveAllListeners();
            overwriteYesButton.onClick.AddListener(OnClickOverwriteYes);
        }
        if (overwriteNoButton)
        {
            overwriteNoButton.onClick.RemoveAllListeners();
            overwriteNoButton.onClick.AddListener(OnClickOverwriteNo);
        }
    }

    // ------------------ Public API ------------------

    public void OpenForSave()
    {
        _mode = Mode.Save;
        EnsureActivatedAndShow();
        if (headerText) headerText.text = "Save Game";
    }

    public void OpenForLoad()
    {
        _mode = Mode.Load;
        EnsureActivatedAndShow();
        if (headerText) headerText.text = "Load Game";
    }

    public void ClosePanel()
    {
        if (overwritePanel) overwritePanel.SetActive(false);
        if (savingVisualRoot) savingVisualRoot.SetActive(false);
        SetContentVisible(false);
    }

    /// Route Esc/Cancel here from your input manager.
    public bool HandleCancel()
    {
        if (overwritePanel && overwritePanel.activeSelf)
        {
            Play(cancelSfx);
            overwritePanel.SetActive(false);
            return true;
        }

        if (IsOpen)
        {
            Play(cancelSfx);
            ClosePanel();
            return true;
        }

        return false;
    }

    public void RefreshAllSlots()
    {
        if (saveSlots == null) return;
        for (int i = 0; i < saveSlots.Length; i++)
        {
            var slot = saveSlots[i];
            if (!slot) continue;
            slot.slotID = i;           // keep index in sync
            slot.UpdateSlotUI();       // your slot script should read PlayerPrefs/SaveSystem
        }
    }

    /// Hook each slot button (Save/Load) to call this with its index.
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
        else // Load
        {
            if (!SlotHasData(slotIndex))
            {
                Play(loadErrorSfx);
                return;
            }

            Play(loadClickSfx);
            SaveSystem.LoadFromSlot(slotIndex);
            ClosePanel(); // optional: collapse immediately
        }
    }

    // ------------------ Internals ------------------

    private void EnsureActivatedAndShow()
    {
        // make sure this GO is active before starting coroutines
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // visuals for the selected mode
        if (overwritePanel) overwritePanel.SetActive(false);
        if (savingVisualRoot) savingVisualRoot.SetActive(false);

        SetContentVisible(true);
        Play(openDialogSfx);

        // delay one frame so child layouts init, then refresh UI
        StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshAllSlots();
    }

    private void SetContentVisible(bool visible)
    {
        if (contentRoot) contentRoot.SetActive(visible);
        if (canvasGroup)
        {
            canvasGroup.alpha = visible ? 1f : 1f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    private void ShowOverwritePanel()
    {
        if (_mode != Mode.Save || !overwritePanel) return;

        var when = GetSlotTimestampStatic(_pendingSlotIndex);
        if (overwriteQuestionText)
        {
            overwriteQuestionText.text = when.HasValue
                ? $"Overwrite this file?\nLast saved: {when.Value.ToString(TimeFormat, CultureInfo.InvariantCulture)}"
                : "Overwrite this file?";
        }

        overwritePanel.SetActive(true);
    }

    private void OnClickOverwriteYes()
    {
        if (_mode != Mode.Save) return;

        Play(confirmSfx);
        if (overwritePanel) overwritePanel.SetActive(false);
        if (_pendingSlotIndex >= 0) StartCoroutine(SaveToSlotFlow(_pendingSlotIndex));
    }

    private void OnClickOverwriteNo()
    {
        Play(cancelSfx);
        if (overwritePanel) overwritePanel.SetActive(false);
        _pendingSlotIndex = -1;
    }

    private IEnumerator SaveToSlotFlow(int slotIndex)
    {
        if (_mode != Mode.Save) yield break;

        SetSlotsInteractable(false);

        if (savingVisualRoot) savingVisualRoot.SetActive(true);
        if (savingText) savingText.text = "Saving.";
        Play(saveStartSfx);

        if (savingText)
        {
            if (_dotsRoutine != null) StopCoroutine(_dotsRoutine);
            _dotsRoutine = StartCoroutine(AnimateSavingDots());
        }

        float start = Time.unscaledTime;

        try
        {
            SaveSystem.SaveToSlot(slotIndex);

            // ---- metadata so slots can display Location + Play Time, etc. ----
            PlayerPrefs.SetString($"SaveSlot_{slotIndex}_scene", SaveSystem.GetCurrentSceneName());
            PlayerPrefs.SetInt($"SaveSlot_{slotIndex}_playSeconds", PlayTimeTracker.TotalSecondsInt);
            PlayerPrefs.SetString($"SaveSlot_{slotIndex}_time",
                DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt($"SaveSlot_{slotIndex}_exists", 1);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UI_SaveLoadPanel] Save failed for slot {slotIndex}: {ex}");
        }

        float elapsed = Time.unscaledTime - start;
        if (elapsed < minimumShowTime)
            yield return new WaitForSecondsRealtime(minimumShowTime - elapsed);

        if (_dotsRoutine != null)
        {
            StopCoroutine(_dotsRoutine);
            _dotsRoutine = null;
        }

        Play(saveDoneSfx);
        if (savingVisualRoot) savingVisualRoot.SetActive(false);

        SetSlotsInteractable(true);
        RefreshAllSlots();
    }

    private IEnumerator AnimateSavingDots()
    {
        string baseText = "Saving";
        int dot = 1;
        while (true)
        {
            if (savingText) savingText.text = baseText + new string('.', dot);
            dot++;
            if (dot > 3) dot = 1;
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    // -------- helpers --------

    private void SetSlotsInteractable(bool interactable)
    {
        if (saveSlots == null) return;
        foreach (var slot in saveSlots)
        {
            if (!slot) continue;

            // If your SaveSlotUI exposes these, great; otherwise remove these lines.
            if (_mode == Mode.Save)
            {
                if (slot.saveButton) slot.saveButton.interactable = interactable;
            }
            else
            {
                if (slot.loadButton) slot.loadButton.interactable = interactable;
            }
        }
    }

    private bool SlotHasData(int slotIndex)
    {
        try { return SaveSystem.HasSavedGameInSlot(slotIndex); }
        catch { /* fall through to prefs flag */ }

        return PlayerPrefs.GetInt(SlotKey(slotIndex, "exists"), 0) == 1;
    }

    public static DateTime? GetSlotTimestampStatic(int slotIndex)
    {
        var s = PlayerPrefs.GetString($"SaveSlot_{slotIndex}_time", string.Empty);
        if (string.IsNullOrEmpty(s)) return null;

        if (DateTime.TryParseExact(s, TimeFormat, CultureInfo.InvariantCulture,
                                   DateTimeStyles.None, out var t))
        {
            return t;
        }
        return null;
    }

    private string SlotKey(int slot, string suffix) => $"SaveSlot_{slot}_{suffix}";

    private void Play(AudioClip clip)
    {
        if (sfx && clip) sfx.PlayOneShot(clip);
    }
}
