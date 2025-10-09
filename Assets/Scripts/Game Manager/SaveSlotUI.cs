using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers;
using System;
using System.Globalization;

public class SaveSlotUI : MonoBehaviour
{
    [Header("Identity")]
    public int slotID; // 0-based index

    [Header("Labels (visible when the slot has data)")]
    [SerializeField] private TextMeshProUGUI locationText;   // "Location: Forest Area 0"
    [SerializeField] private TextMeshProUGUI playTimeText;   // "Time played: 00:00:00"
    [SerializeField] private TextMeshProUGUI dateText;       // "10-12-2025"
    [SerializeField] private TextMeshProUGUI clockText;      // "4:25pm"

    [Header("Empty Label (visible when no data)")]
    [SerializeField] private TextMeshProUGUI emptyText;      // "Empty Slot"

    [Header("Optional header + portrait")]
    [SerializeField] private TextMeshProUGUI slotLabel;      // "Slot 1"
    [SerializeField] private Image playerSpriteImage;        // portrait (optional)

    [Header("Buttons (optional wire-up)")]
    public Button saveButton;                                // referenced by panel
    public Button loadButton;                                // referenced by panel

    // Storage formats/keys
    private const string StoredTimeFmt = "yyyy-MM-dd HH:mm"; // how you store timestamp
    private static string KeyScene(int s) => $"SaveSlot_{s}_scene";
    private static string KeySceneDisplay(int s) => $"SaveSlot_{s}_sceneDisplay"; // <-- ADDED (friendly title)
    private static string KeyPlaySec(int s) => $"SaveSlot_{s}_playSeconds";
    private static string KeyTime(int s) => $"SaveSlot_{s}_time";
    private static string KeyExists(int s) => $"SaveSlot_{s}_exists";
    private static string KeyPortrait(int s) => $"SaveSlot_{s}_portrait"; // optional (Resources path)

    private void Awake()
    {
        AutoFindButtonsIfMissing();
    }

    private void Start()
    {
        UpdateSlotUI();
    }

    private void OnEnable()  // <-- refresh when panel reopens
    {
        UpdateSlotUI();
    }

    public void UpdateSlotUI()
    {
        if (slotLabel) slotLabel.text = $"Slot {slotID + 1}";

        bool hasData = HasData(slotID);

        if (hasData)
        {
            // Show info labels
            if (emptyText) emptyText.gameObject.SetActive(false);
            SafeSetActive(locationText, true);
            SafeSetActive(playTimeText, true);
            SafeSetActive(dateText, true);
            SafeSetActive(clockText, true);

            // Location (prefer friendly display name)
            if (locationText)
            {
                string display = PlayerPrefs.GetString(KeySceneDisplay(slotID), string.Empty);
                if (!string.IsNullOrEmpty(display))
                    locationText.text = $"Location: {display}";
                else
                {
                    string scene = PlayerPrefs.GetString(KeyScene(slotID), "Unknown");
                    locationText.text = $"Location: {scene}";
                }
            }

            // Time Played (HH:MM:SS) — uses exact seconds saved by UI_SaveLoadPanel
            int playSeconds = PlayerPrefs.GetInt(KeyPlaySec(slotID), 0);
            if (playTimeText) playTimeText.text = $"Time played: {FormatHHMMSS(playSeconds)}";

            // Timestamp → display as DD-MM-YYYY and h:mm am/pm (lowercase)
            string ts = PlayerPrefs.GetString(KeyTime(slotID), string.Empty);
            if (DateTime.TryParseExact(ts, StoredTimeFmt, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out var when))
            {
                if (dateText) dateText.text = when.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                if (clockText) clockText.text = when.ToString("h:mmtt", CultureInfo.InvariantCulture).ToLowerInvariant();
            }
            else
            {
                if (dateText) dateText.text = "-";
                if (clockText) clockText.text = "-";
            }

            // Optional: portrait from Resources path you may have stored at save time
            if (playerSpriteImage)
            {
                string portraitId = PlayerPrefs.GetString(KeyPortrait(slotID), string.Empty);
                if (!string.IsNullOrEmpty(portraitId))
                {
                    var spr = Resources.Load<Sprite>(portraitId);
                    if (spr) playerSpriteImage.sprite = spr;
                }
                // If you didn't store anything, whatever is already assigned stays.
            }
        }
        else
        {
            // Hide info, show "Empty Slot"
            SafeSetActive(locationText, false);
            SafeSetActive(playTimeText, false);
            SafeSetActive(dateText, false);
            SafeSetActive(clockText, false);

            if (emptyText)
            {
                emptyText.gameObject.SetActive(true);
                emptyText.text = "Empty Slot";
            }
        }
    }

    public void SaveGame()
    {
        SaveSystem.SaveToSlot(slotID);
        // Your panel’s coroutine writes the metadata (scene, playSeconds, time, exists).
        UpdateSlotUI();
    }

    public void LoadGame()
    {
        if (HasData(slotID))
        {
            SaveSystem.LoadFromSlot(slotID);
        }
        else
        {
            Debug.LogWarning($"[SaveSlotUI] No save found in slot {slotID}");
        }
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (saveButton) saveButton.interactable = interactable;
        if (loadButton) loadButton.interactable = interactable;
    }

    // ======== public helper if you want to refresh only this line ========
    public void RefreshLocationLabelOnly()
    {
        if (!locationText) return;

        if (!HasData(slotID))
        {
            SafeSetActive(locationText, false);
            return;
        }

        SafeSetActive(locationText, true);

        string display = PlayerPrefs.GetString(KeySceneDisplay(slotID), string.Empty);
        if (!string.IsNullOrEmpty(display))
            locationText.text = $"Location: {display}";
        else
            locationText.text = $"Location: {PlayerPrefs.GetString(KeyScene(slotID), "Unknown")}";
    }

    // --- helpers ---

    private static bool HasData(int slot)
    {
        // Prefer SaveSystem provider:
        try { return SaveSystem.HasSavedGameInSlot(slot); }
        catch { /* fallback */ }
        return PlayerPrefs.GetInt(KeyExists(slot), 0) == 1;
    }

    // keep old HH:MM (in case other UI uses it)
    private static string FormatHHMM(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        int h = totalSeconds / 3600;
        int m = (totalSeconds % 3600) / 60;
        return $"{h:00}:{m:00}";
    }

    // NEW: HH:MM:SS for save slot display
    private static string FormatHHMMSS(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        int h = totalSeconds / 3600;
        int m = (totalSeconds % 3600) / 60;
        int s = totalSeconds % 60;
        return $"{h:00}:{m:00}:{s:00}";
    }

    private void AutoFindButtonsIfMissing()
    {
        if (!saveButton || !loadButton)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                var n = b.name.ToLowerInvariant();
                if (!saveButton && n.Contains("save")) saveButton = b;
                else if (!loadButton && n.Contains("load")) loadButton = b;
            }
            if (!saveButton && loadButton) saveButton = loadButton;
            if (!loadButton && saveButton) loadButton = saveButton;
        }
    }

    private static void SafeSetActive(Behaviour comp, bool v)
    {
        if (!comp) return;
        comp.gameObject.SetActive(v);
    }
}
