using UnityEngine;

public class Companion_AnimationTriggers : Entity_AnimationTriggers
{
    private Companion companion;
    private CompanionCombat companionCombat;

    protected override void Awake()
    {
        companion = GetComponentInParent<Companion>();
        companionCombat = GetComponentInParent<CompanionCombat>();
    }

    //private void CurrentStateTrigger()
    //{
    //    if (companion != null)
    //        companion.CurrentStateAnimationTrigger();
    //}

    //private void AttackTrigger()
    //{
    //    Debug.Log("[Companion Animation Trigger] AttackTrigger()");
    //    if (companionCombat != null)
    //        companionCombat.PerformAttack();
    //}
}
