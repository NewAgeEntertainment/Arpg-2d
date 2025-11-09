using UnityEngine;

public class MinimapTarget : MonoBehaviour
{
    [Tooltip("Optional override color per target. Leave a=0 to keep prefab's color.")]
    public Color iconTint = new Color(0, 0, 0, 0);

    [Tooltip("Optional: provide a different sprite for this target.")]
    public Sprite iconSprite;

    [Tooltip("If true, this target will rotate the blip arrow to face its direction from center when offscreen.")]
    public bool rotateArrow = true;

    [Header("Visibility Rules")]
    [Tooltip("When ON, the blip shows only while this GameObject is activeInHierarchy and the component is enabled.")]
    public bool requireActiveInHierarchy = true;

    [HideInInspector] public RectTransform blipRect;   // set by controller
    [HideInInspector] public Transform arrowTransform; // set by controller
}
