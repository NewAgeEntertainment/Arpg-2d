using UnityEngine;

public class Player_MoveState : Player_GroundedState
{
    public Player_MoveState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    public override void Update()
    {
        base.Update();

        player.anim.SetFloat("xInput", xInput); // x 
        player.anim.SetFloat("yInput", yInput);
        // yinput perameters isnt being set in the animator. why?

        if (xInput == 0 && yInput == 0)
        {
            stateMachine.ChangeState(player.idleState);
        }
        else
        {
            player.currentDir = new Vector2(xInput, yInput); // Update currentDir based on input
        }
        
            // Fix: Directly set the currentDirection property since no SetCurrentDirection method exists
            
        

        player.SetVelocity(xInput * player.moveSpeed, yInput * player.moveSpeed); // set
    }
}
