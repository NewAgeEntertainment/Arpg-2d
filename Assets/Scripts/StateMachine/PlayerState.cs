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

    public float attackSpeed { get; protected set; } // Attack speed multiplier for the player
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

    // The error CS0115 indicates that the method `Start` in `PlayerState` is attempting to override a method that does not exist in its base class `EntityState`.  
    // To fix this, we need to remove the `override` keyword from the `Start` method in `PlayerState`.  

    

    public override void Enter()
    {
        base.Enter();
        rPlayer = Rewired.ReInput.players.GetPlayer(playerID);
    }


    public override void Update()
    {
        base.Update();

        // Get input values  
        xInput = rPlayer.GetAxis("Horizontal");
        yInput = rPlayer.GetAxis("Vertical");

        //// Update player's moveInput  
        moveInput = new Vector2(xInput, yInput);

        Vector2 input = new Vector2(xInput, yInput);
        if (input.sqrMagnitude > 0.01f)
        {
            player.lastMoveDirection = input.normalized;
        }
        
        //if (input.Player.Dash.WasPressedThisFrame() && CanDash())
        //{
        //    skillManager.dash.SetSkillOnCooldown();
        //    stateMachine.ChangeState(player.dashState);
        //}

        // Handle skill inputs  
        if (rPlayer.GetButtonDown("Dash") && CanDash())
        {
            skillManager.dash.SetSkillOnCooldown();
            stateMachine.ChangeState(player.dashState);
        }

        if (rPlayer.GetButtonDown("Thrust") && CanThrust())
        {
            stateMachine.ChangeState(player.thrustState);
        }

        if (rPlayer.GetButtonDown("Shard"))
        {
            skillManager.shard.TryUseSkill();
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
        Debug.Log("Can Dash: true");
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
