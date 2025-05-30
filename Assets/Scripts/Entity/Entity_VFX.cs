using System.Collections;
using UnityEngine;

public class Entity_VFX : MonoBehaviour
{

    private SpriteRenderer sr;
    private Entity entity;

    [Header("On Damage VFX")]
    [SerializeField] private Material onDamageMaterial;
    [SerializeField] private float onDamageVFXDuration = .1f;
    private Material originalMaterial;
    private Coroutine onDamageVfxCoroutine;

    [Header("On Doing Damage VFX")]
    [SerializeField] private Color hitVfxColor = Color.white;
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private GameObject critHitVfx;

    [Header("Element Colors")]
    [SerializeField] private Color chillVfx = Color.cyan;
    private Color originalHitVfxColor;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        sr = GetComponentInChildren<SpriteRenderer>();
        originalMaterial = sr.material;
        originalHitVfxColor = hitVfxColor;
    }

    private IEnumerator PlayStatusVfxco(float duration, Color effectColor)
    {
        float tickInterval = .25f; // How often the effect should update
        float timeHasPassed = 0f; // Timer to track the duration of the effect

        Color lightColor = effectColor * 1.2f; // A lighter version of the effect color for the effect
        Color darkColor = effectColor * .8f; // A darker version of the effect color for the effect

        bool toggle = false; // Toggle to switch between light and dark colors
    
        while (timeHasPassed < duration)
        {
            sr.color = toggle ? lightColor : darkColor; // Set the sprite renderer color to the current color
            toggle = !toggle; // Toggle the color for the next iteration

            yield return new WaitForSeconds(tickInterval); // Wait for the specified tick interval

        }
    
    }

    public void CreateOnHitVFX(Transform target,bool isCrit)
    {
        GameObject hitPrefeb = isCrit ? critHitVfx : hitVfx;
        GameObject vfx = Instantiate(hitPrefeb, target.position, Quaternion.identity);
        vfx.GetComponentInChildren<SpriteRenderer>().color = hitVfxColor; // set the color of the hit effect to the specified hitVfxColor

        if (entity.currentDir.x < -1 && isCrit)
        {
            vfx.transform.Rotate(0,180,0);
        }
        else if (entity.currentDir.x > 1 && isCrit)
        {
            vfx.transform.Rotate(0, 0, 0);
        }

        if (entity.currentDir.y < -1)
        {
            vfx.transform.Rotate(0, 0, -90);
        }
        else if (entity.currentDir.y > 1)
        {
            vfx.transform.Rotate(0, 0, -270);
        }

    }

    public void UpdateOnHitColor(ElementType element)
    {
        if (element == ElementType.Ice)
            hitVfxColor = chillVfx;

        if (element == ElementType.None)
            hitVfxColor = originalHitVfxColor;
    }

    public void PlayOnDamageVfx()
    {
        if (onDamageVfxCoroutine != null)
            StopCoroutine(onDamageVfxCoroutine);
        
        
        onDamageVfxCoroutine = StartCoroutine(OnDamageVfxCo());


    }

    private IEnumerator OnDamageVfxCo()
    {
        sr.material = onDamageMaterial;
        yield return new WaitForSeconds(onDamageVFXDuration);
        sr.material = originalMaterial;
    }
}
