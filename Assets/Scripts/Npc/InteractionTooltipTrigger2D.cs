using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
// (Dialogue System Usable is still optional)
using PixelCrushers.DialogueSystem;

[RequireComponent(typeof(Collider2D))]
public class InteractionTooltipTrigger2D : MonoBehaviour
{
    [System.Serializable] public class TransformEvent : UnityEvent<Transform> { }

    [Header("Fallback Events (used when no Usable is present)")]
    public TransformEvent onSelect;
    public UnityEvent onDeselect;
    public TransformEvent onUse;

    // -------- Detection --------
    [Header("Detection")]
    public string playerTag = "Player";
    public List<string> extraAllowedTags = new List<string>();

    // -------- Tooltip --------
    [Header("Tooltip")]
    public GameObject tooltipRoot;
    public TextMeshProUGUI messageText;
    public string fallbackMessage = "Press Interact";
    public string format = "{msg}";

    // -------- Positioning --------
    [Header("Positioning")]
    public Vector3 worldOffset = new Vector3(0f, 1.1f, 0f);
    public bool billboardToCamera = true;
    public bool autoRepositionEachFrame = true;

    // -------- Usable integration --------
    [Header("Usable Integration")]
    public bool useUsableIntegration = true;
    public bool callSelectOnEnter = true;
    public bool callDeselectOnExit = true;
    public bool callOnUse = true;

    // -------- Self-handled input (NO player script needed) --------
    [Header("Input (handled by this trigger)")]
    [Tooltip("If true, this trigger listens for the Interact key while the player is inside.")]
    public bool handleInput = true;
    [Tooltip("Fallback key if Rewired/other input isn’t available.")]
    public KeyCode fallbackKey = KeyCode.E;

    [Tooltip("Use Rewired if present. If false, only the fallback key is used.")]
    public bool useRewiredIfAvailable = true;
    [Tooltip("Rewired action name to trigger Interact.")]
    public string rewiredAction = "Interact";
    [Tooltip("Rewired player id (usually 0).")]
    public int rewiredPlayerId = 0;

    [Header("Direct Use Target (optional)")]
    public MonoBehaviour directUseTarget;

    // -------- Debug --------
    [Header("Debug")]
    [SerializeField] private bool logTextChanges = false;
    [SerializeField] private bool verboseLogs = false;

    private Usable usable;                   // optional (Dialogue System)
    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
    private readonly List<Collider2D> scratchList = new List<Collider2D>(8);
    private Transform _currentActor;
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
        if (trig && !trig.isTrigger) { trig.isTrigger = true; }

        usable = GetComponent<Usable>();

        if (!tooltipRoot)
        {
            var canv = GetComponentInChildren<Canvas>(true);
            tooltipRoot = canv ? canv.gameObject : null;
        }
        if (!messageText && tooltipRoot)
            messageText = tooltipRoot.GetComponentInChildren<TextMeshProUGUI>(true);

        SetVisible(false);
    }

    void Update()
    {
        // Let THIS component listen for Interact presses.
        if (!handleInput || occupants.Count == 0) return;

        bool pressed = Input.GetKeyDown(fallbackKey);

        // Safe Rewired path (only if assembly exists & is ready)
        if (!pressed && useRewiredIfAvailable)
        {
            try
            {
                // Avoid compile errors if Rewired isn't installed in some projects:
                var readyProp = System.Type.GetType("Rewired.ReInput, Rewired")
                                  ?.GetProperty("isReady");
                if (readyProp != null && (bool)readyProp.GetValue(null))
                {
                    var playersType = System.Type.GetType("Rewired.ReInput, Rewired")
                                          .GetProperty("players").GetValue(null);
                    var getPlayer = playersType.GetType().GetMethod("GetPlayer", new[] { typeof(int) });
                    var player = getPlayer.Invoke(playersType, new object[] { rewiredPlayerId });
                    var getButtonDown = player.GetType().GetMethod("GetButtonDown", new[] { typeof(string) });
                    pressed = (bool)getButtonDown.Invoke(player, new object[] { rewiredAction });
                }
            }
            catch { /* ignore if Rewired not present */ }
        }

        if (pressed) Interact(_currentActor);
    }

    void LateUpdate()
    {
        if (tooltipRoot && tooltipRoot.activeSelf && autoRepositionEachFrame)
        {
            tooltipRoot.transform.position = transform.position + worldOffset;
            if (billboardToCamera && cam)
                tooltipRoot.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);

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
            _currentActor = other.transform;

            string final = format.Replace("{name}", GetUsableName())
                                 .Replace("{msg}", GetUsableMessage());

            Reposition();
            ShowWithText(final);

            if (useUsableIntegration && usable && callSelectOnEnter)
                usable.gameObject.SendMessage("OnSelect", _currentActor, SendMessageOptions.DontRequireReceiver);
            else
                onSelect?.Invoke(_currentActor);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!occupants.Remove(other)) return;

        if (_currentActor == other.transform)
            _currentActor = FindAnyActorStillInside();

        if (occupants.Count == 0)
        {
            SetVisible(false);
            if (useUsableIntegration && usable && callDeselectOnExit)
                usable.gameObject.SendMessage("OnDeselect", SendMessageOptions.DontRequireReceiver);
            else
                onDeselect?.Invoke();
        }
    }

    // Called internally when input is pressed (no player script needed)
    public void Interact(Transform actor)
    {
        if (occupants.Count == 0) return;

        var effectiveActor = actor ? actor : _currentActor;

        if (useUsableIntegration && usable && callOnUse)
        {
            usable.gameObject.SendMessage("OnUse", effectiveActor, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            if (directUseTarget != null)
                directUseTarget.SendMessage("OnUse", effectiveActor, SendMessageOptions.DontRequireReceiver);
            else
                onUse?.Invoke(effectiveActor);
        }

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

    private Transform FindAnyActorStillInside()
    {
        scratchList.Clear();
        foreach (var c in occupants) if (c) scratchList.Add(c);
        return scratchList.Count > 0 ? scratchList[0].transform : null;
    }

    private void Reposition()
    {
        if (!tooltipRoot) return;
        tooltipRoot.transform.position = transform.position + worldOffset;
        if (billboardToCamera && cam)
            tooltipRoot.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }

    private void ShowWithText(string txt)
    {
        if (!tooltipRoot || !messageText) return;
        if (!tooltipRoot.activeSelf) tooltipRoot.SetActive(true);
        messageText.enabled = true;
        messageText.richText = true;
        messageText.text = txt ?? "";
        messageText.ForceMeshUpdate(true, true);
        Canvas.ForceUpdateCanvases();
        var rt = messageText.transform as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        var prt = tooltipRoot.transform as RectTransform;
        if (prt) LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
        var cg = tooltipRoot.GetComponentInParent<CanvasGroup>();
        if (cg) cg.alpha = 1f;
        tooltipRoot.transform.localScale = Vector3.one;
    }

    private void SetVisible(bool v) { if (tooltipRoot) tooltipRoot.SetActive(v); }

    private string GetUsableName()
    {
        if (!usable) return gameObject.name;
        var t = usable.GetType();
        var prop = t.GetProperty("name", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(string)) return (string)prop.GetValue(usable, null);
        var field = t.GetField("name", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(string)) return (string)field.GetValue(usable);
        var m = t.GetMethod("GetName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (m != null && m.ReturnType == typeof(string)) return (string)m.Invoke(usable, null);
        return gameObject.name;
    }

    private string GetUsableMessage()
    {
        if (!usable) return fallbackMessage;
        var t = usable.GetType();

        string TryProp(string n)
        {
            var p = t.GetProperty(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return (p != null && p.PropertyType == typeof(string)) ? (string)p.GetValue(usable, null) : null;
        }
        string TryField(string n)
        {
            var f = t.GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return (f != null && f.FieldType == typeof(string)) ? (string)f.GetValue(usable) : null;
        }

        return TryProp("useMessage") ?? TryProp("overrideUseMessage") ??
               TryField("useMessage") ?? TryField("overrideUseMessage") ??
               (t.GetMethod("GetUseMessage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.Invoke(usable, null) as string)
               ?? fallbackMessage;
    }
}
