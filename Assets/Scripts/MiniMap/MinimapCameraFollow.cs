using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapCameraFollow : MonoBehaviour
{
    public Transform target;
    public MinimapSettings settings;

    [Header("Framing")]
    public float followLerp = 20f;
    public float orthoSizeMinimap = 8f;
    public float orthoSizeFullscreen = 35f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam.orthographic == false) _cam.orthographic = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, target.position.x, Time.deltaTime * followLerp);
        pos.y = Mathf.Lerp(pos.y, target.position.y, Time.deltaTime * followLerp);
        transform.position = pos;

        // Optional clamp inside world bounds (keep camera within)
        if (settings != null)
        {
            float half = _cam.orthographicSize;
            // assume square pixel aspect; for more precision, account for viewport aspect
            pos.x = Mathf.Clamp(pos.x, settings.worldMin.x + half, settings.worldMax.x - half);
            pos.y = Mathf.Clamp(pos.y, settings.worldMin.y + half, settings.worldMax.y - half);
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }
    }


    public void SetFullscreen(bool full)
    {
        _cam.orthographicSize = full ? orthoSizeFullscreen : orthoSizeMinimap;
    }
}
