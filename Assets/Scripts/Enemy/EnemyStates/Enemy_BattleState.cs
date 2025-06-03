using UnityEngine;

public class Enemy_BattleState : EnemyState
{
    private Transform player;
    private float lastTimeWasInBattle;
    private new Enemy_Rabbie enemy; // Use 'new' keyword to explicitly hide the inherited member
    private Vector2 moveDir;

    public Enemy_BattleState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
        this.enemy = enemy as Enemy_Rabbie; // Ensure the enemy is cast to Enemy_Rabbie
    }

    public override void Enter()
    {
        base.Enter();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        //if (player == null)
        //{
        //   // player = enemy.GetPlayerReference();
        //}
        
    }

    public override void Update()
    {
        base.Update();

        if (player == null)
        {
            
            return;
        }

        Collider2D detectedPlayer = enemy.PlayerDetected();
        
        if (detectedPlayer != null)
        {
            stateTimer = enemy.battleTime;

            float moveToPlayer = Vector2.Distance(enemy.transform.position, detectedPlayer.transform.position);

            if (moveToPlayer < enemy.attackDistance)
            {
                if (CanAttack())
                {
                    stateMachine.ChangeState(enemy.attackState);
                }
            }
        }
        else
        {
            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > 7)
                stateMachine.ChangeState(enemy.idleState);
        }

        moveDir = new Vector2(player.position.x - enemy.transform.position.x, player.position.y - enemy.transform.position.y);
        moveDir.Normalize();

        enemy.currentDir = moveDir; // Update the enemy's current direction

        enemy.anim.SetFloat("xInput", enemy.currentDir.x);
        enemy.anim.SetFloat("yInput", enemy.currentDir.y);

        moveDir.Normalize();

        enemy.SetVelocity(moveDir.x * enemy.moveSpeed, moveDir.y * enemy.moveSpeed);
    }

    private bool CanAttack()
    {
        if (Time.time >= enemy.lastTimeAttacked + enemy.attackCooldown)
        {
            enemy.lastTimeAttacked = Time.time;
            return true;
        }
        
        return false;
    }

    //private void UpdateStateTimer()
    //{
    //    // Add this method to resolve CS0103
    //    stateTimer = enemy.battleTime;
    //}

    public override void Exit()
    {
        base.Exit();
        

        enemy.anim.SetFloat("xInput", 0f);
        enemy.anim.SetFloat("yInput", 0f);
    }
}
