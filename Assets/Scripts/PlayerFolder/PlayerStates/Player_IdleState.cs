using UnityEngine;

public class Player_IdleState : Player_GroundedState
{
    public Player_IdleState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.SetVelocity(0, 0);
        UpdateAnimationParameters();
        anim.SetFloat("xInput", xInput);
        anim.SetFloat("yInput", yInput);
        
    }

    public override void Update()
    {
        base.Update();

        //if (player.moveInput.x == player.facingDir && player.wallDetected) //
        //    return;

        if (xInput != 0 || yInput != 0) // if player is not moving
            stateMachine.ChangeState(player.moveState); // 
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();

    }

}
