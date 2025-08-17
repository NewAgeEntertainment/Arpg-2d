using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Rewired;

public abstract class PlayerState : EntityState
{
    protected Player player;
    protected PlayerInputSet input;
    protected Player_SkillManager skillManager;
    protected Entity_Mana mana;

    public float attackSpeed { get; protected set; }

    // Because PlayerState is NOT a MonoBehaviour, SerializeField won't show in Inspector.
    // We support an overridable ID via SetRewiredPlayerId; otherwise default 0.
    private int _cachedRewiredId = 0;
    public Rewired.Player rPlayer { get; protected set; }

    protected Vector2 moveInput;

    // Action names are strings; keep them in one place
    protected virtual string DashAction => "Dash";
    protected virtual string ThrustAction => "Thrust";
    protected virtual string ShardAction => "Shard";

    public PlayerState(Player player, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.player = player;

        anim = player.anim;
        rb = player.rb;
        input = player.input;
        stats = player.stats;
        skillManager = player.skillManager;
        mana = player.GetComponent<Entity_Mana>();
    }

    /// <summary>Call this from your Player when constructing states to override the default (0).</summary>
    public void SetRewiredPlayerId(int id) => _cachedRewiredId = Mathf.Max(0, id);

    public override void Enter()
    {
        base.Enter();
        EnsureRewiredPlayer();
    }

    public override void Update()
    {
        base.Update();

        if (!EnsureRewiredPlayer())
            return;

        // Axes
        xInput = rPlayer.GetAxis("Horizontal");
        yInput = rPlayer.GetAxis("Vertical");

        moveInput = new Vector2(xInput, yInput);
        if (moveInput.sqrMagnitude > 0.01f)
            player.lastMoveDirection = moveInput.normalized;

        // --- Skills ---
        // --- Skills ---
        // in PlayerState input:
        if (rPlayer.GetButtonDown(DashAction))
        {
            if (skillManager.dash.CanUseSkillCheck(out var why))
                stateMachine.ChangeState(player.dashState);
            else
                Debug.LogWarning($"Dash blocked: {why}");
        }

        if (rPlayer.GetButtonDown(ThrustAction))
        {
            if (skillManager.thrust.CanUseSkillCheck(out var why))
                stateMachine.ChangeState(player.thrustState);
            else
                Debug.LogWarning($"Thrust blocked: {why}");
        }



        if (rPlayer.GetButtonDown(ShardAction))
        {
            Debug.Log("[Input] Shard pressed");
            if (skillManager.shard != null)
                skillManager.shard.TryUseSkill();
            else
                Debug.LogError("[Skill] Shard component missing on player!");
        }
    }


    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();
    }

    private bool EnsureRewiredPlayer()
    {
        if (rPlayer != null) return true;

        try
        {
            // If your Player exposes a public 'rewiredPlayerId', use it; otherwise fall back to cached/default (0).
            int id = _cachedRewiredId;
            // If you *do* have player.rewiredPlayerId in your Player class, uncomment:
            // id = player != null ? player.rewiredPlayerId : _cachedRewiredId;

            rPlayer = ReInput.players.GetPlayer(id);
            if (rPlayer == null)
            {
                // Rewired not ready yet or wrong ID
                return false;
            }
            return true;
        }
        catch
        {
            // ReInput might not be initialized yet
            return false;
        }
    }

    private bool CanDash(out string reason)
    {
        reason = "";
        if (skillManager == null) { reason = "skillManager null"; return false; }
        if (skillManager.dash == null) { reason = "dash missing"; return false; }
        if (!skillManager.dash.CanUseSkill()) { reason = "dash on cooldown/locked"; return false; }
        if (stateMachine.currentState == player.dashState) { reason = "already dashing"; return false; }
        return true;
    }

    private bool CanThrust(out string reason)
    {
        reason = "";
        if (skillManager == null) { reason = "skillManager null"; return false; }
        if (skillManager.thrust == null) { reason = "thrust missing"; return false; }
        if (!skillManager.thrust.CanUseSkill()) { reason = "thrust on cooldown/locked"; return false; }
        if (stateMachine.currentState == player.thrustState) { reason = "already thrusting"; return false; }
        return true;
    }


}
