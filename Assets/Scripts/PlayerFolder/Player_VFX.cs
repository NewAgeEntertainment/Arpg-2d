using System.Collections;
using UnityEngine;

public class Player_VFX : Entity_VFX
{
    // ==================== Image Echo (yours, unchanged) ====================
    [Header("Image Echo VFX")]
    [Range(.01f, .2f)]
    [SerializeField] private float imageEchoInterval = .05f;
    [SerializeField] private GameObject imageEchoPrefeb;
    private Coroutine imageEchoCo;

    public void CreateEffectOf(GameObject effect, Transform target)
    {
        if (!effect || !target) return;
        Instantiate(effect, target.position, Quaternion.identity);
    }

    public void DoImageEchoEffect(float duration)
    {
        if (imageEchoCo != null)
            StopCoroutine(imageEchoCo);

        imageEchoCo = StartCoroutine(ImageEchoEffectCo(duration));
    }

    private IEnumerator ImageEchoEffectCo(float duration)
    {
        float timeTracker = 0f;
        while (timeTracker < duration)
        {
            CreateImageEcho();
            yield return new WaitForSeconds(imageEchoInterval);
            timeTracker += imageEchoInterval;
        }
        imageEchoCo = null;
    }

    private void CreateImageEcho()
    {
        if (!imageEchoPrefeb) return;
        var imageEcho = Instantiate(imageEchoPrefeb, transform.position, transform.rotation);

        // 'sr' comes from Entity_VFX (base). If it's null, try to grab one.
        var sourceSr = sr != null ? sr : GetComponentInChildren<SpriteRenderer>();
        if (imageEcho && sourceSr)
        {
            var childSr = imageEcho.GetComponentInChildren<SpriteRenderer>();
            if (childSr) childSr.sprite = sourceSr.sprite;
        }
    }
    // ======================================================================


    // ==================== Grass VFX/SFX (added) ===========================
    [Header("Grass VFX/SFX (optional)")]
    [Tooltip("One-shot when first entering grass.")]
    public ParticleSystem grassEnterFX;
    [Tooltip("Loop while in grass (will be stopped on exit).")]
    public ParticleSystem grassLoopFX;
    [Tooltip("One-shot when exiting grass.")]
    public ParticleSystem grassExitFX;
    [Tooltip("Small burst used during footsteps while in grass.")]
    public ParticleSystem grassRustleFX;

    [Tooltip("Optional shared AudioSource for grass SFX.")]
    public AudioSource sfx;
    public AudioClip grassEnterSfx;
    public AudioClip grassExitSfx;
    public AudioClip grassRustleSfx;

    /// <summary>
    /// Play a named effect if available.
    /// Supported ids: "Grass_Enter", "Grass_Exit", "GrassRustle".
    /// Returns true if something played.
    /// </summary>
    public bool TryPlay(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        switch (id)
        {
            case "Grass_Enter":
                if (grassEnterFX) grassEnterFX.Play();
                if (grassLoopFX && !grassLoopFX.isPlaying) grassLoopFX.Play();
                if (sfx && grassEnterSfx) sfx.PlayOneShot(grassEnterSfx);
                return (grassEnterFX || grassLoopFX || (sfx && grassEnterSfx));

            case "Grass_Exit":
                if (grassExitFX) grassExitFX.Play();
                if (grassLoopFX && grassLoopFX.isPlaying)
                    grassLoopFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (sfx && grassExitSfx) sfx.PlayOneShot(grassExitSfx);
                return (grassExitFX || (sfx && grassExitSfx));

            case "GrassRustle":
                if (grassRustleFX) grassRustleFX.Play();
                if (sfx && grassRustleSfx) sfx.PlayOneShot(grassRustleSfx);
                return (grassRustleFX || (sfx && grassRustleSfx));
        }

        return false;
    }

    private void OnDisable()
    {
        // Make sure any looping grass FX are stopped if the object disables.
        if (grassLoopFX && grassLoopFX.isPlaying)
            grassLoopFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    // ======================================================================
}
