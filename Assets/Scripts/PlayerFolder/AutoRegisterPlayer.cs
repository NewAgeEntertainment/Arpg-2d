// Attach on Player prefab
using UnityEngine;

public class AutoRegisterPlayer : MonoBehaviour
{
    private void Awake()
    {
        var p = GetComponent<Player>();
        if (p != null) GameManager.Instance?.RegisterPlayer(p);
    }
}
