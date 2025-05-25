using UnityEngine;

public class Player_CounterAttackState : PlayerState
{
    private Player_Combat combat;
    private bool counteredSomebody; // Flag to check if a counter attack was performed

    public Player_CounterAttackState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        combat = player.GetComponent<Player_Combat>(); // Get the Player_Combat component from the player
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = combat.GetCounterRecoveryDuration(); // Set the state timer to the duration of the counter attack
        counteredSomebody = combat.CounterAttackPerformed(); // Reset the countered flag when entering the state.Use the counter attack performed method from Player_Combat to check if a counter was performed
       
        anim.SetBool("counterAttackPerformed", counteredSomebody); // Reset the counter attack performed animation parameter

    }


    public override void Update()
    {
        base.Update();

        player.SetVelocity(0, 0);

        if (triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }

        if (stateTimer < 0 && counteredSomebody == false)
        {
            stateMachine.ChangeState(player.idleState);
        }
        
    }
        

        

}
