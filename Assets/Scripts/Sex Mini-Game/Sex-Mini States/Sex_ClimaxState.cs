using System.Collections;
using UnityEngine;

public class Sex_ClimaxState : SexyTimeState
{
    public Sex_ClimaxState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine) { }

    public override void EnterState()
    {
        Debug.Log("Entering Climax State");

        logic.anim.Play("sperm shot", 0, 0f);
        logic.shouldPause = true;
        logic.cumReached = true;

        logic.StartCoroutine(ClimaxRoutine());
    }

    private IEnumerator ClimaxRoutine()
    {
        var ui = logic.UI;

        // Drain bars visually over time
        float drainDuration = 1.5f;
        float t = 0f;

        float startPlayer = ui.PlayerBarValue;
        float startPartner = ui.PartnerBarValue;
        float pMax = ui.PlayerBarMax;
        float partnerMax = ui.PartnerBarMax;

        while (t < drainDuration)
        {
            t += Time.deltaTime;
            float lerp = 1f - (t / drainDuration);
            ui.UpdateBars(startPlayer * lerp, pMax, startPartner * lerp, partnerMax);
            yield return null;
        }

        ui.UpdateBars(0f, pMax, 0f, partnerMax);

        // ✅ Wait for the 'sperm shot' animation to finish
        AnimatorStateInfo animState = logic.anim.GetCurrentAnimatorStateInfo(0);

        if (animState.IsName("sperm shot"))
        {
            float remainingTime = animState.length * (1f - animState.normalizedTime);
            yield return new WaitForSeconds(remainingTime);
        }
        else
        {
            Debug.LogWarning("[Sex_ClimaxState] Animation state mismatch; defaulting to delay.");
            yield return new WaitForSeconds(1f);
        }

        logic.shouldPause = false;

        // ✅ Close the mini-game cleanly
        logic.ResetSexyTime();
    }

    public override void UpdateState() { }
}
