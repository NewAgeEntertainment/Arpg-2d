using UnityEngine;
using System;

[DefaultExecutionOrder(-500)] // Make sure this announces early each scene
public class PlayerLocator : MonoBehaviour
{
    /// <summary>The current, canonical Player transform.</summary>
    public static Transform Current { get; private set; }

    /// <summary>Raised whenever the current player reference changes.</summary>
    public static event Action<Transform> OnChanged;

    [Tooltip("If enabled, marks this GameObject DontDestroyOnLoad in Awake().")]
    public bool makeDDOL = false;

    private void Awake()
    {
        if (makeDDOL) DontDestroyOnLoad(gameObject);
        Set(transform);
    }

    private void OnEnable() => Set(transform);
    private void OnDisable() => Clear(transform);
    private void OnDestroy() => Clear(transform);

    private static void Set(Transform t)
    {
        Current = t;
        OnChanged?.Invoke(Current);
    }

    private static void Clear(Transform t)
    {
        if (Current == t)
        {
            Current = null;
            OnChanged?.Invoke(null);
        }
    }
}
