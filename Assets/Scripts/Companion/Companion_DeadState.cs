using UnityEngine;

/// <summary>
/// Companion stays in a death animation for a set delay, then auto-revives
/// and returns to follow/idle depending on party status.
/// </summary>
public class Companion_DeadState : CompanionState
{
    private readonly float reviveDelay;
    private readonly float revivePercent; // 0..1 of Max HP to restore on revive
    private readonly string deathAnimBool; // animator bool used by this state

    private Entity_Health health;
    private float timer;
    private bool revived;

    public Companion_DeadState(
        Companion companion,
        StateMachine stateMachine,
        string deathAnimBoolName = "dead",
        float reviveDelaySeconds = 3f,
        float reviveToPercent01 = 0.25f)
        : base(companion, stateMachine, deathAnimBoolName)
    {
        deathAnimBool = deathAnimBoolName;
        reviveDelay = Mathf.Max(0f, reviveDelaySeconds);
        revivePercent = Mathf.Clamp01(reviveToPercent01);
        health = companion.GetComponent<Entity_Health>();
    }

    public override void Enter()
    {
        base.Enter();

        timer = 0f;
        revived = false;

        // Stop all motion immediately
        if (rb != null) rb.velocity = Vector2.zero;
        companion.SetVelocity(0f, 0f);

        // Disable combat/AI while dead (no missing method risk)
        if (companion.combat != null) companion.combat.enabled = false;

        // Ensure the death anim is latched (base.Enter already sets animBool true,
        // but this is safe even if base doesn't)
        if (anim != null && !string.IsNullOrEmpty(deathAnimBool))
            anim.SetBool(deathAnimBool, true);
    }

    public override void Update()
    {
        base.Update();

        timer += Time.deltaTime;

        if (!revived && timer >= reviveDelay)
        {
            revived = true;
            ReviveNow();
        }
    }

    public override void Exit()
    {
        base.Exit();

        // Clear death flag (base.Exit sets animBool false too, but safe to force)
        if (anim != null && !string.IsNullOrEmpty(deathAnimBool))
            anim.SetBool(deathAnimBool, false);

        // Re-enable combat
        if (companion.combat != null) companion.combat.enabled = true;
    }

    private void ReviveNow()
    {
        if (health == null) return;

        // Revive and set desired HP
        health.ForceReviveToFull();
        if (revivePercent > 0f && revivePercent < 1f)
            health.SetCurrentHealth(health.GetMaxHealth() * revivePercent);

        // Decide where to go next
        if (companion.InParty && companion.followState != null)
            stateMachine.ChangeState(companion.followState);
        else if (companion.idleState != null)
            stateMachine.ChangeState(companion.idleState);
    }
}
