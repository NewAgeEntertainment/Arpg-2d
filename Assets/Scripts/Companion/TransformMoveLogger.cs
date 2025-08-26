using UnityEngine;

public class TransformMoveLogger : MonoBehaviour
{
    Vector3 last;
    void OnEnable() { last = transform.position; Debug.Log($"[Probe] Enable @ {last}"); }
    void LateUpdate()
    {
        if ((transform.position - last).sqrMagnitude > 0.0001f)
        {
            Debug.Log($"[Probe] Moved to {transform.position}\n{System.Environment.StackTrace}");
            last = transform.position;
        }
    }
}
