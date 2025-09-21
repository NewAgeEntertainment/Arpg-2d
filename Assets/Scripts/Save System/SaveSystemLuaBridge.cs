#if PIXELCRUSHERS
using System.Reflection;
using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

public class SaveSystemLuaBridge : MonoBehaviour
{
    private void OnEnable()
    {
        Lua.RegisterFunction("SetSavingEnabled", this,
            SymbolExtensions.GetMethodInfo(() => SetSavingEnabled(false)));
    }

    private void OnDisable()
    {
        Lua.UnregisterFunction("SetSavingEnabled");
    }

    // Lua: SetSavingEnabled(true/false)
    public void SetSavingEnabled(bool enabled)
    {
        var sys = Object.FindObjectOfType<SaveSystem>(true);
        if (sys == null)
        {
            Debug.LogWarning("[SaveSystemLuaBridge] No SaveSystem found.");
            return;
        }

        // Try common flags across versions; remember if we set any.
        bool applied = false;
        applied |= SetIfExists(sys, "isSavingAllowed", enabled);
        applied |= SetIfExists(sys, "allowSave", enabled);
        applied |= SetIfExists(sys, "allowSaving", enabled);
        applied |= SetIfExists(sys, "saveAllowed", enabled);

        // Fallback: toggle the component itself if no known flag exists.
        if (!applied) sys.enabled = enabled;
    }

    private static bool SetIfExists(object obj, string member, object value)
    {
        var t = obj.GetType();

        var p = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite) { p.SetValue(obj, value, null); return true; }

        var f = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null) { f.SetValue(obj, value); return true; }

        return false;
    }
}
#endif
