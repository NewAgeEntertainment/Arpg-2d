using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GrassVolume : MonoBehaviour
{
    private void Reset()
    {
        // Ensure we truly are a trigger on the composite
        var comp = GetComponent<CompositeCollider2D>();
        if (comp) comp.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<Player>();
        if (p != null) p.SetInGrass(true);
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        var p = other.GetComponentInParent<Player>();
        if (p != null) p.SetInGrass(true);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        var p = other.GetComponentInParent<Player>();
        if (p != null) p.SetInGrass(false);
    }
}
