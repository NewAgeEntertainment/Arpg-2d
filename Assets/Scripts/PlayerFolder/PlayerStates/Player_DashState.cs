using UnityEngine;

public class Player_DashState : PlayerState
{
    //private float originalGravityScale;
    private float dashDirx;
    private float dashDiry;

    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        skillManager.dash.OnStartEffect();
        player.vfx.DoImageEchoEffect(player.dashDuration);

        // this is where we set the dash direction. for 4 directional movement.
        dashDirx = player.currentDir.x;
        dashDiry = player.currentDir.y;
        //----
        stateTimer = player.dashDuration;

        //originalGravityScale = rb.gravityScale;
        //rb.gravityScale = 0;
    }


    public override void Update()
    {
        base.Update();
        CancelDashIfNeeded();
        player.SetVelocity(player.dashSpeed * dashDirx, player.dashSpeed * dashDiry);


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

        skillManager.dash.OnEndEffect();

        player.SetVelocity(0, 0);
        //rb.gravityScale = originalGravityScale;
    }

    private void CancelDashIfNeeded()
    {
        
    }
}
