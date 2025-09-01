using PixelCrushers.DialogueSystem;
using UnityEngine;

public class ConquestLua : MonoBehaviour
{
    void OnEnable()
    {
        Lua.RegisterFunction("UnlockRoster", this, SymbolExtensions.GetMethodInfo(() => UnlockRoster(string.Empty)));
        Lua.RegisterFunction("IsUnlockedRoster", this, SymbolExtensions.GetMethodInfo(() => IsUnlockedRoster(string.Empty)));
        Lua.RegisterFunction("AddAffection", this, SymbolExtensions.GetMethodInfo(() => AddAffection(string.Empty, 0.0)));
        Lua.RegisterFunction("SetAffection", this, SymbolExtensions.GetMethodInfo(() => SetAffection(string.Empty, 0.0)));
        Lua.RegisterFunction("GetAffection", this, SymbolExtensions.GetMethodInfo(() => GetAffection(string.Empty)));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("UnlockRoster");
        Lua.UnregisterFunction("IsUnlockedRoster");
        Lua.UnregisterFunction("AddAffection");
        Lua.UnregisterFunction("SetAffection");
        Lua.UnregisterFunction("GetAffection");
    }

    // Lua: UnlockRoster("Elaina")
    public void UnlockRoster(string profileName)
    {
        var mgr = ConquestRosterManager.Instance;
        if (mgr == null) { Debug.LogWarning("[ConquestLua] No ConquestRosterManager in scene."); return; }

        var profile = mgr.FindProfileByName(profileName);
        if (profile == null)
        {
            Debug.LogWarning($"[ConquestLua] Profile '{profileName}' not found in scene refs.");
            return;
        }

        var stats = mgr.FindStatsForProfile(profile); // <-- this is why you saw the error; now it exists
        mgr.Unlock(profile, stats);
        Debug.Log($"[ConquestLua] Unlocked: {profile.name}");
    }

    // Lua: IsUnlockedRoster("Elaina")
    public bool IsUnlockedRoster(string profileName)
    {
        var mgr = ConquestRosterManager.Instance;
        if (mgr == null) return false;

        var profile = mgr.FindProfileByName(profileName);
        return mgr.IsUnlocked(profile);
    }

    // Lua: AddAffection("Elaina", 5)
    public void AddAffection(string profileName, double delta)
    {
        var mgr = ConquestRosterManager.Instance;
        if (mgr == null) return;

        var profile = mgr.FindProfileByName(profileName);
        if (profile == null) return;

        mgr.AddAffection(profile, (int)delta);
    }

    // Lua: SetAffection("Elaina", 42)
    public void SetAffection(string profileName, double value)
    {
        var mgr = ConquestRosterManager.Instance;
        if (mgr == null) return;

        var profile = mgr.FindProfileByName(profileName);
        if (profile == null) return;

        mgr.SetAffection(profile, (int)value);
    }

    // Lua: GetAffection("Elaina")
    public int GetAffection(string profileName)
    {
        var mgr = ConquestRosterManager.Instance;
        if (mgr == null) return 0;

        var profile = mgr.FindProfileByName(profileName);
        return mgr.GetAffection(profile);
    }
}
