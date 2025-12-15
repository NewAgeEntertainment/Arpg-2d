using UnityEngine;
using Rewired;

public class Player_GroundedState : PlayerState
{
    private int _lastAttackStartFrame = -1;
    private int _lastCounterStartFrame = -1;

    private const string AttackAction = "Attack";
    private const string CounterAction = "Counter";

    // NEW
    private const string SkillModifierAction = "SkillModifier";

    public Player_GroundedState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Update()
    {
        base.Update();

        if (!ReInput.isReady || rPlayer == null) return;

        // NEW: while holding SkillModifier, DO NOT allow basic attack/counter
        if (rPlayer.GetButton(SkillModifierAction))
            return;

        // ---- START BASIC ATTACK (edge only, once per frame) ----
        if (rPlayer.GetButtonDown(AttackAction) && Time.frameCount != _lastAttackStartFrame)
        {
            _lastAttackStartFrame = Time.frameCount;
            stateMachine.ChangeState(player.basicAttackState);
            return;
        }

        // ---- START COUNTER (edge only, once per frame) ----
        if (rPlayer.GetButtonDown(CounterAction) && Time.frameCount != _lastCounterStartFrame)
        {
            _lastCounterStartFrame = Time.frameCount;
            stateMachine.ChangeState(player.counterAttackState);
            return;
        }
    }
}
