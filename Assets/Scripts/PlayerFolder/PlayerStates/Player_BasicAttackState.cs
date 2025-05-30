using UnityEngine;

public class Player_BasicAttackState : PlayerState
{
    private float attackVelocityTimer;
    private float lastTimeAttacked;

    private bool comboAttackQueued;
    private float attackDir; // Corrected type
    private float attackDiry; // Corrected type
    private int comboIndex = 1;
    private int comboLimit = 3; // 
    private const int FirstComboIndex = 1; // We start combo index with number 1, this parameter is used in the Animator.

    public Player_BasicAttackState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        if (comboLimit != player.attackMovement.Length)
        {
            Debug.LogWarning("Adjusted combo limit to match attack velocity array!");
            comboLimit = player.attackMovement.Length;
        }
    }

    public override void Enter()
    {
        base.Enter();
        comboAttackQueued = false;
        ResetComboIndexIfNeeded();
        SyncAttackSpeed(); // Sync the attack speed with the player's stats

        // Modify attack direction based on player input  
        attackDir = player.currentDir.x; // Use xInput for horizontal direction  
        attackDiry = player.currentDir.y; // Use yInput for vertical direction  
        // this is where we set the attack direction. for 4 directional movement.

        anim.SetInteger("basicAttackIndex", comboIndex);
        ApplyAttackVelocity();
    }

    public override void Update()
    {
        base.Update();
        HandleAttackVelocity();

        // detect and damage enemies
        if (Input.GetKeyDown(KeyCode.E)) // Replaced 'input.GetKeyDown' with 'Input.GetKeyDown' from UnityEngine
            QueueNextAttack();

        if (triggerCalled)
            HandleStateExit();
    }

    public override void Exit()
    {
        base.Exit();
        comboIndex++;
        lastTimeAttacked = Time.time;
    }

    private void HandleStateExit()
    {
        if (comboAttackQueued)
        {
            anim.SetBool(animBoolName, false);
            player.EnterAttackStateWithDelay();
        }
        else
            stateMachine.ChangeState(player.idleState);
    }

    private void QueueNextAttack()
    {
        if (comboIndex < comboLimit)
            comboAttackQueued = true;
    }

    private void HandleAttackVelocity()
    {
        attackVelocityTimer -= Time.deltaTime;

        if (attackVelocityTimer < 0)
            player.SetVelocity(0, 0);
    }

    private void ApplyAttackVelocity() // use this as a example of how to add attack velocity to the player. for directional movement.
    {
        // change the method for it can work with 4 dirctional movment. 
        float attackMovement = player.attackMovement[comboIndex - 1];

        
        player.SetVelocity(attackMovement * attackDir, attackMovement * attackDiry);
    }// this is the method that sets the attack velocity.

    private void ResetComboIndexIfNeeded()
    {
        if (Time.time > lastTimeAttacked + player.comboResetTime)
            comboIndex = FirstComboIndex;

        if (comboIndex > comboLimit)
            comboIndex = FirstComboIndex;
    }
}
