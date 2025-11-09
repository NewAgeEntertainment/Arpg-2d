#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class TilemapAreaMapBaker : EditorWindow
{
    [System.Serializable]
    public class LayerEntry
    {
        public Tilemap tilemap;
        public Color color = Color.white;
        public bool enabled = true;
    }

    public enum RenderMode { FlatColors /*, SpriteSampling*/ }

    public Grid grid;
    public List<LayerEntry> layers = new List<LayerEntry>();
    public bool includeInactive = true;

    public RenderMode renderMode = RenderMode.FlatColors;
    public int pixelsPerCell = 32;
    public Color background = Color.black;
    public string subFolder = "Maps";
    public string fileNameOverride = "";

    [MenuItem("Tools/Area Map/Bake Tilemap Map...")]
    static void Open() => GetWindow<TilemapAreaMapBaker>("Tilemap Map Baker").Show();

    void OnEnable()
    {
        if (layers.Count == 0) AutoCollectTilemaps();
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Bake a PNG from selected Tilemaps.", MessageType.Info);
        grid = (Grid)EditorGUILayout.ObjectField("Grid (optional)", grid, typeof(Grid), true);
        if (GUILayout.Button("Auto-Collect Tilemaps (children)")) AutoCollectTilemaps();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layers (top renders last):", EditorStyles.boldLabel);
        for (int i = 0; i < layers.Count; i++)
        {
            var e = layers[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            e.enabled = EditorGUILayout.Toggle(e.enabled, GUILayout.Width(22));
            e.tilemap = (Tilemap)EditorGUILayout.ObjectField(e.tilemap, typeof(Tilemap), true);
            if (GUILayout.Button("▲", GUILayout.Width(24)) && i > 0) { (layers[i], layers[i - 1]) = (layers[i - 1], layers[i]); }
            if (GUILayout.Button("▼", GUILayout.Width(24)) && i < layers.Count - 1) { (layers[i], layers[i + 1]) = (layers[i + 1], layers[i]); }
            if (GUILayout.Button("✕", GUILayout.Width(24))) { layers.RemoveAt(i); i--; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
            EditorGUILayout.EndHorizontal();

            if (renderMode == RenderMode.FlatColors)
                e.color = EditorGUILayout.ColorField("Color", e.color);
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Tilemap")) layers.Add(new LayerEntry());

        EditorGUILayout.Space();
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        renderMode = (RenderMode)EditorGUILayout.EnumPopup("Render Mode", renderMode);
        pixelsPerCell = Mathf.Max(4, EditorGUILayout.IntField("Pixels Per Cell", pixelsPerCell));
        background = EditorGUILayout.ColorField("Background", background);
        subFolder = EditorGUILayout.TextField("Subfolder (under Assets)", subFolder);
        fileNameOverride = EditorGUILayout.TextField("File Name (optional)", fileNameOverride);

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake Map PNG", GUILayout.Height(30))) Bake();
    }

    void AutoCollectTilemaps()
    {
        layers.Clear();
        Grid baseGrid = grid;
        if (!baseGrid)
        {
            var allGrids = FindObjectsOfType<Grid>(includeInactive);
            if (allGrids.Length > 0) baseGrid = allGrids[0];
        }

        Tilemap[] tms = baseGrid ? baseGrid.GetComponentsInChildren<Tilemap>(includeInactive)
                                 : FindObjectsOfType<Tilemap>(includeInactive);
        foreach (var tm in tms)
            layers.Add(new LayerEntry { tilemap = tm, color = Color.white, enabled = true });
    }

    void Bake()
    {
        var activeLayers = layers.FindAll(l => l.enabled && l.tilemap != null);
        if (activeLayers.Count == 0) { EditorUtility.DisplayDialog("Baker", "No Tilemaps selected.", "OK"); return; }

        bool hasBounds = false;
        BoundsInt combined = new BoundsInt();
        foreach (var L in activeLayers)
        {
            var b = L.tilemap.cellBounds;
            if (!hasBounds) { combined = b; hasBounds = true; }
            else
            {
                int minX = Mathf.Min(combined.xMin, b.xMin);
                int minY = Mathf.Min(combined.yMin, b.yMin);
                int maxX = Mathf.Max(combined.xMax, b.xMax);
                int maxY = Mathf.Max(combined.yMax, b.yMax);
                combined = new BoundsInt(minX, minY, 0, maxX - minX, maxY - minY, 1);
            }
        }
        if (!hasBounds || combined.size.x <= 0 || combined.size.y <= 0)
        { EditorUtility.DisplayDialog("Baker", "Empty bounds.", "OK"); return; }

        int cellsX = combined.size.x;
        int cellsY = combined.size.y;
        int width = cellsX * pixelsPerCell;
        int height = cellsY * pixelsPerCell;

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var bg32 = To32(background);
        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg32;

        foreach (var L in activeLayers)
        {
            var tm = L.tilemap;
            var color = To32(L.color);
            var b = tm.cellBounds;
            var tileArray = tm.GetTilesBlock(b);

            for (int y = 0; y < b.size.y; y++)
                for (int x = 0; x < b.size.x; x++)
                {
                    var t = tileArray[x + y * b.size.x];
                    if (t == null) continue;

                    int cx = (b.xMin + x) - combined.xMin;
                    int cy = (b.yMin + y) - combined.yMin;

                    int pxMin = cx * pixelsPerCell;
                    int pyMin = cy * pixelsPerCell;

                    for (int py = 0; py < pixelsPerCell; py++)
                    {
                        int row = (pyMin + py) * width + pxMin;
                        for (int px = 0; px < pixelsPerCell; px++)
                            pixels[row + px] = color;
                    }
                }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);

        string folderPath = ("Assets/" + subFolder).Replace("\\", "/");
        EnsureFolder(folderPath);

        string sceneName = SceneManager.GetActiveScene().name;
        string fileName = string.IsNullOrWhiteSpace(fileNameOverride) ? $"{sceneName}_tilemap.png" : fileNameOverride;
        string assetPath = (folderPath + "/" + fileName).Replace("\\", "/");

        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        // Optional metadata creation moved to runtime class file (no editor-only using issues)
        var meta = ScriptableObject.CreateInstance<TilemapMapMetadata>();
        meta.sceneName = sceneName;
        meta.cellOrigin = new Vector2Int(combined.xMin, combined.yMin);
        meta.cellSize = new Vector2Int(cellsX, cellsY);
        meta.pixelsPerCell = pixelsPerCell;
        string metaPath = (folderPath + "/" + Path.GetFileNameWithoutExtension(fileName) + "_Meta.asset").Replace("\\", "/");
        AssetDatabase.CreateAsset(meta, metaPath);
        AssetDatabase.SaveAssets();

        DestroyImmediate(tex);
        EditorUtility.DisplayDialog("Tilemap Map Baker", $"Saved:\n{assetPath}\n{metaPath}", "OK");
    }

    static void EnsureFolder(string fullAssetsPath)
    {
        if (AssetDatabase.IsValidFolder(fullAssetsPath)) return;
        string[] parts = fullAssetsPath.Split('/');
        string cur = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    static Color32 To32(Color c) => new Color32(
        (byte)Mathf.RoundToInt(c.r * 255f),
        (byte)Mathf.RoundToInt(c.g * 255f),
        (byte)Mathf.RoundToInt(c.b * 255f),
        (byte)Mathf.RoundToInt(c.a * 255f)
    );
}
#endif
