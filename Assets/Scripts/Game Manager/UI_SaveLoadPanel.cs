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

    public enum OpenContext { None, TitleMenu, PauseMenu, GameOver }
    public OpenContext Context { get; private set; } = OpenContext.None;
    public event Action<OpenContext> Closed;

    [Header("Mode UI")]
    [SerializeField] private TextMeshProUGUI headerText;

    [Header("Slots")]
    public SaveSlotUI[] saveSlots;
    [SerializeField] private GameObject panel;

    [Header("Overwrite Panel (Save mode only)")]
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private TextMeshProUGUI overwriteQuestionText;
    [SerializeField] private Button overwriteYesButton;
    [SerializeField] private Button overwriteNoButton;

    [Header("Saving Visual (Save mode only)")]
    [SerializeField] private GameObject savingVisualRoot;
    [SerializeField] private TextMeshProUGUI savingText;
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

    private const string TimeFormat = "yyyy-MM-dd HH:mm";
    private int _pendingSlotIndex = -1;
    private Coroutine _dotsRoutine;
    private Mode _mode = Mode.Save;

    public bool IsOpen => (panel != null ? panel.activeSelf : gameObject.activeSelf);

    private void Awake()
    {
        if (panel == null) panel = gameObject;

        panel?.SetActive(false);
        overwritePanel?.SetActive(false);
        savingVisualRoot?.SetActive(false);
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
        panel?.SetActive(false);

        var ctx = Context;
        Context = OpenContext.None;
        Closed?.Invoke(ctx);

        if (ctx == OpenContext.GameOver) UI_GameOver.ShowStatic();
    }

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
        foreach (var slot in saveSlots) if (slot != null) slot.UpdateSlotUI();
    }

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
        else
        {
            if (!SlotHasData(slotIndex))
            {
                Play(loadErrorSfx);
                return;
            }

            Play(loadClickSfx);
            Context = OpenContext.None;
            try { SaveSystem.LoadFromSlot(slotIndex); }
            catch (Exception e) { Debug.LogError($"[UI_SaveLoadPanel] Load failed: {e}"); }
            ClosePanel();
        }
    }

    private void OpenPanelInternal(Mode mode)
    {
        _mode = mode;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (panel != null && !panel.activeSelf) panel.SetActive(true);
        overwritePanel?.SetActive(false);
        savingVisualRoot?.SetActive(false);
        Play(openDialogSfx);
        RefreshAllSlots();
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

    private void OnClickOverwriteYes()
    {
        if (_mode != Mode.Save) return;
        Play(confirmSfx);
        overwritePanel?.SetActive(false);
        if (_pendingSlotIndex >= 0) StartCoroutine(SaveToSlotFlow(_pendingSlotIndex));
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

        try
        {
            SaveSystem.SaveToSlot(slotIndex);

            string sceneName;
            try { sceneName = SaveSystem.GetCurrentSceneName(); }
            catch { sceneName = SceneManager.GetActiveScene().name; }

            PlayerPrefs.SetString($"SaveSlot_{slotIndex}_scene", sceneName);
            PlayerPrefs.SetInt($"SaveSlot_{slotIndex}_playSeconds", PlayTimeTracker.TotalSecondsInt);
            PlayerPrefs.SetString($"SaveSlot_{slotIndex}_time", DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt($"SaveSlot_{slotIndex}_exists", 1);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UI_SaveLoadPanel] Save failed for slot {slotIndex}: {ex}");
        }

        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < minimumShowTime)
            yield return new WaitForSecondsRealtime(minimumShowTime - elapsed);

        if (_dotsRoutine != null) { StopCoroutine(_dotsRoutine); _dotsRoutine = null; }

        Play(saveDoneSfx);
        if (savingVisualRoot != null) savingVisualRoot.SetActive(false);

        SetSlotsInteractable(true);
        RefreshAllSlots();
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

    private bool SlotHasData(int slotIndex)
    {
        try { return SaveSystem.HasSavedGameInSlot(slotIndex); } catch { }
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

    private void Play(AudioClip clip)
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip);
    }

    private void SetSlotsInteractable(bool interactable)
    {
        if (saveSlots == null) return;
        foreach (var slot in saveSlots)
        {
            if (slot == null) continue;
            if (_mode == Mode.Save)
            {
                if (slot.saveButton != null) slot.saveButton.interactable = interactable;
            }
            else
            {
                if (slot.loadButton != null) slot.loadButton.interactable = interactable;
            }
        }
    }
}
