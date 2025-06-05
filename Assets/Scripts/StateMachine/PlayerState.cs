using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Rewired; // Ensure you have the Rewired package installed for input handling

public abstract class PlayerState : EntityState
{

    protected Player player;
    protected PlayerInputSet input;

    [SerializeField] private int playerID = 0; // Player ID for multiplayer support    
    public Rewired.Player rPlayer {  get; protected set; }

    protected Vector2 moveInput; // Declare moveInput to fix CS0103    

    public PlayerState(Player player, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.player = player;

        anim = player.anim;
        rb = player.rb;
        input = player.input;
        stats = player.stats; // Get the Entity_Stats component from the player    
    }

    public override void Enter()
    {
        base.Enter();
        rPlayer = Rewired.ReInput.players.GetPlayer(playerID);
    }


    public override void Update()
    {
        base.Update();
        xInput = rPlayer.GetAxis("Horizontal");
        yInput = rPlayer.GetAxis("Vertical");

        if (rPlayer.GetButtonDown("Dash") && CanDash())
            stateMachine.ChangeState(player.dashState);

        if (rPlayer.GetButtonDown("Thrust") && CanThrust())
            stateMachine.ChangeState(player.thrustState);
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();
    }

    private bool CanDash()
    {
        if (stateMachine.currentState == player.dashState)
            return false;

        return true;
    }

    private bool CanThrust()
    {
        if (stateMachine.currentState == player.thrustState)
            return false;

        return true;
    }
}
