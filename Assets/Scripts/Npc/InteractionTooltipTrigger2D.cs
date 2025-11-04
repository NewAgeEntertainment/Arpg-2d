// InteractionTooltipTrigger2D.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PixelCrushers.DialogueSystem; // OK if you use Dialogue System; harmless otherwise
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class InteractionTooltipTrigger2D : MonoBehaviour
{
    // ------------------ UnityEvents fallbacks (work w/o Usable) ------------------
    [System.Serializable] public class TransformEvent : UnityEvent<Transform> { }

    [Header("Fallback Events (used when no Usable is present)")]
    public TransformEvent onSelect;   // fired with actor Transform
    public UnityEvent onDeselect; // fired with no args
    public TransformEvent onUse;      // fired with actor Transform

    // ------------------ Detection ------------------
    [Header("Detection")]
    [Tooltip("Tag that can trigger this (usually 'Player').")]
    public string playerTag = "Player";

    [Tooltip("Optional: other tags that may trigger (companions, etc.).")]
    public List<string> extraAllowedTags = new List<string>();

    // ------------------ Tooltip ------------------
    [Header("Tooltip")]
    [Tooltip("Root GameObject of the tooltip (usually a child Canvas).")]
    public GameObject tooltipRoot;

    [Tooltip("TMP text inside tooltipRoot that will display the message.")]
    public TextMeshProUGUI messageText;

    [Tooltip("Fallback message if no Usable message is available.")]
    public string fallbackMessage = "Press Interact";

    [Tooltip("Use {name} and {msg} placeholders if you want formatting.")]
    public string format = "{msg}";

    // ------------------ Positioning ------------------
    [Header("Positioning")]
    [Tooltip("World offset for the tooltip relative to this object.")]
    public Vector3 worldOffset = new Vector3(0f, 1.1f, 0f);

    [Tooltip("If true, the tooltip will face the main camera.")]
    public bool billboardToCamera = true;

    [Tooltip("Keep the tooltip pinned/updated every frame while visible.")]
    public bool autoRepositionEachFrame = true;

    // ------------------ Usable integration toggles ------------------
    [Header("Usable Integration")]
    [Tooltip("If true and a Usable is present, call OnSelect/OnDeselect/OnUse on it.")]
    public bool useUsableIntegration = true;

    [Tooltip("When true, OnTriggerEnter2D will call Usable.Select (via SendMessage).")]
    public bool callSelectOnEnter = true;

    [Tooltip("When true, OnTriggerExit2D will call Usable.Deselect (via SendMessage).")]
    public bool callDeselectOnExit = true;

    [Tooltip("When true, Interact() will call Usable.OnUse (via SendMessage).")]
    public bool callOnUse = true;

    // ------------------ Debug ------------------
    [Header("Debug")]
    [SerializeField] private bool logTextChanges = false;
    [SerializeField] private bool verboseLogs = false;

    // Optional Dialogue System integration (won't error if not present in scene)
    private Usable usable;

    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
    private readonly List<Collider2D> scratchList = new List<Collider2D>(8);
    private Transform _currentActor;                  // most-recent valid actor inside
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

        // Dialogue System: Usable is optional
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
            // Set/refresh current actor
            _currentActor = other.transform;

            string uName = GetUsableName();
            string uMsg = GetUsableMessage();
            string final = format.Replace("{name}", uName).Replace("{msg}", uMsg);

            Reposition();
            ShowWithText(final);

            // Integration path: Usable or fallback event
            if (useUsableIntegration && usable && callSelectOnEnter)
            {
                if (verboseLogs) Debug.Log($"[{name}] OnSelect -> Usable ({_currentActor?.name})");
                usable.gameObject.SendMessage("OnSelect", _currentActor, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                if (verboseLogs) Debug.Log($"[{name}] onSelect.Invoke ({_currentActor?.name})");
                onSelect?.Invoke(_currentActor);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!occupants.Remove(other)) return;

        // If the actor leaving was our current actor, pick another still inside (if any)
        if (_currentActor == other.transform)
        {
            _currentActor = FindAnyActorStillInside();
        }

        if (occupants.Count == 0)
        {
            SetVisible(false);

            // Integration path: Usable or fallback event
            if (useUsableIntegration && usable && callDeselectOnExit)
            {
                if (verboseLogs) Debug.Log($"[{name}] OnDeselect -> Usable");
                usable.gameObject.SendMessage("OnDeselect", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                if (verboseLogs) Debug.Log($"[{name}] onDeselect.Invoke()");
                onDeselect?.Invoke();
            }
        }
        else
        {
            // Still at least one actor in; keep tooltip up and keep current actor reference
            if (verboseLogs) Debug.Log($"[{name}] Occupant left, {occupants.Count} remain. CurrentActor={_currentActor?.name}");
        }
    }

    // Called by your Player when pressing Interact (see Player code below)
    public void Interact(Transform actor)
    {
        if (occupants.Count == 0) return;

        // Prefer the passed-in actor (from player controller), else use last known
        var effectiveActor = actor ? actor : (_currentActor ? _currentActor : null);

        if (useUsableIntegration && usable && callOnUse)
        {
            if (verboseLogs) Debug.Log($"[{name}] OnUse -> Usable ({effectiveActor?.name})");
            usable.gameObject.SendMessage("OnUse", effectiveActor, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            if (verboseLogs) Debug.Log($"[{name}] onUse.Invoke ({effectiveActor?.name})");
            onUse?.Invoke(effectiveActor);
        }

        // Typical UX: hide after use (optional)
        SetVisible(false);
    }

    // ---------- Helpers ----------
    private bool IsAllowed(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            // Player player = other.GetComponent<Player>();
            return true;
        }

        for (int i = 0; i < extraAllowedTags.Count; i++)
        {
            if (other.CompareTag(extraAllowedTags[i])) return true;
        }

        return false;
    }

    private Transform FindAnyActorStillInside()
    {
        // Rebuild scratch list from occupants HashSet (HashSet has no indexer)
        scratchList.Clear();
        foreach (var c in occupants) if (c) scratchList.Add(c);
        return scratchList.Count > 0 ? scratchList[0].transform : null;
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
