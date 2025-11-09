using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MapFogOfWar : MonoBehaviour
{
    [Header("Baked Map Info")]
    public TilemapMapMetadata metadata;
    public Grid grid;                       // your Tilemap Grid
    [Tooltip("RectTransform of the Image/RawImage that displays the baked map PNG")]
    public RectTransform mapImageRect;

    [Header("Reveal")]
    [Range(1, 256)] public int revealRadiusPixels = 48;   // soft edge radius in pixels
    [Range(0f, 1f)] public float edgeSoftness = 0.5f;     // 0 = hard edge, 1 = very soft
    public bool revealContinuously = true;                // call RevealAtPlayer in Update

    // --- Internals
    Image _fogImg;               // this Image shows our fog texture
    Texture2D _fogTex;           // alpha = coverage (1 covered → 0 revealed)
    Color32[] _px;
    int _w, _h;
    bool _dirty;

    void Awake()
    {
        _fogImg = GetComponent<Image>();
        InitTexture();
    }

    void OnEnable()
    {
        if (_fogTex == null) InitTexture();
        ApplyPixels();
    }

    void Update()
    {
        if (revealContinuously && metadata && grid)
            RevealAtWorld(PlayerWorldPos());
        if (_dirty) ApplyPixels();
    }

    public void InitTexture(byte[] existingPngBytes = null)
    {
        if (!metadata)
        {
            Debug.LogWarning("MapFogOfWar: Missing metadata.");
            return;
        }
        _w = Mathf.Max(2, metadata.cellSize.x * metadata.pixelsPerCell);
        _h = Mathf.Max(2, metadata.cellSize.y * metadata.pixelsPerCell);

        if (_fogTex != null && (_fogTex.width != _w || _fogTex.height != _h))
        {
            Destroy(_fogTex);
            _fogTex = null;
        }
        if (_fogTex == null)
        {
            _fogTex = new Texture2D(_w, _h, TextureFormat.RGBA32, false, false);
            _fogTex.wrapMode = TextureWrapMode.Clamp;
            _px = new Color32[_w * _h];
        }

        if (existingPngBytes != null && existingPngBytes.Length > 0)
        {
            _fogTex.LoadImage(existingPngBytes);
            var arr = _fogTex.GetPixels32();
            if (_px.Length != arr.Length) _px = new Color32[_w * _h];
            for (int i = 0; i < _px.Length; i++) _px[i] = arr[i];
        }
        else
        {
            // Start fully covered (black with alpha 1)
            for (int i = 0; i < _px.Length; i++) _px[i] = new Color32(0, 0, 0, 255);
        }

        var spr = Sprite.Create(_fogTex, new Rect(0, 0, _w, _h), new Vector2(0.5f, 0.5f), 100f);
        _fogImg.sprite = spr;
        _fogImg.preserveAspect = false; // easier alignment; keep your map image similarly un-stretched
        _dirty = true;
    }

    public byte[] GetPngBytes()
    {
        // Important: ensure texture reflects _px before encoding
        _fogTex.SetPixels32(_px);
        _fogTex.Apply(false, false);
        return _fogTex.EncodeToPNG();
    }

    // Reveal a soft circle at a world position.
    public void RevealAtWorld(Vector3 worldPos)
    {
        if (!metadata || _px == null) return;
        Vector2 p = metadata.WorldToMapPixels(worldPos, grid);   // pixel coords in baked map
        RevealCircle((int)p.x, (int)p.y, revealRadiusPixels, edgeSoftness);
    }

    // Core painter: lowers alpha toward 0 (transparent) inside a soft circle.
    void RevealCircle(int cx, int cy, int r, float softness)
    {
        if (_px == null) return;
        int r2 = r * r;
        float softStart = Mathf.Clamp01(1f - softness); // 1 → hard edge, 0 → very soft
        float innerR = r * softStart;
        float innerR2 = innerR * innerR;

        int xMin = Mathf.Max(0, cx - r);
        int xMax = Mathf.Min(_w - 1, cx + r);
        int yMin = Mathf.Max(0, cy - r);
        int yMax = Mathf.Min(_h - 1, cy + r);

        for (int y = yMin; y <= yMax; y++)
        {
            int dy = y - cy;
            int dy2 = dy * dy;
            int row = y * _w;
            for (int x = xMin; x <= xMax; x++)
            {
                int dx = x - cx;
                int d2 = dx * dx + dy2;
                if (d2 > r2) continue;

                float a; // target alpha 0..1
                if (d2 <= innerR2) a = 0f;           // fully revealed
                else
                {
                    // Smooth falloff between inner and outer radius
                    float t = Mathf.InverseLerp(r2, innerR2, d2);
                    a = t; // linear; looks fine. Use t*t or smoothstep for softer
                    a = a * a * (3f - 2f * a); // smoothstep
                }

                int i = row + x;
                byte curA = _px[i].a;
                byte newA = (byte)Mathf.Min(curA, Mathf.RoundToInt(a * 255f)); // only ever reduce alpha
                if (newA != curA)
                {
                    _px[i].a = newA;
                    // keep color black
                }
            }
        }
        _dirty = true;
    }

    void ApplyPixels()
    {
        if (_fogTex == null || _px == null) return;
        _fogTex.SetPixels32(_px);
        _fogTex.Apply(false, false);
        _dirty = false;
    }

    Vector3 PlayerWorldPos()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        return p ? p.transform.position : Vector3.zero;
    }
}
