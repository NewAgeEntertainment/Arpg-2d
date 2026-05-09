using UnityEngine;
using Rewired;

public abstract class PlayerState : EntityState
{
    protected Player player;
    protected PlayerInputSet input;
    protected Player_SkillManager skillManager;
    protected Entity_Mana mana;

    // Rewired
    private int _cachedRewiredId = 0;
    public Rewired.Player rPlayer { get; protected set; }

    // Movement input (filled every Update by this base class)
    protected Vector2 moveInput;

    // Action names (match your Rewired setup)
    protected virtual string HorizontalAction => "Horizontal";
    protected virtual string VerticalAction => "Vertical";
    protected virtual string DashAction => "Dash";      // <-- standalone Dash here
    protected virtual string ThrustAction => "Thrust";
    protected virtual string ShardAction => "Shard";

    protected virtual string SkillModifierAction => "SkillModifier";

    public PlayerState(Player player, StateMachine stateMachine, string animBoolName)
        : base(stateMachine, animBoolName)
    {
        this.player = player;

        anim = player.anim;
        rb = player.rb;
        stats = player.stats;

        input = player.input;
        skillManager = player.skillManager;
        mana = player.GetComponent<Entity_Mana>();
    }

    /// Call when constructing states from Player to set the Rewired player id.
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

        // ----- Read processed axes from Rewired (deadzone handled in Input Behavior) -----
        float x = GetAxisSafe(rPlayer, HorizontalAction);
        float y = GetAxisSafe(rPlayer, VerticalAction);

        moveInput = new Vector2(x, y);
        player.moveInput = moveInput; // mirror for other systems/states

        // Keep facing fresh when there is meaningful input.
        // Locks facing to only up, down, left, or right.
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            {
                player.lastMoveDirection = moveInput.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                player.lastMoveDirection = moveInput.y > 0 ? Vector2.up : Vector2.down;
            }
        }

        bool skillModifierHeld = rPlayer.GetButton(SkillModifierAction);


        // ----- Standalone Dash (no modifier) -----
        if (rPlayer.GetButtonDown(DashAction))
        {
            if (skillManager != null && skillManager.dash != null && skillManager.dash.CanUseSkillCheck(out _))
                stateMachine.ChangeState(player.dashState);
        }

        // Optional: Thrust on its own button too (keep if you like; otherwise remove)
        if (rPlayer.GetButtonDown(ThrustAction))
        {
            if (skillManager != null && skillManager.thrust != null && skillManager.thrust.CanUseSkillCheck(out _))
                stateMachine.ChangeState(player.thrustState);
        }

        // Example instant skill (no state): shard fires right away
        if (rPlayer.GetButtonDown(ShardAction))
        {
            if (skillManager != null && skillManager.shard != null)
                skillManager.shard.TryUseSkill();
            else
                Debug.LogError("[Skill] Shard component missing on player!");
        }
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();
        // states that need to write animator floats should do so explicitly
    }

    // ---------------- Helpers ----------------

    private bool EnsureRewiredPlayer()
    {
        if (rPlayer != null) return true;

        try
        {
            int id = (player != null ? player.rewiredPlayerId : _cachedRewiredId);
            rPlayer = ReInput.players.GetPlayer(id);
            return rPlayer != null;
        }
        catch
        {
            return false; // ReInput not ready yet
        }
    }

    private static float GetAxisSafe(Rewired.Player p, string actionName)
    {
        if (p == null || string.IsNullOrEmpty(actionName)) return 0f;
        int actionId = ReInput.mapping.GetActionId(actionName);
        if (actionId < 0) return 0f;
        return p.GetAxis(actionId); // processed axis (uses Rewired deadzone/sensitivity)
    }
}
