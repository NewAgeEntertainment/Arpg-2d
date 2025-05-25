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

        //UpdateStateTimer();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        //if (player == null)
        //{
        //   // player = enemy.GetPlayerReference();
        //}
        Debug.Log("Battle State Entered");

       
    }

    public override void Update()
    {
        base.Update();

        if (player == null)
        {
            Debug.LogWarning("Player reference is null. Skipping Update logic.");
            return;
        }

        Collider2D detectedPlayer = enemy.PlayerDetected();
        Debug.Log("Detected Player: " + detectedPlayer);
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
                stateMachine.ChangeState(enemy.patrollingState);
        }

        moveDir = new Vector2(player.position.x - enemy.transform.position.x, player.position.y - enemy.transform.position.y);

        enemy.anim.SetFloat("xInput", moveDir.x);
        enemy.anim.SetFloat("yInput", moveDir.y);

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
        Debug.Log("Attack is on cooldown");
        return false;
    }

    //private void UpdateStateTimer()
    //{
    //    // Add this method to resolve CS0103
    //    stateTimer = enemy.battleTime;
    //}
}
