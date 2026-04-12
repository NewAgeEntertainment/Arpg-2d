// Assets/Scripts/Game Manager/UI_SaveLoadPanel.cs
using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers;
using UnityEngine.SceneManagement;

public class UI_SaveLoadPanel : MonoBehaviour
{
    public enum Mode { Save, Load }

    // NEW: remember who opened this panel (so we can bounce back correctly).
    public enum OpenContext { None, TitleMenu, PauseMenu, GameOver }
    public OpenContext Context { get; private set; } = OpenContext.None;
    public event Action<OpenContext> Closed;

    [Header("Mode UI")]
    [SerializeField] private TextMeshProUGUI headerText;

    [Header("Slots")]
    public SaveSlotUI[] saveSlots;    // Each slot button should call OnClickSlot(index)
    [SerializeField] private GameObject panel; // root container for this window (the visible panel)

    [Header("Overwrite Panel (Save mode only)")]
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private TextMeshProUGUI overwriteQuestionText;
    [SerializeField] private Button overwriteYesButton;
    [SerializeField] private Button overwriteNoButton;

    [Header("Saving Visual (Save mode only)")]
    [SerializeField] private GameObject savingVisualRoot;   // spinner/text block
    [SerializeField] private TextMeshProUGUI savingText;    // "Saving.", "Saving..", "Saving..."
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

    // NEW: Lock rules
    [Header("Lock Rules")]
    [Tooltip("If true, slot 0 cannot be used in Save mode (it will remain usable in Load mode).")]
    [SerializeField] private bool lockSlot0FromSaving = true;

    private const string TimeFormat = "yyyy-MM-dd HH:mm";
    private int _pendingSlotIndex = -1;
    private Coroutine _dotsRoutine;
    private Mode _mode = Mode.Save;

    public Mode CurrentMode => _mode;

    public bool IsOpen => panel != null && panel.activeSelf;
    public bool IsOverwriteOpen => overwritePanel != null && overwritePanel.activeSelf;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (overwritePanel != null) overwritePanel.SetActive(false);
        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);
        if (sfx != null) sfx.ignoreListenerPause = true;

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

    // ------------------ Public Open/Close API ------------------

    public void OpenForSave(OpenContext context = OpenContext.None)
    {
        Context = context;
        OpenPanelInternal(Mode.Save);
        if (headerText) headerText.text = "Save Game";
    }

    public void OpenForLoad(OpenContext context = OpenContext.None)
    {
        Context = context;
        OpenPanelInternal(Mode.Load);
        if (headerText) headerText.text = "Load Game";
    }

    public void ClosePanel()
    {
        overwritePanel?.SetActive(false);
        savingVisualRoot?.SetActive(false);
        if (panel) panel.SetActive(false);

        var ctx = Context;     // capture before reset
        Context = OpenContext.None;
        Closed?.Invoke(ctx);

        // ✅ IMPORTANT:
        // Do NOT open main menu or game over here.
        // UI.cs will decide what to show (so book animation stays consistent).
    }


    /// Route Esc/back here from UI.cs
    public bool HandleCancel()
    {
        if (overwritePanel && overwritePanel.activeSelf)
        {
            Play(cancelSfx);
            overwritePanel.SetActive(false);
            return true;
        }
        if (panel && panel.activeSelf)
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
        foreach (var slot in saveSlots)
        {
            if (slot != null) slot.UpdateSlotUI();
        }

        // NEW: re-apply locks after any refresh
        ApplyModeLocksToButtons();
    }

    /// Hook this from each slot button: pass its 0-based index.
    public void OnClickSlot(int slotIndex)
    {
        // NEW: block locked slots for the current mode (e.g., slot 0 in Save mode)
        if (IsSlotLockedForCurrentMode(slotIndex))
        {
            Play(cancelSfx);
            return;
        }

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

            // Prevent bounce-back to GameOver after a successful load.
            Context = OpenContext.None;

            try
            {
                if (AudioManager.instance != null)
                    AudioManager.instance.MarkBgmForRestoreAfterLoad();

                SaveSystem.LoadFromSlot(slotIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UI_SaveLoadPanel] Load failed: {e}");
            }

            ClosePanel(); // optional; scene load will hide this anyway
        }
    }

    // ------------------ Internals ------------------

    private void OpenPanelInternal(Mode mode)
    {
        _mode = mode;

        // Ensure visible objects are active before any coroutines start.
        if (gameObject.activeSelf == false) gameObject.SetActive(true);
        if (panel != null && panel.activeSelf == false) panel.SetActive(true);

        // Only Save mode uses these extras; just ensure they're hidden on open.
        overwritePanel?.SetActive(false);
        savingVisualRoot?.SetActive(false);

        Play(openDialogSfx);
        RefreshAllSlots();

        // NEW: enforce lock visuals on open
        ApplyModeLocksToButtons();
    }

    private void ShowOverwritePanel()
    {
        if (_mode != Mode.Save) return;

        var when = GetSlotTimestampStatic(_pendingSlotIndex);
        if (overwriteQuestionText != null)
            overwriteQuestionText.text = when.HasValue
                ? $"Overwrite this file?\nLast saved: {when.Value.ToString(TimeFormat, CultureInfo.InvariantCulture)}"
                : "Overwrite this file?";

        overwritePanel?.SetActive(true);
    }

    private static void WriteSceneNamesToPrefs(int slotIndex)
    {
        // Internal, unique scene name (prefer PixelCrushers if present)
        string sceneName;
        try { sceneName = SaveSystem.GetCurrentSceneName(); }
        catch { sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; }

        // Pretty display title (from a provider in the scene, else fall back to name)
        string sceneDisplay = sceneName;
        var provider = UnityEngine.Object.FindFirstObjectByType<SceneDisplayNameProvider>(FindObjectsInactive.Include);
        if (provider != null && !string.IsNullOrWhiteSpace(provider.DisplayName))
            sceneDisplay = provider.DisplayName;

        // Persist both
        PlayerPrefs.SetString($"SaveSlot_{slotIndex}_scene", sceneName);          // unique internal
        PlayerPrefs.SetString($"SaveSlot_{slotIndex}_sceneDisplay", sceneDisplay); // pretty title
        PlayerPrefs.Save();
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

    // UI_SaveLoadPanel.cs
    private IEnumerator SaveToSlotFlow(int slotIndex)
    {
        if (_mode != Mode.Save) yield break;

        SetSlotsInteractable(false);

        if (savingVisualRoot != null) savingVisualRoot.SetActive(true);
        if (savingText != null) savingText.text = "Saving.";
        Play(saveStartSfx);

        if (savingText != null)
        {
            if (_dotsRoutine != null) StopCoroutine(_dotsRoutine);
            _dotsRoutine = StartCoroutine(AnimateSavingDots());
        }

        float startTime = Time.unscaledTime;

        // Do the actual save and write metadata
        try
        {
            SaveSystem.SaveToSlot(slotIndex);

            // --- Metadata for your slot UI (scene, time, playtime) ---
            string sceneName;
            try { sceneName = SaveSystem.GetCurrentSceneName(); }
            catch { sceneName = SceneManager.GetActiveScene().name; }

            string sceneDisplay = SceneManager.GetActiveScene().name;
            var provider = FindFirstObjectByType<SceneDisplayNameProvider>(FindObjectsInactive.Include);
            if (provider != null && !string.IsNullOrEmpty(provider.DisplayName))
                sceneDisplay = provider.DisplayName;

            PlayerPrefs.SetString($"SaveSlot_{slotIndex}_scene", sceneName);
            PlayerPrefs.SetString($"SaveSlot_{slotIndex}_sceneDisplay", sceneDisplay);

            int uiSeconds = UI.Instance != null ? UI.Instance.CurrentTimePlayedSeconds : PlayTimeTracker.TotalSecondsInt;
            PlayerPrefs.SetInt($"SaveSlot_{slotIndex}_playSeconds", uiSeconds);

            PlayerPrefs.SetString($"SaveSlot_{slotIndex}_time", DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt($"SaveSlot_{slotIndex}_exists", 1);

            // Save current level music group too
            string musicGroup = string.Empty;
            if (LevelManager.Instance != null && !string.IsNullOrWhiteSpace(LevelManager.Instance.MusicGroupName))
                musicGroup = LevelManager.Instance.MusicGroupName;

            PlayerPrefs.SetString($"SaveSlot_{slotIndex}_musicGroup", musicGroup);

            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UI_SaveLoadPanel] Save failed for slot {slotIndex}: {ex}");
        }

        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < minimumShowTime)
            yield return new WaitForSecondsRealtime(minimumShowTime - elapsed);

        if (_dotsRoutine != null)
        {
            StopCoroutine(_dotsRoutine);
            _dotsRoutine = null;
        }

        Play(saveDoneSfx);
        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);

        SetSlotsInteractable(true);
        RefreshAllSlots();
    }

    public static string GetSavedMusicGroup(int slotIndex)
    {
        return PlayerPrefs.GetString($"SaveSlot_{slotIndex}_musicGroup", string.Empty);
    }

    private IEnumerator AnimateSavingDots()
    {
        string baseText = "Saving";
        int dotCount = 1;
        while (true)
        {
            if (savingText != null) savingText.text = baseText + new string('.', dotCount);
            dotCount = (dotCount % 3) + 1;
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    // -------- Slot helpers --------

    private bool SlotHasData(int slotIndex)
    {
        // Prefer PixelCrushers API if present:
        try { return SaveSystem.HasSavedGameInSlot(slotIndex); }
        catch { /* fall through */ }

        // Fallback: our PlayerPrefs flag
        return PlayerPrefs.GetInt(SlotKey(slotIndex, "exists"), 0) == 1;
    }

    public static DateTime? GetSlotTimestampStatic(int slotIndex)
    {
        var s = PlayerPrefs.GetString($"SaveSlot_{slotIndex}_time", string.Empty);
        if (string.IsNullOrEmpty(s)) return null;

        if (DateTime.TryParseExact(s, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            return t;

        return null;
    }

    private string SlotKey(int slotIndex, string suffix) => $"SaveSlot_{slotIndex}_{suffix}";

    // -------- Audio and UI helpers --------

    private void Play(AudioClip clip)
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip);
    }

    // NEW: determine if a slot is locked in the current mode
    private bool IsSlotLockedForCurrentMode(int slotIndex)
    {
        return _mode == Mode.Save && lockSlot0FromSaving && slotIndex == 0;
    }

    // NEW: apply locks to visible buttons based on mode (called on open/refresh)
    private void ApplyModeLocksToButtons()
    {
        if (saveSlots == null) return;

        for (int i = 0; i < saveSlots.Length; i++)
        {
            var slot = saveSlots[i];
            if (slot == null) continue;

            bool locked = IsSlotLockedForCurrentMode(i);

            if (_mode == Mode.Save)
            {
                if (slot.saveButton != null) slot.saveButton.interactable = !locked;
                // Load buttons may exist on the prefab; keep them off in Save mode if you prefer:
                // if (slot.loadButton != null) slot.loadButton.interactable = false;
            }
            else // Load mode
            {
                if (slot.loadButton != null) slot.loadButton.interactable = true;
                // In Load mode, save buttons are irrelevant:
                // if (slot.saveButton != null) slot.saveButton.interactable = false;
            }
        }
    }

    // UPDATED: respect per-slot lock when toggling interactability
    private void SetSlotsInteractable(bool interactable)
    {
        if (saveSlots == null) return;

        for (int i = 0; i < saveSlots.Length; i++)
        {
            var slot = saveSlots[i];
            if (slot == null) continue;

            if (_mode == Mode.Save)
            {
                if (slot.saveButton != null)
                    slot.saveButton.interactable = interactable && !IsSlotLockedForCurrentMode(i);
            }
            else
            {
                if (slot.loadButton != null)
                    slot.loadButton.interactable = interactable;
            }
        }
    }
}
