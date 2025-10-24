using UnityEngine;
using Rewired;
using PixelCrushers.DialogueSystem;

[RequireComponent(typeof(ProximitySelector))]
public class RewiredProximitySelectorBridge : MonoBehaviour
{
    [Header("Rewired")]
    public int playerId = 0;
    public string useAction = "Interact";
    public string cancelAction = "Cancel";

    [Header("Optional")]
    public bool holdToUse = false;
    public float holdDuration = 0.35f;

    private ProximitySelector selector;
    private Rewired.Player rPlayer;
    private float holdTimer;

    void Awake()
    {
        selector = GetComponent<ProximitySelector>();
        rPlayer = ReInput.players.GetPlayer(playerId);

        if (selector == null) Debug.LogError("[RewiredProximitySelectorBridge] ProximitySelector missing.");
        if (rPlayer == null) Debug.LogError("[RewiredProximitySelectorBridge] Rewired Player not found. Check playerId.");
    }

    void Update()
    {
        if (selector == null || rPlayer == null) return;

        // Use / Interact
        if (!holdToUse)
        {
            if (rPlayer.GetButtonDown(useAction)) TryUseCurrentUsable();
        }
        else
        {
            if (rPlayer.GetButton(useAction))
            {
                holdTimer += Time.unscaledDeltaTime;
                if (holdTimer >= holdDuration)
                {
                    holdTimer = 0f;
                    TryUseCurrentUsable();
                }
            }
            else holdTimer = 0f;
        }

        // Cancel / Deselect
        if (rPlayer.GetButtonDown(cancelAction))
        {
            TryCancelSelection();
        }
    }

    private void TryUseCurrentUsable()
    {
        var currentUsable = GetCurrentUsable(selector);
        if (currentUsable == null) return;

        // Try the common API first: OnUse(Transform actor)
        bool invoked = InvokeIfExists(currentUsable, "OnUse", transform);

        // Some versions expose Use(Transform) or Use()—try those too:
        if (!invoked) invoked = InvokeIfExists(currentUsable, "Use", transform);
        if (!invoked) invoked = InvokeIfExists(currentUsable, "Use");

        // If none of the above exist, fall back to SendMessage (no error if missing).
        if (!invoked) currentUsable.gameObject.SendMessage("OnUse", transform, SendMessageOptions.DontRequireReceiver);
    }

    private void TryCancelSelection()
    {
        // Try selector-level methods first:
        bool deselected =
            InvokeIfExists(selector, "Deselect") ||
            InvokeIfExists(selector, "DeSelect") ||
            InvokeIfExists(selector, "CancelSelection") ||
            InvokeIfExists(selector, "ClearSelection");

        if (!deselected)
        {
            // Fall back: tell the current usable to deselect itself.
            var currentUsable = GetCurrentUsable(selector);
            if (currentUsable != null)
            {
                bool called = InvokeIfExists(currentUsable, "OnDeselect");
                if (!called)
                    currentUsable.gameObject.SendMessage("OnDeselect", SendMessageOptions.DontRequireReceiver);
            }

            // Optional: try to null out selector.currentUsable via reflection (if field/property exists).
            TryClearCurrentUsable(selector);
        }
    }

    private Usable GetCurrentUsable(ProximitySelector sel)
    {
        if (sel == null) return null;

        // Try property CurrentUsable / currentUsable
        var prop = typeof(ProximitySelector).GetProperty("CurrentUsable")
                   ?? typeof(ProximitySelector).GetProperty("currentUsable");
        if (prop != null) return prop.GetValue(sel, null) as Usable;

        // Try field currentUsable
        var field = typeof(ProximitySelector).GetField("currentUsable",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null) return field.GetValue(sel) as Usable;

        return null;
    }

    private void TryClearCurrentUsable(ProximitySelector sel)
    {
        // Property setter?
        var prop = typeof(ProximitySelector).GetProperty("CurrentUsable");
        if (prop != null && prop.CanWrite) { prop.SetValue(sel, null, null); return; }

        // Field?
        var field = typeof(ProximitySelector).GetField("currentUsable",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null) field.SetValue(sel, null);
    }

    private bool InvokeIfExists(object target, string methodName, params object[] args)
    {
        if (target == null) return false;
        var t = target.GetType();

        // Try matching by args signature first:
        var argTypes = System.Array.ConvertAll(args, a => a?.GetType() ?? typeof(object));
        var method = t.GetMethod(methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, argTypes, null);

        if (method == null)
        {
            // Fallback: any method with that name (ignore signature)
            var methods = t.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            foreach (var m in methods)
            {
                if (m.Name == methodName)
                {
                    // Try to adapt a single Transform argument if available
                    var ps = m.GetParameters();
                    if (ps.Length == 0)
                    {
                        m.Invoke(target, null);
                        return true;
                    }
                    if (ps.Length == 1 && args.Length >= 1 && ps[0].ParameterType.IsAssignableFrom(args[0]?.GetType()))
                    {
                        m.Invoke(target, new object[] { args[0] });
                        return true;
                    }
                }
            }
            return false;
        }

        method.Invoke(target, args);
        return true;
    }
}
