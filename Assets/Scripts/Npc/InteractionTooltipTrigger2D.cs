// InteractionTooltipTrigger2D.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PixelCrushers.DialogueSystem; // OK if you use Dialogue System; harmless otherwise
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class InteractionTooltipTrigger2D : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Tag that can trigger this (usually 'Player').")]
    public string playerTag = "Player";

    [Tooltip("Optional: other tags that may trigger (companions, etc.).")]
    public List<string> extraAllowedTags = new List<string>();

    [Header("Tooltip")]
    [Tooltip("Root GameObject of the tooltip (usually a child Canvas).")]
    public GameObject tooltipRoot;

    [Tooltip("TMP text inside tooltipRoot that will display the message.")]
    public TextMeshProUGUI messageText;

    [Tooltip("Fallback message if no Usable message is available.")]
    public string fallbackMessage = "Press Interact";

    [Tooltip("Use {name} and {msg} placeholders if you want formatting.")]
    public string format = "{msg}";

    [Header("Positioning")]
    [Tooltip("World offset for the tooltip relative to this object.")]
    public Vector3 worldOffset = new Vector3(0f, 1.1f, 0f);

    [Tooltip("If true, the tooltip will face the main camera.")]
    public bool billboardToCamera = true;

    [Tooltip("Keep the tooltip pinned/updated every frame while visible.")]
    public bool autoRepositionEachFrame = true;

    [Header("Debug")]
    [SerializeField] private bool logTextChanges = false;

    // Optional Dialogue System integration (won't error if not present in scene)
    private Usable usable;

    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
    private Camera cam;
    private Collider2D trig;
    private string _lastLogged;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    void Awake()
    {
        cam = Camera.main;
        trig = GetComponent<Collider2D>();
        if (trig && !trig.isTrigger)
        {
            Debug.LogWarning($"[{name}] Collider2D was not trigger; setting isTrigger = true.");
            trig.isTrigger = true;
        }

        usable = GetComponent<Usable>();

        // Respect explicit assignments; only auto-find if null:
        if (!tooltipRoot)
        {
            var canv = GetComponentInChildren<Canvas>(true);
            tooltipRoot = canv ? canv.gameObject : null;
        }

        if (!messageText && tooltipRoot)
        {
            // Find a single TMP under the tooltip root only
            messageText = tooltipRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (!tooltipRoot) Debug.LogWarning($"[{name}] tooltipRoot is not assigned.");
        if (!messageText) Debug.LogWarning($"[{name}] messageText is not assigned or not found under tooltipRoot.");

        SetVisible(false);
    }

    void LateUpdate()
    {
        if (tooltipRoot && tooltipRoot.activeSelf && autoRepositionEachFrame)
        {
            tooltipRoot.transform.position = transform.position + worldOffset;

            if (billboardToCamera && cam)
            {
                var fwd = cam.transform.forward;
                if (fwd.sqrMagnitude > 0.0001f)
                    tooltipRoot.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }

            if (logTextChanges && messageText && _lastLogged != messageText.text)
            {
                _lastLogged = messageText.text;
                Debug.Log($"[{name}] Tooltip text -> '{_lastLogged}'", messageText);
            }
        }
    }

    // ---------- Trigger detection ----------
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsAllowed(other)) return;

        if (occupants.Add(other))
        {
            string uName = GetUsableName();
            string uMsg = GetUsableMessage();
            string final = format.Replace("{name}", uName).Replace("{msg}", uMsg);

            Reposition();
            ShowWithText(final);

            // Optional: tell Dialogue System a selection happened
            if (usable)
                usable.gameObject.SendMessage("OnSelect", other.transform, SendMessageOptions.DontRequireReceiver);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!occupants.Remove(other)) return;

        if (occupants.Count == 0)
        {
            SetVisible(false);

            // Optional: Dialogue System deselect
            if (usable)
                usable.gameObject.SendMessage("OnDeselect", SendMessageOptions.DontRequireReceiver);
        }
    }

    // Called by your Player when pressing Interact (see Player code below)
    public void Interact(Transform actor)
    {
        if (occupants.Count == 0) return;

        // Forward to Dialogue System if present
        if (usable)
            usable.gameObject.SendMessage("OnUse", actor, SendMessageOptions.DontRequireReceiver);

        // Hide tooltip on use (typical UX)
        SetVisible(false);
    }

    // ---------- Helpers ----------
    private bool IsAllowed(Collider2D other)
    {
        if (other.CompareTag(playerTag)) return true;
        for (int i = 0; i < extraAllowedTags.Count; i++)
            if (other.CompareTag(extraAllowedTags[i])) return true;
        return false;
    }

    private void Reposition()
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

    private void ShowWithText(string txt)
    {
        if (!tooltipRoot || !messageText) return;

        // 1) Enable first so TMP can build a mesh
        if (!tooltipRoot.activeSelf) tooltipRoot.SetActive(true);

        // 2) Assign text
        messageText.enabled = true;
        messageText.richText = true;
        messageText.text = txt ?? "";

        // 3) Force UI rebuild to prevent "text disappears on first frame"
        messageText.ForceMeshUpdate(true, true);
        Canvas.ForceUpdateCanvases();

        var rt = messageText.transform as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        var prt = tooltipRoot.transform as RectTransform;
        if (prt) LayoutRebuilder.ForceRebuildLayoutImmediate(prt);

        // 4) Safety: ensure no hidden alpha/scale
        var cg = tooltipRoot.GetComponentInParent<CanvasGroup>();
        if (cg) cg.alpha = 1f;
        tooltipRoot.transform.localScale = Vector3.one;
    }

    private void SetVisible(bool v)
    {
        if (tooltipRoot) tooltipRoot.SetActive(v);
    }

    private string GetUsableName()
    {
        if (!usable) return gameObject.name;
        var t = usable.GetType();

        // Try field/property 'name' on Usable, then method GetName()
        var prop = t.GetProperty("name", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(string)) return (string)prop.GetValue(usable, null);

        var field = t.GetField("name", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(string)) return (string)field.GetValue(usable);

        var m = t.GetMethod("GetName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
        if (m != null && m.ReturnType == typeof(string)) return (string)m.Invoke(usable, null);

        return gameObject.name;
    }

    private string GetUsableMessage()
    {
        if (!usable) return fallbackMessage;
        var t = usable.GetType();

        // Try fields/properties 'useMessage' or 'overrideUseMessage', then GetUseMessage()
        var prop = t.GetProperty("useMessage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(string))
        {
            string s = (string)prop.GetValue(usable, null);
            if (!string.IsNullOrEmpty(s)) return s;
        }
        var prop2 = t.GetProperty("overrideUseMessage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (prop2 != null && prop2.PropertyType == typeof(string))
        {
            string s = (string)prop2.GetValue(usable, null);
            if (!string.IsNullOrEmpty(s)) return s;
        }

        var field = t.GetField("useMessage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(string))
        {
            string s = (string)field.GetValue(usable);
            if (!string.IsNullOrEmpty(s)) return s;
        }
        var field2 = t.GetField("overrideUseMessage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field2 != null && field2.FieldType == typeof(string))
        {
            string s = (string)field2.GetValue(usable);
            if (!string.IsNullOrEmpty(s)) return s;
        }

        var m = t.GetMethod("GetUseMessage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
        if (m != null && m.ReturnType == typeof(string))
        {
            string s = (string)m.Invoke(usable, null);
            if (!string.IsNullOrEmpty(s)) return s;
        }

        return fallbackMessage;
    }
}
