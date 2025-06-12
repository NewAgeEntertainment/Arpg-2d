using UnityEngine;
using UnityEngine.InputSystem;

public class Player_GroundedState : PlayerState
{
    public Player_GroundedState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        //if (rb.linearVelocity.y < 0 && player.groundDetected == false)
        //    stateMachine.ChangeState(player.fallState);

        //if (input.Player.Jump.WasPerformedThisFrame())
        //    stateMachine.ChangeState(player.jumpState);

        //if (input.Player.Attack.WasPerformedThisFrame())
        //    stateMachine.ChangeState(player.basicAttackState);

        if (rPlayer.GetButton("Attack")) // Replaced 'input.GetKeyDown' with 'Input.GetKeyDown' from UnityEngine
            stateMachine.ChangeState(player.basicAttackState);
    
        //if (input.Player.Attack.WasPerformedThisFrame()) // Using the new input system
        //    stateMachine.ChangeState(player.counterAttackState);

        if (rPlayer.GetButtonDown("Counter")) // Replaced 'input.GetKeyDown' with 'Input.GetKeyDown' from UnityEngine
            stateMachine.ChangeState(player.counterAttackState);
    }
}
