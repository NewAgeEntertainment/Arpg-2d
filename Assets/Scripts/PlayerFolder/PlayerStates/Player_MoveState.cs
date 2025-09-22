using Rewired;
using UnityEngine;

public class Player_MoveState : Player_GroundedState
{
    // Small deadzone so tiny stick noise doesn't change facing
    private const float stickDeadzone = 0.25f;
    private static readonly float stickDeadzoneSqr = stickDeadzone * stickDeadzone;

    public Player_MoveState(Player player, StateMachine stateMachine, string stateName)
        : base(player, stateMachine, stateName) { }

    public override void Update()
    {
        base.Update();

        Vector2 input = player.moveInput;
        bool hasInput = input.sqrMagnitude > 0.0001f;

        if (!hasInput)
        {
            player.SetVelocity(0f, 0f);
            stateMachine.ChangeState(player.idleState);
            return; // <-- don't execute movement below
        }

        // Movement first (independent of animator availability)
        player.SetVelocity(input.x * player.moveSpeed, input.y * player.moveSpeed);

        // Update facing
        Vector2 facing = input.normalized;
        player.currentDir = facing;
        player.lastMoveDirection = facing;

        // Animator (safe, but don't early-return if missing)
        if (player.anim == null || player.anim.runtimeAnimatorController == null)
        {
            player.ReacquireAnimatorIfNeeded();
        }
        else
        {
            anim.SetFloat("xInput", Mathf.Round(facing.x));
            anim.SetFloat("yInput", Mathf.Round(facing.y));
        }
    }




}
