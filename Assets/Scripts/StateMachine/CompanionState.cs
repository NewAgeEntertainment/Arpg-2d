using UnityEngine;

public abstract class CompanionState : EntityState
{
    protected Companion companion;

    public CompanionState(Companion companion, StateMachine stateMachine, string animBoolName)
        : base(stateMachine, animBoolName)
    {
        this.companion = companion;

        rb = companion.rb;
        anim = companion.anim;
        this.stats = companion.GetComponent<Entity_Stats>();
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();

        if (companion == null || anim == null || rb == null)
            return;

        float battleAnimSpeedMultiplier = companion.battleMoveSpeed / companion.moveSpeed;
        anim.SetFloat("battleAnimSpeedMultiplier", battleAnimSpeedMultiplier);
        anim.SetFloat("moveAnimSpeedMultiplier", companion.moveAnimSpeedMultiplier);

        anim.SetFloat("xInput", rb.velocity.x);
        anim.SetFloat("yInput", rb.velocity.y);
    }

    
}
