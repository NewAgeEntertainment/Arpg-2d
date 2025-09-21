using UnityEngine;

// Companion attacks an enemy
public class Companion_AttackState : CompanionState
{
    private Transform target;

    // Animator helpers
    private static readonly int AttackBool = Animator.StringToHash("attack");
    private const int BaseLayer = 0;
    private static readonly int AttackTag = Animator.StringToHash("Attack"); // tag your attack states as "Attack"

    // Safety fallbacks
    private float _fallbackTimer;
    private const float MaxAttackWait = 2.0f; // if the anim event never fires, don't hang forever

    public Companion_AttackState(Companion c, StateMachine sm)
        : base(c, sm, "attack") { }

    public override void Enter()
    {
        base.Enter();

        target = companion.GetNearestEnemy();
        companion.StopMovement();
        rb.velocity = Vector2.zero;

        // Sync attack speed (expects CompanionState->SyncAttackSpeed to set anim float)
        SyncAttackSpeed();

        // Drive the Animator using a bool (safer than transient triggers)
        anim.SetBool(AttackBool, true);

        // Face target immediately (if any)
        if (target != null)
            companion.FaceTarget(target.position);

        _fallbackTimer = 0f;
        triggerCalled = false;

        // Debug.Log("[Companion_AttackState] Enter()");
    }

    public override void Update()
    {
        base.Update();

        // Too far from the player? Go back toward them.
        if (companion.IsTooFarFromPlayer())
        {
            stateMachine.ChangeState(companion.returnState);
            return;
        }

        // If no target or the target moved out of chase radius, try to reacquire
        if (target == null || !companion.IsEnemyInChaseRadius(target))
        {
            target = companion.GetNearestEnemy();
            if (target == null)
            {
                // No enemies to chase → go back to follow/idle
                stateMachine.ChangeState(companion.followState);
                return;
            }
        }

        // If target is not in attack range anymore, chase it
        if (!companion.IsEnemyInAttackRange(target))
        {
            stateMachine.ChangeState(companion.chaseState);
            return;
        }

        // Keep facing the target (helps 4-dir anims)
        companion.FaceTarget(target.position);

        // Optional: drive x/y inputs for blend trees
        Vector2 dir = (target.position - companion.transform.position).normalized;
        anim.SetFloat("xInput", dir.x);
        anim.SetFloat("yInput", dir.y);

        // --- Exit conditions ---
        // Preferred: animation event calls state.AnimationTrigger() -> sets triggerCalled = true
        if (triggerCalled)
        {
            // Attack animation finished via event → decide next state
            FinishAndBranch();
            return;
        }

        // Fallback: if currently in an "Attack" tagged state and it's finished (non-looping)
        var st = anim.GetCurrentAnimatorStateInfo(BaseLayer);
        bool inAttack = st.tagHash == AttackTag || st.IsTag("Attack"); // tag-safe check
        if (inAttack && !st.loop && st.normalizedTime >= 1f)
        {
            FinishAndBranch();
            return;
        }

        // Hard timeout so we never hang
        _fallbackTimer += Time.deltaTime;
        if (_fallbackTimer >= MaxAttackWait)
        {
            FinishAndBranch();
            return;
        }
    }

    private void FinishAndBranch()
    {
        // If we still have a target & it's in chase radius, go chase (re-assess attack next frame)
        if (target != null && companion.IsEnemyInChaseRadius(target))
            stateMachine.ChangeState(companion.chaseState);
        else
            stateMachine.ChangeState(companion.followState);
    }

    public override void Exit()
    {
        // Reset attack driver
        anim.SetBool(AttackBool, false);
        companion.StopMovement();
        base.Exit();
        // Debug.Log("[Companion_AttackState] Exit()");
    }
}
