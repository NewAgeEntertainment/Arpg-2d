using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Return to Player
public class Companion_ReturnState : CompanionState
{
    public Companion_ReturnState(Companion c, StateMachine sm)
        : base(c, sm, "move") { }

    public override void Update()
    {
        base.Update();

        companion.MoveTo(companion.playerTarget.position);

        if (!companion.IsTooFarFromPlayer())
        {
            stateMachine.ChangeState(companion.followState);
        }
    }
}

