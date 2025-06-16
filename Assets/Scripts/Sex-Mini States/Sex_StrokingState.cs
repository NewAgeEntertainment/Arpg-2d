using UnityEngine;
using UnityEngine.UI;
using Rewired;

using UnityEngine;

public class Sex_StrokingState : SexyTimeState
{
    private float strokeCooldown = 0.2f;
    private float timeBetweenStrokes;
    private float lastStrokeTime = 1f;
   
    public Sex_StrokingState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine) { }

    public override void EnterState()
    {
        Debug.Log("Entered Stroking State");
        lastStrokeTime = Time.time;
    }

    public override void HandleStroke()
    {
        if (Time.time - lastStrokeTime < 0.2f) // optional cooldown
            return;

        lastStrokeTime = Time.time;

        AnimatorStateInfo stateInfo = logic.anim.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName("fuck") || stateInfo.normalizedTime >= 1f)
        {
            logic.anim.Play("fuck", 0, 0f);
        }

        ApplyStrokeBars();
    }

    private void ApplyStrokeBars()
    {
        AnimatorStateInfo stateInfo = logic.anim.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName("fuck"))
        {
            logic.anim.Play("fuck", 0, 0f);
        }

        logic.anim.SetFloat("speed", GetSpeed());

        // Get resilience multiplier (resilience capped in GetSexualResistance)
        float resilience = logic.playerStats.GetSexualResistance();
        float reductionMultiplier = 1f - (resilience / 100f);

        // Blue bar increase: arouselPerStroke reduced by resilience
        float blueIncrease = logic.arouselPerStroke * reductionMultiplier;
        logic.blueBar.value += blueIncrease;

        // Get stroke damage and check for crit
        bool isCrit = false;
        float strokeDamage = logic.playerStats.GetSexualDamage(out isCrit);

        // Pink bar increases by arouselPerStroke always
        logic.pinkBar.value += logic.arouselPerStroke;

        // If crit, increase pink bar more with strokeDamage * crit multiplier (e.g., 1.5x)
        if (isCrit)
        {
            logic.pinkBar.value += strokeDamage;
            // Show crit feedback here if needed

            // Show crit feedback, example:
            //if (logic.critText != null)
            //{
            //    logic.critText.text = "CRIT!";
            //    logic.critText.gameObject.SetActive(true);
            //}
        }
        else
        {
            // Normal strokeDamage addition
            logic.pinkBar.value += strokeDamage;

            //if (logic.critText != null)
            //    logic.critText.gameObject.SetActive(false);
        }

        // Clamp bars to max values
        logic.blueBar.value = Mathf.Min(logic.blueBar.value, logic.blueBarMaxValue);
        logic.pinkBar.value = Mathf.Min(logic.pinkBar.value, logic.pinkBarMaxValue);

        // Optional: update the "25 / 100" style bar text
        logic.UpdateBarText();

        // Climax trigger
        if (logic.blueBar.value >= logic.blueBarMaxValue || logic.pinkBar.value >= logic.pinkBarMaxValue)
        {
            logic.cumReached = true;
            logic.cumTimeElapsed = 0f;
            stateMachine.ChangeState(new Sex_ClimaxState(logic, stateMachine));
        }

        // Milestone triggers
        if (logic.blueBar.value >= logic.blueBarMaxValue && !logic.blueBarReachedOnce)
        {
            logic.blueBarReachedOnce = true;
            logic.OnBlueBarFull?.Invoke();
        }

        if (logic.pinkBar.value >= logic.pinkBarMaxValue && !logic.pinkBarReachedOnce)
        {
            logic.pinkBarReachedOnce = true;
            logic.OnPinkBarFull?.Invoke();
        }
    }





    private float GetSpeed()
    {
        if (timeBetweenStrokes >= 0.5f)
            return 1f;
        else if (timeBetweenStrokes >= 0.25f)
            return 1.25f;
        else if (timeBetweenStrokes >= 0.15f)
            return 1.5f;
        else
            return 2f;
    }

    public override void UpdateState()
    {
        // Optional: add passive drain or time logic here
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Stroking State");
    }
}


