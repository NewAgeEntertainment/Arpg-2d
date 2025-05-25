using UnityEngine;

public class Player_ThrustState : PlayerState
{
    private Vector2 ThrustDir; // Changed from float to Vector2

    public Player_ThrustState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        ThrustDir = new Vector2(player.currentDir.x, player.currentDir.y); // No changes needed here
                                                                           // this is where we set the dash direction. for 4 directional movement.
        
        
        stateTimer = player.ThrustDuration;

        //originalGravityScale = rb.gravityScale;
        //rb.gravityScale = 0;
    }

    public override void Update()
    {
        base.Update();
        CancelThrustIfNeeded();
        player.SetVelocity(player.dashSpeed * ThrustDir.x, player.dashSpeed * ThrustDir.y); // Updated to use ThrustDir.x and ThrustDir.y

        if (stateTimer < 0)
        {
            stateMachine.ChangeState(player.idleState);
            //else
            //    stateMachine.ChangeState(player.fallState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        player.SetVelocity(0, 0);
        //rb.gravityScale = originalGravityScale;
    }

    private void CancelThrustIfNeeded()
    {

    }
}
