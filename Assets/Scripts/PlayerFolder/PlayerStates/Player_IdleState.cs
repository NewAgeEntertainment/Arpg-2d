using UnityEngine;

public class Player_IdleState : Player_GroundedState
{
    public Player_IdleState(Player player, StateMachine stateMachine, string stateName)
        : base(player, stateMachine, stateName) { }

    public override void Enter()
    {
        base.Enter();

        // Ensure gameplay animator is valid
        if (player.anim == null || player.anim.runtimeAnimatorController == null)
        {
            player.ReacquireAnimatorIfNeeded();
            if (player.anim == null || player.anim.runtimeAnimatorController == null)
                return; // abort this frame if animator still missing
        }

        // Stop residual motion
        player.SetVelocity(0f, 0f);

        // Resolve facing from lastMoveDirection (fallback to down)
        Vector2 face = player.lastMoveDirection.sqrMagnitude > 0.0001f
            ? player.lastMoveDirection.normalized
            : Vector2.down;

        // Stamp animator facing (rounded if your controller expects 4/8-way)
        anim.SetFloat("xInput", Mathf.Round(face.x));
        anim.SetFloat("yInput", Mathf.Round(face.y));

        // Keep helpers in sync for dash/attacks out of idle
        player.currentDir = face;
        player.lastMoveDirection = face;
    }

    public override void Update()
    {
        base.Update();

        // Ensure gameplay animator is valid
        if (player.anim == null || player.anim.runtimeAnimatorController == null)
        {
            player.ReacquireAnimatorIfNeeded();
            if (player.anim == null || player.anim.runtimeAnimatorController == null)
                return;
        }

        // Use moveInput populated by PlayerState.Update (Rewired processed axes)
        Vector2 input = player.moveInput;
        bool hasInput = input.sqrMagnitude > 0.0001f;

        if (hasInput)
        {
            // Update facing immediately so dash/attack out of idle uses stick dir
            Vector2 facing = input.normalized;
            player.currentDir = facing;
            player.lastMoveDirection = facing;

            anim.SetFloat("xInput", Mathf.Round(facing.x));
            anim.SetFloat("yInput", Mathf.Round(facing.y));

            stateMachine.ChangeState(player.moveState);
            return; // important: don't keep idling this frame
        }

        // No input: keep idle facing stamped and velocity at zero
        Vector2 face = player.lastMoveDirection.sqrMagnitude > 0.0001f
            ? player.lastMoveDirection
            : Vector2.down;

        anim.SetFloat("xInput", Mathf.Round(face.x));
        anim.SetFloat("yInput", Mathf.Round(face.y));

        player.SetVelocity(0f, 0f);
    }

    public override void UpdateAnimationParameters()
    {
        // Intentionally empty; we stamp animator values explicitly in Enter/Update.
    }
}
