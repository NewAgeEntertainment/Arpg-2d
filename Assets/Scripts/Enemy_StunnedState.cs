using UnityEngine;

public class Enemy_StunnedState : EnemyState
{
    private Enemy_VFX vfx;
    public Enemy_StunnedState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
        vfx = enemy.GetComponent<Enemy_VFX>();
    }


    public override void Enter()
    {
        base.Enter();

        vfx.EnableAttackAlert(false); // Disable the attack alert when entering the stunned state
        enemy.EnableCounterWindow(false); // Disable the counter window
        stateTimer = enemy.stunnedDuration; // Set the state timer to the stunned duration
        rb.linearVelocity = new Vector2(enemy.stunnedVelocity.x * -enemy.currentDir.x, enemy.stunnedVelocity.y -enemy.currentDir.y); // Apply the stunned velocity
    

    }

    public override void Update()
    {
        base.Update();
        
        if (stateTimer < 0)
        {
            stateMachine.ChangeState(enemy.idleState); // Change to idle state if the timer is less than 0
        }
    }

}
