using Rewired;
using UnityEngine;

public class Player_MoveState : Player_GroundedState
{
    public Player_MoveState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    //protected override void FixedUpdate()
    //{
    //    base.FixedUpdate();

    //    player.anim.SetFloat("xInput", player.moveInput.x);
    //    player.anim.SetFloat("yInput", player.moveInput.y);
    //}

    public override void Update()
    {
        base.Update();

        // Use Rewired for input handling  

        // Update animator parameters  
        anim.SetFloat("xInput", xInput);
        anim.SetFloat("yInput", yInput);

        // Transition to idle state if no input  
        if (moveInput.x == 0 && moveInput.y == 0)
        {
            stateMachine.ChangeState(player.idleState);
        }
        else
        {
            player.currentDir = new Vector2(moveInput.x, moveInput.y); // Update current direction  
        }

        // Set player velocity based on input  
        player.SetVelocity(moveInput.x * player.moveSpeed, moveInput.y * player.moveSpeed);
    }
}
