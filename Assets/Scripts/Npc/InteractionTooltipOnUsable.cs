using UnityEngine;
using TMPro;
using PixelCrushers.DialogueSystem;

/// Attach to the SAME GameObject as Pixel Crushers 'Usable'.
/// Shows a per-object tooltip when ProximitySelector selects this usable.
[RequireComponent(typeof(Usable))]
public class InteractionTooltipOnUsable : MonoBehaviour
{
    [Header("Assign your tooltip (usually a child Canvas)")]
    public GameObject tooltipRoot;                 // Your tooltip object on THIS usable
    public TextMeshProUGUI messageText;            // Optional: message label inside tooltip

    [Header("Content")]
    [Tooltip("Text format. {name} = Usable name, {msg} = Usable use message")]
    public string format = "{msg}";
    [Tooltip("Fallback message if Usable has no Use Message.")]
    public string fallbackMessage = "Press Interact";

    [Header("Positioning")]
    public Vector3 worldOffset = new Vector3(0f, 1.1f, 0f); // offset above the object
    public bool billboardToCamera = true;                   // make it face camera (world-space)
    public bool autoRepositionEachFrame = true;             // keep pinned while selected

    Usable usable;
    Camera cam;

    void Awake()
    {
        usable = GetComponent<Usable>();
        cam = Camera.main;
        SetVisible(false);

        // If tooltip not assigned, try to find one on children:
        if (!tooltipRoot)
        {
            var rt = GetComponentInChildren<RectTransform>(true);
            if (rt) tooltipRoot = rt.gameObject;
        }
        if (!messageText && tooltipRoot)
        {
            messageText = tooltipRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    void LateUpdate()
    {
        // Keep the tooltip sitting above this object while it's visible
        if (tooltipRoot && tooltipRoot.activeSelf && autoRepositionEachFrame)
        {
            tooltipRoot.transform.position = transform.position + worldOffset;
            if (billboardToCamera && cam)
            {
                var fwd = cam.transform.forward;
                if (fwd.sqrMagnitude > 0.0001f)
                    tooltipRoot.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
        }
    }

    // -------- Messages sent by Dialogue System --------
    // ProximitySelector will call these on the Usable / attached components.

    // Called when the selector highlights this usable:
    public void OnSelect(Transform actor)
    {
        RefreshText();
        Reposition();
        SetVisible(true);
    }

    // Called when selector leaves or another gets selected:
    public void OnDeselect()
    {
        SetVisible(false);
    }

    // Called when the player uses/interacts:
    public void OnUse(Transform actor)
    {
        // Usually hide on use
        SetVisible(false);
    }

    // -------- Helpers --------

    void RefreshText()
    {
        if (!messageText) return;

        string uName = SafeGetString(usable, "name")
            ?? SafeCallString(usable, "GetName")
            ?? gameObject.name;

        string uMsg = SafeGetString(usable, "useMessage")
            ?? SafeGetString(usable, "overrideUseMessage")
            ?? SafeCallString(usable, "GetUseMessage")
            ?? fallbackMessage;

        messageText.text = format.Replace("{name}", uName).Replace("{msg}", uMsg);
    }

    void Reposition()
    {
        if (!tooltipRoot) return;
        tooltipRoot.transform.position = transform.position + worldOffset;

        if (billboardToCamera && cam)
        {
            var fwd = cam.transform.forward;
            if (fwd.sqrMagnitude > 0.0001f)
                tooltipRoot.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }
    }

    void SetVisible(bool v)
    {
        if (tooltipRoot) tooltipRoot.SetActive(v);
    }

    // Safely read strings across DS versions (field/prop/method)
    string SafeGetString(object obj, string fieldOrProp)
    {
        if (obj == null) return null;
        var t = obj.GetType();

        var prop = t.GetProperty(fieldOrProp,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(string))
            return prop.GetValue(obj, null) as string;

        var field = t.GetField(fieldOrProp,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(string))
            return field.GetValue(obj) as string;

        return null;
    }

    string SafeCallString(object obj, string method)
    {
        if (obj == null) return null;
        var m = obj.GetType().GetMethod(method,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, System.Type.EmptyTypes, null);
        if (m != null && m.ReturnType == typeof(string))
            return (string)m.Invoke(obj, null);
        return null;
    }
}
