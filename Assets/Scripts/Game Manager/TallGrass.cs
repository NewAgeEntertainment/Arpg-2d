// TallGrass.cs
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(BoxCollider2D))]
public class TallGrass : MonoBehaviour
{
    [Tooltip("Feet sensor tag to react only to player's feet.")]
    public string playerFeetTag = "PlayerFeet";

    Animator anim;
    int insideCount;

    void Awake() { anim = GetComponent<Animator>(); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerFeetTag)) return;
        insideCount++;
        anim.SetBool("occupied", true);
        anim.SetTrigger("rustle");
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerFeetTag)) return;
        var rb = other.attachedRigidbody;
        float spd = (rb ? rb.velocity.magnitude : 0f);
        anim.SetFloat("speed", spd);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerFeetTag)) return;
        insideCount = Mathf.Max(0, insideCount - 1);
        if (insideCount == 0) anim.SetBool("occupied", false);
    }
}
