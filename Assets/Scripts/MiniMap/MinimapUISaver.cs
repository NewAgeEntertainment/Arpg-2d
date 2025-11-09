using UnityEngine;

#if PIXEL_CRUSHERS
using PixelCrushers;
#endif

/// <summary>
/// Saves/restores MinimapUI presentation (fullscreen state + mini layout settings).
/// Add this to the same GameObject as MinimapUI. Make sure the object has a UniqueID
/// (Pixel Crushers will add a "Save System Identifier" if missing).
/// </summary>
#if PIXEL_CRUSHERS
[AddComponentMenu("Saving/Minimap UI Saver")]
public class MinimapUISaver : Saver
{
    [Tooltip("If not assigned, this component will auto-find MinimapUI on the same GameObject.")]
    public MinimapUI ui;

    [System.Serializable]
    public class Data
    {
        public bool isFullscreen;
        public int miniLayoutMode;      // cast of MinimapUI.MiniLayoutMode
        public Vector2 miniSizePx;
        public Vector2 miniMarginPx;
        public Vector2 miniAnchorMin;
        public Vector2 miniAnchorMax;
        public Vector2 fullAnchorMin;
        public Vector2 fullAnchorMax;
    }

    void Reset()
    {
        if (ui == null) ui = GetComponent<MinimapUI>();
    }

    public override string RecordData()
    {
        if (ui == null) ui = GetComponent<MinimapUI>();
        if (ui == null) return string.Empty;

        var d = new Data
        {
            isFullscreen  = ui.IsFullscreen,
            miniLayoutMode= (int)ui.miniLayoutMode,
            miniSizePx    = ui.miniSizePx,
            miniMarginPx  = ui.miniMarginPx,
            miniAnchorMin = ui.miniAnchorMin,
            miniAnchorMax = ui.miniAnchorMax,
            fullAnchorMin = ui.fullAnchorMin,
            fullAnchorMax = ui.fullAnchorMax
        };

        return SaveSystem.Serialize(d);
    }

    public override void ApplyData(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        if (ui == null) ui = GetComponent<MinimapUI>();
        if (ui == null) return;

        var d = SaveSystem.Deserialize<Data>(s);
        if (d == null) return;

        // Restore fields
        ui.miniLayoutMode = (MinimapUI.MiniLayoutMode)d.miniLayoutMode;
        ui.miniSizePx     = d.miniSizePx;
        ui.miniMarginPx   = d.miniMarginPx;
        ui.miniAnchorMin  = d.miniAnchorMin;
        ui.miniAnchorMax  = d.miniAnchorMax;
        ui.fullAnchorMin  = d.fullAnchorMin;
        ui.fullAnchorMax  = d.fullAnchorMax;

        // Reapply fog (in case a new MinimapFog was created this scene)
        ui.ReapplyFogTexture();

        // Apply layout state last (this calls into SetFullscreen and updates layout)
        ui.SetFullscreen(d.isFullscreen);
    }
}
#else
// Safe stub when Pixel Crushers isn't present (keeps project compiling).
public class MinimapUISaver : MonoBehaviour
{
    [Tooltip("If Pixel Crushers Save System is not in the project, this component is a no-op.")]
    public MinimapUI ui;

    void Reset()
    {
        if (ui == null) ui = GetComponent<MinimapUI>();
    }
}
#endif
