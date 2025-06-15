using System.Collections;
using UnityEngine;

public class Sex_ClimaxState : SexyTimeState
{

    public Sex_ClimaxState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine) : base(logic, stateMachine) { }

    public override void EnterState()
    {
        Debug.Log("Entering Climax State");

        logic.anim.Play("sperm shot", 0, 0f);
        logic.shouldPause = true;
        logic.cumReached = true;

        // Start coroutine to end sexy time after climax duration
        logic.StartCoroutine(ClimaxRoutine());
    }

    private IEnumerator ClimaxRoutine()
    {
        yield return new WaitForSeconds(logic.cumDuration);

        logic.shouldPause = false;
        logic.ResetSexyTime();

        stateMachine.ChangeState(new Sex_IdleState(logic, stateMachine));
    }

    public override void UpdateState() { } // Empty since coroutine handles everything

}

