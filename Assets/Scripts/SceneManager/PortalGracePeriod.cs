using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DefaultExecutionOrder(-50)]
public class PortalGracePeriod : MonoBehaviour
{
    [Tooltip("How long to ignore portal triggers right after a scene load/teleport.")]
    public float graceSeconds = 0.35f;

    Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col == null) _col = GetComponentInChildren<Collider>();
        if (_col != null && !_col.isTrigger)
            Debug.LogWarning($"{name}: Portal collider should be a trigger.");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnEnable()
    {
        // Also guard when re-enabling via teleport scripts.
        StartCoroutine(DisableForSeconds(graceSeconds));
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DisableForSeconds(graceSeconds));
    }

    IEnumerator DisableForSeconds(float seconds)
    {
        if (_col != null)
        {
            _col.enabled = false;
            yield return null; // let physics settle a frame
            yield return new WaitForSeconds(seconds);
            _col.enabled = true;
        }
    }
}
