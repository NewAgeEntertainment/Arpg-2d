using UnityEngine;
public class Ping2D : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Ping2D] {name} got enter from {other.name}");
    }
}
