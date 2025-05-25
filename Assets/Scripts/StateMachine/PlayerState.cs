using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class PlayerState : EntityState
{
    //protected float xInput; // Use 'new' keyword to explicitly hide the inherited member
    //protected float yInput; // Use 'new' keyword to explicitly hide the inherited member

    protected Player player;
    protected PlayerInputSet input;

    public PlayerState(Player player, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.player = player;

        anim = player.anim;
        rb = player.rb;
        input = player.input;
    }

    public override void Update()
    {
        base.Update();
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        // this is the new input system.
        //if (input.Player.Dash.WasPressedThisFrame() && CanDash())
        //    stateMachine.ChangeState(player.dashState);

        /// this is the old input system.
        if (Input.GetKeyDown(KeyCode.F) && CanDash())
            stateMachine.ChangeState(player.dashState);

        if (Input.GetKeyDown(KeyCode.G) && CanThrust())
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
