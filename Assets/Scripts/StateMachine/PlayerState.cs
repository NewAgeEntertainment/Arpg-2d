using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Rewired; // Ensure you have the Rewired package installed for input handling

public abstract class PlayerState : EntityState
{

    protected Player player;
    protected PlayerInputSet input;
    protected Player_SkillManager skillManager;
    protected Entity_Mana mana; // Reference to the Entity_Mana component for mana management

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
        skillManager = player.skillManager;
        mana = player.GetComponent<Entity_Mana>(); // Get the Entity_Mana component from the player
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
        {
            skillManager.dash.SetSkillOnCooldown();
            stateMachine.ChangeState(player.dashState);
        } 

        if (rPlayer.GetButtonDown("Thrust") && CanThrust())
        {
            
            skillManager.thrust.SetSkillOnCooldown();
            stateMachine.ChangeState(player.thrustState);
        }
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();
    }

    private bool CanDash()
    {
        if (skillManager.dash.CanUseSkill() == false)
            return false;

        if (stateMachine.currentState == player.dashState)
            return false;

        return true;
    }

    private bool CanThrust()
    {
        //if (mana.UseMana(skillManager.thrust.manaCost) == false)
        //    return false; // Check if there is enough mana to use the thrust skill

        if (skillManager.thrust.CanUseSkill() == false)
            return false;

        if (stateMachine.currentState == player.thrustState)
            return false;

        return true;
    }
}
