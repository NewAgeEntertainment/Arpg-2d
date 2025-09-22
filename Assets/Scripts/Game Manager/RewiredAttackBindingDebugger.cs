using UnityEngine;
using Rewired;

public class RewiredAttackBindingDebugger : MonoBehaviour
{
    [SerializeField] private int rewiredPlayerId = 0;
    [SerializeField] private string actionName = "Attack";

    // Press this Rewired button action to dump bindings (set it temporarily in your maps),
    // or change this to a key check if you prefer.
    [SerializeField] private string dumpBindingsAction = "ShowBindings";

    void Update()
    {
        if (!ReInput.isReady) return;

        var p = ReInput.players.GetPlayer(rewiredPlayerId);
        if (p == null) return;

        if (!string.IsNullOrEmpty(dumpBindingsAction) && p.GetButtonDown(dumpBindingsAction))
        {
            DumpAttackBindings(p, actionName);
        }
    }

    private void DumpAttackBindings(Rewired.Player p, string action)
    {
        int actionId = ReInput.mapping.GetActionId(action);
        if (actionId < 0)
        {
            Debug.LogWarning($"[RewiredAttackBindingDebugger] Action '{action}' not found.");
            return;
        }

        Debug.Log($"--- Attack bindings for player {p.id} (Action '{action}', id {actionId}) ---");

        // Iterate all enabled controller maps
        var maps = p.controllers.maps.GetAllMaps();
        foreach (var map in maps)
        {
            if (map == null || !map.enabled) continue;

            // Print all element maps bound to our actionId
            var elementMaps = map.ElementMaps;
            for (int i = 0; i < elementMaps.Count; i++)
            {
                var aem = elementMaps[i];
                if (aem == null || aem.actionId != actionId) continue;

                // Safe, available properties on ActionElementMap / ControllerMap
                Debug.Log(
                    $"[{map.controllerType} cat:{map.categoryId} layout:{map.layoutId}] " +
                    $"elem='{aem.elementIdentifierName}' type={aem.elementType} axisRange={aem.axisRange} invert={aem.invert}"
                );
            }
        }

        Debug.Log("--- end ---");
    }
}
