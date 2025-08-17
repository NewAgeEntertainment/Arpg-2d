using UnityEngine;

/// <summary>
/// Minimal hook to guarantee the Player state machine enters its initial state after spawn,
/// so PlayerState.Enter() runs and Rewired gets cached.
/// If your Player already calls stateMachine.Initialize(...) you don't need this.
/// </summary>
[RequireComponent(typeof(Player))]
public class Player_InitHook : MonoBehaviour
{
    [Tooltip("Optional: if your Player exposes an idleState, leave this null and the hook will try to find it via reflection. Otherwise drag a starting state here.")]
    public EntityState initialState;

    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    public void InitializeAfterSpawn()
    {
        // If Player already has a public/protected InitializeAfterSpawn, prefer that.
        var method = typeof(Player).GetMethod("InitializeAfterSpawn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (method != null)
        {
            method.Invoke(player, null);
            return;
        }

        // Fallback: call stateMachine.Initialize(initialState) if we can find both pieces.
        var smField = typeof(Player).GetField("stateMachine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var idleField = typeof(Player).GetField("idleState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        var sm = smField != null ? smField.GetValue(player) as StateMachine : null;
        var startState = initialState != null ? initialState : (idleField != null ? idleField.GetValue(player) as EntityState : null);

        if (sm != null && startState != null)
        {
            sm.Initialize(startState);
            Debug.Log("[Player_InitHook] Initialized state machine with initial state.");
        }
        else
        {
            Debug.LogWarning("[Player_InitHook] Could not initialize state machine. Ensure your Player initializes itself.");
        }
    }
}
