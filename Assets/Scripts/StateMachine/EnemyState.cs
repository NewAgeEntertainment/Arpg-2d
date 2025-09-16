public class EnemyState : EntityState
{
    protected Enemy enemy;

    public EnemyState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.enemy = enemy;

        rb = enemy.rb;
        anim = enemy.anim;
        stats = enemy.stats; // Get the Entity_Stats component from the enemy

    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();

        float battleAnimSpeedMultiplier = enemy.battleMoveSpeed / enemy.moveSpeed;
        anim.SetFloat("battleAnimSpeedMultiplier", battleAnimSpeedMultiplier);
        anim.SetFloat("moveAnimSpeedMultiplier", enemy.moveAnimSpeedMultiplier);

        // If we are moving, face movement. Otherwise keep last / face target.
        if (rb.velocity.sqrMagnitude > 0.0001f)
        {
            enemy.SetFacing(rb.velocity);
        }
        else
        {
            // If we have a player, face them; else keep last.
            var p = enemy.GetPlayerReference();
            if (p != null)
                enemy.SetFacing(p.position - enemy.transform.position);
            else
                enemy.SetFacing(enemy.LastDir);
        }
    }

}
