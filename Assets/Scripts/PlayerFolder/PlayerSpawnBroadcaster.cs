using System;
using System.Collections;
using UnityEngine;

public class PlayerSpawnBroadcaster : MonoBehaviour
{
    public static event Action<Transform> OnPlayerSpawned;

    [Tooltip("Delay a frame or two so everything is initialized before we broadcast.")]
    public int framesToDelay = 1;

    IEnumerator Start()
    {
        for (int i = 0; i < framesToDelay; i++) yield return null;
        OnPlayerSpawned?.Invoke(transform);
    }

    void OnDestroy()
    {
        // Optional: notify despawn if useful in your game
        // OnPlayerSpawned?.Invoke(null);
    }
}
