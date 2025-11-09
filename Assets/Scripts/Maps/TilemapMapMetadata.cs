using UnityEngine;

[CreateAssetMenu(fileName = "TilemapMapMetadata", menuName = "Maps/Tilemap Map Metadata")]
public class TilemapMapMetadata : ScriptableObject
{
    public string sceneName;
    public Vector2Int cellOrigin;   // bottom-left cell in baked image
    public Vector2Int cellSize;     // width/height in cells
    public int pixelsPerCell;       // pixels per cell used for bake

    // Convert world -> pixel coordinates in the baked map
    public Vector2 WorldToMapPixels(Vector3 worldPos, Grid grid)
    {
        if (!grid) return Vector2.zero;
        var cell = grid.WorldToCell(worldPos);
        int cx = cell.x - cellOrigin.x;
        int cy = cell.y - cellOrigin.y;
        float px = (cx + 0.5f) * pixelsPerCell;
        float py = (cy + 0.5f) * pixelsPerCell;
        return new Vector2(px, py);
    }

    // Convert world -> normalized UV (0..1)
    public Vector2 WorldToMapUV(Vector3 worldPos, Grid grid)
    {
        var p = WorldToMapPixels(worldPos, grid);
        float w = Mathf.Max(1, cellSize.x * pixelsPerCell);
        float h = Mathf.Max(1, cellSize.y * pixelsPerCell);
        return new Vector2(p.x / w, p.y / h);
    }
}
