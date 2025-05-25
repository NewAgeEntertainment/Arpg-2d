using UnityEngine;

public class Player_DeadState : PlayerState
{
    public Player_DeadState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        //this is for the new input system.
        //input.Disable();
        //this is for the old input system.
        input.Player.Movement.Disable();
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // Disable physics simulation
    }
}
