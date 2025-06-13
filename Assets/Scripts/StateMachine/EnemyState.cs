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

        anim.SetFloat("xInput", rb.velocity.x);
        anim.SetFloat("yInput", rb.velocity.y);
    }
}
