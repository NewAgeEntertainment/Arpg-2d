public class EnemyState : EntityState
{
    protected Enemy enemy;

    public EnemyState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.enemy = enemy;

        rb = enemy.rb;
        anim = enemy.anim;
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();

        float battleAnimSpeedMultiplier = enemy.battleMoveSpeedMultiplier / enemy.moveSpeed;

        anim.SetFloat("battleAnimSpeedMultiplier", battleAnimSpeedMultiplier);
        anim.SetFloat("moveAnimSpeedMultiplier", enemy.moveAnimSpeedMultiplier);

        // Fix for CS1503: Correctly pass float values for xVelocity and yVelocity
        anim.SetFloat("xInput", rb.linearVelocity.x);
        anim.SetFloat("yInput", rb.linearVelocity.y);
    }
}
