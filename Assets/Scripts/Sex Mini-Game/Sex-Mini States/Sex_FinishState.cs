using System.Collections;
using UnityEngine;

public class Sex_FinishState : SexyTimeState
{
    public Sex_FinishState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine) : base(logic, stateMachine) { }

    public override void EnterState()
    {
        logic.StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        yield return new WaitForSeconds(0.75f);

        logic.ResetSexyTime();
    }



    public override void UpdateState() { }
    public override void ExitState() { }
    public override void HandleStroke() { }
    public override void HandleDeepBreathe() { }
}
