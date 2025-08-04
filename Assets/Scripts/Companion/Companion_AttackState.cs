using UnityEngine;

// Companion attacks an enemy
public class Companion_AttackState : CompanionState
{
    private Transform target;

    public Companion_AttackState(Companion c, StateMachine sm)
        : base(c, sm, "attack") { }

    public override void Enter()
    {
        base.Enter();

        target = companion.GetNearestEnemy();
        companion.StopMovement();
        SyncAttackSpeed();
        rb.velocity = Vector2.zero;

        // ✅ Use SetBool instead of trigger for safer transitions
        anim.SetBool("attack", true);

        Debug.Log("[Companion_AttackState] Enter()");
    }

    public override void Update()
    {
        base.Update();

        if (companion.IsTooFarFromPlayer())
        {
            stateMachine.ChangeState(companion.returnState);
            return;
        }

        if (target == null || !companion.IsEnemyInAttackRange(target))
        {
            stateMachine.ChangeState(companion.chaseState);
            return;
        }

        // Face the target and update direction input
        Vector2 dir = (target.position - companion.transform.position).normalized;
        companion.FaceTarget(target.position);

        anim.SetFloat("xInput", dir.x);
        anim.SetFloat("yInput", dir.y);
        Debug.Log($"[Update] xInput: {dir.x}, yInput: {dir.y}");

        // ✅ Wait for animation to finish before exiting
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (triggerCalled && stateInfo.IsName("Attack Tree") && stateInfo.normalizedTime >= 1f)
        {
            Debug.Log("[Companion_AttackState] Animation complete. Switching to chase.");
            stateMachine.ChangeState(companion.chaseState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        anim.SetBool("attack", false); // ✅ Reset attack bool
        Debug.Log("[Companion_AttackState] Exit()");
    }
}
