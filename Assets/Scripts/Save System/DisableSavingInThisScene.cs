#if PIXELCRUSHERS
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using PixelCrushers;

/// Disable Pixel Crushers autosaves in this scene.
/// Works across Save System versions by reflecting known field/property names.
/// Optionally disables the component entirely to block manual saves as well.
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class DisableSavingInThisScene : MonoBehaviour
{
    [Tooltip("If true, also disable the SaveSystem component while this scene is active (blocks manual saves too).")]
    public bool disableComponentToBlockManualSaves = false;

    [Tooltip("Log what this component changes.")]
    public bool verbose = true;

    private SaveSystem _sys;

    // Backup of values we change so we can restore them on exit.
    private readonly Dictionary<string, object> _backups = new Dictionary<string, object>();

    // Common names used by various Save System versions:
    private static readonly string[] BoolNames_SceneChange = { "autoSaveOnSceneChange", "saveOnSceneChange" };
    private static readonly string[] BoolNames_OnQuit = { "autoSaveOnQuit", "saveOnQuit" };
    private static readonly string[] FloatNames_Interval = { "autoSaveFrequencyInSeconds", "autoSaveEverySeconds", "autoSaveIntervalSeconds", "autoSaveInterval" };
    private static readonly string[] BoolNames_GlobalAllow = { "isSavingAllowed", "allowSave", "allowSaving", "saveAllowed" };

    private void OnEnable()
    {
        _sys = FindObjectOfType<SaveSystem>(true);
        if (_sys == null)
        {
            if (verbose) Debug.LogWarning("[DisableSavingInThisScene] No PixelCrushers.SaveSystem found in scene.");
            return;
        }

        // Turn off autosaves:
        BackupAndSet(_sys, BoolNames_SceneChange, false);
        BackupAndSet(_sys, BoolNames_OnQuit, false);
        BackupAndSet(_sys, FloatNames_Interval, 0f);

        // If SaveSystem exposes a global allow flag, set it false (blocks many autosave pathways):
        BackupAndSet(_sys, BoolNames_GlobalAllow, false);

        if (disableComponentToBlockManualSaves)
        {
            BackupEnabled(_sys);
            _sys.enabled = false;
        }

        if (verbose) Debug.Log("[DisableSavingInThisScene] Saving disabled in this scene.");
    }

    private void OnDisable()
    {
        if (_sys == null) return;

        // Restore everything we changed:
        RestoreAll(_sys);

        if (verbose) Debug.Log("[DisableSavingInThisScene] Restored SaveSystem state.");
    }

    // ───────────────────────── helpers ─────────────────────────

    private void BackupAndSet(SaveSystem sys, string[] names, bool newValue)
    {
        foreach (var n in names) if (TryBackup(sys, n)) { TrySet(sys, n, newValue); break; }
    }

    private void BackupAndSet(SaveSystem sys, string[] names, float newValue)
    {
        foreach (var n in names) if (TryBackup(sys, n)) { TrySet(sys, n, newValue); break; }
    }

    private void BackupEnabled(SaveSystem sys)
    {
        const string k = "__component_enabled__";
        if (!_backups.ContainsKey(k)) _backups[k] = sys.enabled;
    }

    private void RestoreAll(SaveSystem sys)
    {
        foreach (var kv in _backups)
        {
            if (kv.Key == "__component_enabled__") { sys.enabled = (bool)kv.Value; continue; }
            TrySet(sys, kv.Key, kv.Value);
        }
        _backups.Clear();
    }

    private bool TryBackup(object obj, string memberName)
    {
        if (_backups.ContainsKey(memberName)) return true; // already backed up
        if (TryGetMember(obj, memberName, out var cur))
        {
            _backups[memberName] = cur;
            return true;
        }
        return false;
    }

    private static bool TrySet(object obj, string memberName, object value)
    {
        var t = obj.GetType();
        // property
        var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite) { p.SetValue(obj, ConvertTo(value, p.PropertyType), null); return true; }
        // field
        var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null) { f.SetValue(obj, ConvertTo(value, f.FieldType)); return true; }
        return false;
    }

    private static bool TryGetMember(object obj, string memberName, out object value)
    {
        value = null;
        var t = obj.GetType();
        var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanRead) { value = p.GetValue(obj, null); return true; }
        var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null) { value = f.GetValue(obj); return true; }
        return false;
    }

    private static object ConvertTo(object value, Type targetType)
    {
        try
        {
            if (value == null || targetType.IsInstanceOfType(value)) return value;
            if (targetType == typeof(bool)) return Convert.ToBoolean(value);
            if (targetType == typeof(float)) return Convert.ToSingle(value);
            if (targetType.IsEnum) return Enum.ToObject(targetType, value);
            return Convert.ChangeType(value, targetType);
        }
        catch { return value; }
    }
}
#endif
