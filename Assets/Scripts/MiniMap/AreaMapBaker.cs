#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class AreaMapBaker : EditorWindow
{
    [Header("What to capture")]
    public LayerMask mapLayer = 0;                // set to your "Map" layer
    public bool includeInactive = true;

    [Header("Framing")]
    public float paddingWorldUnits = 1f;          // a little border around the bounds
    public float cameraZ = -50f;                  // where to put the temp camera

    [Header("Resolution")]
    public float pixelsPerWorldUnit = 32f;        // 32–128 is typical
    public int maxSize = 4096;                    // clamp huge maps

    [Header("Background")]
    public Color clearColor = Color.black;        // map background color

    [Header("Output")]
    public string subFolder = "Maps";             // saved under Assets/Maps
    public string fileName = "";                  // leave blank = SceneName_map.png

    [MenuItem("Tools/Area Map/Bake Current Scene Map...")]
    static void Open()
    {
        GetWindow<AreaMapBaker>("Area Map Baker").Show();
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Bake a top-down PNG of everything rendered on the selected layer(s).", MessageType.Info);
        mapLayer = EditorGUILayout.MaskField("Map Layer(s)", mapLayer, InternalEditorUtility.layers)
            .ToLayerMask();

        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        paddingWorldUnits = EditorGUILayout.FloatField("Padding (World Units)", paddingWorldUnits);
        cameraZ = EditorGUILayout.FloatField("Camera Z", cameraZ);

        pixelsPerWorldUnit = EditorGUILayout.FloatField("Pixels Per World Unit", pixelsPerWorldUnit);
        maxSize = EditorGUILayout.IntField("Max Texture Size", maxSize);

        clearColor = EditorGUILayout.ColorField("Background Color", clearColor);

        subFolder = EditorGUILayout.TextField("Subfolder (under Assets)", subFolder);
        fileName = EditorGUILayout.TextField("File Name (optional)", fileName);

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake Map PNG"))
        {
            Bake();
        }
    }

    void Bake()
    {
        // 1) Gather renderers on the chosen layers (2D/3D)
        var allRenderers = FindObjectsOfType<Renderer>(includeInactive)
            .Where(r => ((1 << r.gameObject.layer) & mapLayer.value) != 0)
            .ToArray();

        if (allRenderers.Length == 0)
        {
            EditorUtility.DisplayDialog("Area Map Baker", "No renderers found on the selected Map layers.", "OK");
            return;
        }

        // 2) Compute world-space bounds
        var bounds = allRenderers[0].bounds;
        foreach (var r in allRenderers) bounds.Encapsulate(r.bounds);
        bounds.Expand(paddingWorldUnits * 2f);

        // 3) Set up temp camera
        var go = new GameObject("__MapBakeCamera__");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = clearColor;
        cam.cullingMask = mapLayer.value;
        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, cameraZ);

        // orthographic size fits height; aspect fits width
        float worldWidth = Mathf.Max(bounds.size.x, 0.001f);
        float worldHeight = Mathf.Max(bounds.size.y, 0.001f);

        // 4) Pick output size from world size
        int targetWidth = Mathf.Clamp(Mathf.CeilToInt(worldWidth * pixelsPerWorldUnit), 8, maxSize);
        int targetHeight = Mathf.Clamp(Mathf.CeilToInt(worldHeight * pixelsPerWorldUnit), 8, maxSize);

        // Maintain aspect: camera size is half of world height
        float aspect = (float)targetWidth / targetHeight;
        cam.aspect = aspect;
        cam.orthographicSize = worldHeight * 0.5f;

        // If width would be cropped, increase ortho size so width fits too
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * aspect;
        if (halfWidth * 2f < worldWidth)
        {
            cam.orthographicSize = worldWidth * 0.5f / aspect;
            halfHeight = cam.orthographicSize;
            halfWidth = halfHeight * aspect;
        }

        // 5) Render to RT and read back
        var rt = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        var prevActive = RenderTexture.active;
        var prevTarget = cam.targetTexture;
        cam.targetTexture = rt;

        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false, false);
        tex.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        tex.Apply();

        // 6) Save PNG into Assets/subFolder
        string folderPath = Path.Combine("Assets", subFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parent = "Assets";
            foreach (var part in subFolder.Split('/'))
            {
                if (string.IsNullOrEmpty(part)) continue;
                string candidate = Path.Combine(parent, part).Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(candidate))
                    AssetDatabase.CreateFolder(parent, part);
                parent = candidate;
            }
            folderPath = Path.Combine("Assets", subFolder).Replace("\\", "/");
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string finalName = string.IsNullOrWhiteSpace(fileName) ? $"{sceneName}_map.png" : fileName;
        string assetPath = Path.Combine(folderPath, finalName).Replace("\\", "/");

        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(assetPath, bytes);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        // 7) Cleanup
        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        rt.Release();
        DestroyImmediate(rt);
        DestroyImmediate(go);
        DestroyImmediate(tex);

        EditorUtility.DisplayDialog("Area Map Baker", $"Saved map PNG:\n{assetPath}", "OK");
    }
}

static class LayerMaskExtensions
{
    public static LayerMask ToLayerMask(this int maskField, int firstLayer = 0)
    {
        // Unity’s MaskField returns an index-set based on InternalEditorUtility.layers order,
        // but we only need the raw value here; returning as-is works for this usage.
        return (LayerMask)maskField;
    }
}
#endif
