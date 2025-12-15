using UnityEngine;

public class Entity_SFX : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("SFX Names")]
    [SerializeField] private string attackHit;
    [SerializeField] private string attackMiss;
    [Space]
    [SerializeField] private float soundDistance = 15f;
    [SerializeField] private bool showGizmo;

    private void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogWarning($"[Entity_SFX] No AudioSource found on {name} or its children. " +
                             "Attack SFX will be muted.");
        }
    }

    public void PlayAttackHit()
    {
        if (AudioManager.instance == null)
        {
            Debug.LogWarning("[Entity_SFX] AudioManager.instance is NULL, cannot play attackHit.");
            return;
        }

        if (audioSource == null)
        {
            // already warned in Awake, but guard again so we don’t crash
            return;
        }

        if (string.IsNullOrEmpty(attackHit))
        {
            Debug.LogWarning("[Entity_SFX] attackHit clip name is empty.");
            return;
        }

        AudioManager.instance.PlaySFX(attackHit, audioSource, soundDistance);
    }

    public void PlayAttackMiss()
    {
        if (AudioManager.instance == null)
        {
            Debug.LogWarning("[Entity_SFX] AudioManager.instance is NULL, cannot play attackMiss.");
            return;
        }

        if (audioSource == null) return;
        if (string.IsNullOrEmpty(attackMiss))
        {
            Debug.LogWarning("[Entity_SFX] attackMiss clip name is empty.");
            return;
        }

        AudioManager.instance.PlaySFX(attackMiss, audioSource, soundDistance);
    }

    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, soundDistance);
        }
    }
}
