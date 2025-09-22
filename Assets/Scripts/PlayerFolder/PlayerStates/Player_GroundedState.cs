using UnityEngine;
using Rewired;

public class Player_GroundedState : PlayerState
{
    // one-per-frame guards so a single press can’t trigger multiple ChangeState calls
    private int _lastAttackStartFrame = -1;
    private int _lastCounterStartFrame = -1;

    private const string AttackAction = "Attack";
    private const string CounterAction = "Counter";

    public Player_GroundedState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Update()
    {
        base.Update();

        if (!ReInput.isReady || rPlayer == null) return;

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

        // (your move / jump checks would go here if needed)
    }
}
