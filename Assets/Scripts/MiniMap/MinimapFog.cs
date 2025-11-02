using System;
using UnityEngine;

[DefaultExecutionOrder(-900)]
public class MinimapFog : MonoBehaviour
{
    public static MinimapFog Instance { get; private set; }

    [Header("References")]
    public MinimapSettings settings;

    [Header("Runtime")]
    public Texture2D fogTexture;    // Alpha = 1 (black) undiscovered, 0 (transparent) discovered
    private Color32[] _pixels;
    private bool _dirty;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (settings == null)
        {
            Debug.LogError("[MinimapFog] Missing MinimapSettings!");
            enabled = false;
            return;
        }

        CreateOrLoadTexture();
    }

    private void CreateOrLoadTexture()
    {
        fogTexture = new Texture2D(settings.textureWidth, settings.textureHeight, TextureFormat.ARGB32, false, true);
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Bilinear;

        _pixels = new Color32[settings.textureWidth * settings.textureHeight];

        // Try to load
        if (settings.saveToPlayerPrefs && PlayerPrefs.HasKey(settings.playerPrefsKey))
        {
            try
            {
                var data = Convert.FromBase64String(PlayerPrefs.GetString(settings.playerPrefsKey));
                fogTexture.LoadImage(data, false);
                fogTexture.Apply(false);
                _pixels = fogTexture.GetPixels32();
                return;
            }
            catch { /* ignore and init fresh */ }
        }

        // Initialize fully opaque black (undiscovered)
        for (int i = 0; i < _pixels.Length; i++)
            _pixels[i] = new Color32(0, 0, 0, 255);

        fogTexture.SetPixels32(_pixels);
        fogTexture.Apply(false);
    }

    private void LateUpdate()
    {
        if (_dirty)
        {
            fogTexture.SetPixels32(_pixels);
            fogTexture.Apply(false);
            _dirty = false;
        }
    }

    private void OnApplicationQuit() => Save();
#if UNITY_EDITOR
    private void OnDisable() => Save();
#endif

    private void Save()
    {
        if (!settings.saveToPlayerPrefs || fogTexture == null) return;
        try
        {
            var png = fogTexture.EncodeToPNG();
            var b64 = Convert.ToBase64String(png);
            PlayerPrefs.SetString(settings.playerPrefsKey, b64);
            PlayerPrefs.Save();
        }
        catch { /* ignore */ }
    }

    /// Reveal a circle in world space
    public void RevealCircle(Vector2 worldPos, float worldRadius)
    {
        if (fogTexture == null || _pixels == null) return;

        WorldToUV(worldPos, out float u, out float v);
        int cx = Mathf.RoundToInt(u * (settings.textureWidth - 1));
        int cy = Mathf.RoundToInt(v * (settings.textureHeight - 1));

        float pxPerUnitX = (settings.textureWidth - 1) / (settings.worldMax.x - settings.worldMin.x);
        float pxPerUnitY = (settings.textureHeight - 1) / (settings.worldMax.y - settings.worldMin.y);
        float prx = worldRadius * pxPerUnitX;
        float pry = worldRadius * pxPerUnitY;
        // Approximate with average radius for circle
        int pr = Mathf.CeilToInt((prx + pry) * 0.5f);

        int x0 = Mathf.Max(0, cx - pr);
        int x1 = Mathf.Min(settings.textureWidth - 1, cx + pr);
        int y0 = Mathf.Max(0, cy - pr);
        int y1 = Mathf.Min(settings.textureHeight - 1, cy + pr);

        int w = settings.textureWidth;

        int pr2 = pr * pr;
        for (int y = y0; y <= y1; y++)
        {
            int dy = y - cy;
            int dy2 = dy * dy;
            int row = y * w;
            for (int x = x0; x <= x1; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy2 <= pr2)
                {
                    int idx = row + x;
                    var c = _pixels[idx];
                    // Set alpha to 0 (transparent = discovered), keep RGB black
                    if (c.a != 0) { c.a = 0; _pixels[idx] = c; _dirty = true; }
                }
            }
        }
    }

    public void WorldToUV(Vector2 world, out float u, out float v)
    {
        var min = settings.worldMin;
        var max = settings.worldMax;
        u = Mathf.InverseLerp(min.x, max.x, world.x);
        v = Mathf.InverseLerp(min.y, max.y, world.y);
    }
}
