using UnityEngine;

public abstract class NPCState : EntityState
{
    protected NPC npc;

    protected NPCState(NPC npc, StateMachine stateMachine, string animBoolName)
        : base(stateMachine, animBoolName)
    {
        this.npc = npc;
        this.anim = npc.anim;
        this.rb = npc.rb;
        this.stats = npc.GetComponent<Entity_Stats>();
    }

    public override void UpdateAnimationParameters()
    {
        // Optional: feed animator movement values (already set in Entity via inputs if you like)
        if (rb == null || anim == null) return;
        anim.SetFloat("xInput", rb.velocity.x);
        anim.SetFloat("yInput", rb.velocity.y);
    }
}
