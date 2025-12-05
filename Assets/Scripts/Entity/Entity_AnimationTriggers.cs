using UnityEngine;

public class Entity_AnimationTriggers : MonoBehaviour
{
    private Entity entity;
    private Entity_Combat entityCombat;

    protected virtual void Awake()
    {
        // Grab references from the parent (your Player/Enemy root)
        entity = GetComponentInParent<Entity>();
        entityCombat = GetComponentInParent<Entity_Combat>();
    }

    /// <summary>
    /// Called by a generic animation event if you want the current state
    /// (idle/attack/etc) to be notified.
    /// </summary>
    private void CurrentStateTrigger()
    {
        if (entity != null)
            entity.CurrentStateAnimationTrigger();
    }

    /// <summary>
    /// Classic one-shot attack: put this event on the exact frame
    /// you want a single hit-check.
    /// </summary>
    private void AttackTrigger()
    {
        if (entityCombat == null)
        {
            Debug.LogWarning("[AnimTrigger] No Entity_Combat on " + name);
            return;
        }

        Debug.Log("[AnimTrigger] AttackTrigger fired on " + name);
        entityCombat.PerformAttack();
    }


    /// <summary>
    /// BEGIN hit window – call this from an animation event named
    /// "AttackHitStart" (or whatever you like) at the frame when the
    /// weapon becomes active.
    /// </summary>
    private void AttackHitStart()
    {
        if (entityCombat != null)
            entityCombat.BeginAttackHitbox();   // <- name matches Entity_Combat
    }

    /// <summary>
    /// END hit window – call this from an animation event named
    /// "AttackHitEnd" at the frame when the swing stops being active.
    /// </summary>
    private void AttackHitEnd()
    {
        if (entityCombat != null)
            entityCombat.EndAttackHitbox();     // <- name matches Entity_Combat
    }
}
