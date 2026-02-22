using System.Collections;
using UnityEngine;

public class Sex_ClimaxState : SexyTimeState
{
    public Sex_ClimaxState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine) { }

    public override void EnterState()
    {
        Debug.Log("Entering Climax State");

        logic.cumReached = true;
        logic.shouldPause = true;

        if (logic.anim != null)
        {
            logic.anim.speed = 1f;                 // ✅ force normal speed for climax
            logic.anim.Play("sperm shot", 0, 0f);
        }

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
            if (logic.anim != null) logic.anim.speed = 1f;
        }

        ui.UpdateBars(0f, pMax, 0f, partnerMax);

        // ✅ Wait a frame so Animator state is correct
        yield return null;

        // ✅ Wait for animation to finish (more reliable)
        if (logic.anim != null)
        {
            AnimatorStateInfo st = logic.anim.GetCurrentAnimatorStateInfo(0);

            // If we didn't land in the state yet, give it up to a few frames:
            int safetyFrames = 5;
            while (!st.IsName("sperm shot") && safetyFrames-- > 0)
            {
                yield return null;
                st = logic.anim.GetCurrentAnimatorStateInfo(0);
            }

            if (st.IsName("sperm shot"))
            {
                float remaining = st.length * (1f - st.normalizedTime);
                yield return new WaitForSeconds(remaining);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        logic.shouldPause = false;

        logic.shouldPause = false;
        stateMachine.ChangeState(new Sex_FinishState(logic, stateMachine));
    }

    public override void UpdateState() { }
}