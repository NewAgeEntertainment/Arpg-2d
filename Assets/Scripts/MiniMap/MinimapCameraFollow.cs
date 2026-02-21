using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapCameraFollow : MonoBehaviour
{
    public Transform target;
    public MinimapSettings settings;

    [Header("Framing")]
    public float followLerp = 20f;

    [Tooltip("Minimap orthographic size (no fullscreen mode).")]
    public float orthoSizeMinimap = 8f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam.orthographic) _cam.orthographic = true;

        // Always minimap size:
        _cam.orthographicSize = orthoSizeMinimap;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Always enforce minimap size (prevents any other script from changing it):
        if (!Mathf.Approximately(_cam.orthographicSize, orthoSizeMinimap))
            _cam.orthographicSize = orthoSizeMinimap;

        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, target.position.x, Time.deltaTime * followLerp);
        pos.y = Mathf.Lerp(pos.y, target.position.y, Time.deltaTime * followLerp);
        transform.position = pos;

        // Optional clamp inside world bounds
        if (settings != null)
        {
            float half = _cam.orthographicSize;
            pos.x = Mathf.Clamp(pos.x, settings.worldMin.x + half, settings.worldMax.x - half);
            pos.y = Mathf.Clamp(pos.y, settings.worldMin.y + half, settings.worldMax.y - half);
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }
    }

    // Removed: SetFullscreen(bool)
}
