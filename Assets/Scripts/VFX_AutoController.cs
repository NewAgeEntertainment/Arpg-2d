using UnityEngine;

public class VFX_AutoController : MonoBehaviour
{
    [SerializeField] private bool autoDestroy = true; // Automatically destroy the GameObject after the effect is done
    [SerializeField] private float autoDestroyDelay = 1f; // Delay before the GameObject is destroyed
    [Space]
    [SerializeField] private bool randomOffset = true;
    [SerializeField] private bool randomRotation = true;

    [Header("Random Rotation")]
    [SerializeField] private float minRotation = 0f; // Minimum rotation angle
    [SerializeField] private float maxRotation = 360f; // Maximum rotation angle

    [Header("Random Position")]
    [SerializeField] private float xMinoffset = -.3f;
    [SerializeField] private float xMaxoffset = .3f;
    [Space]
    [SerializeField] private float yMinOffset = -.3f;
    [SerializeField] private float yMaxOffset = .3f;
   
    
    private void Start()
    {

        ApplyRandomOffset();
        ApplyRandomRotation();

        if (autoDestroy)
            Destroy(gameObject, autoDestroyDelay);
        
    }


    private void ApplyRandomOffset()
    {
        if (randomOffset == false)
            return;

        float xoffset = Random.Range(xMinoffset, xMaxoffset);
        float yoffset = Random.Range(yMinOffset, yMaxOffset);

        transform.position = transform.position + new Vector3(xoffset, yoffset);
    }

    private void ApplyRandomRotation()
    {
        if(randomRotation == false)
            return;

        float zRotation = Random.Range(minRotation, maxRotation);
        transform.Rotate(0, 0, zRotation);
    }

}
