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
            logic.anim.Play("fuck", 0, 0f);

        logic.anim.SetFloat("speed", GetSpeed());

        float maxArousal = logic.playerStats.GetMaxArousel();
        float strokeGain = logic.playerStats.GetBlueBarStrokeValue();

        float resilience = logic.playerStats.GetResilienceMitigation(0f);
        float reductionMultiplier = 1f - (resilience / 100f);
        float blueIncrease = logic.arouselPerStroke * reductionMultiplier;

        float totalGain = strokeGain + blueIncrease;

        float oldValue = logic.currentArousal;
        logic.currentArousal = Mathf.Min(logic.currentArousal + totalGain, maxArousal);
        logic.playerBar.value = logic.currentArousal;

        Debug.Log($"[STROKE] StrokeGain: {strokeGain}, BlueIncrease: {blueIncrease}, TotalGain: {totalGain}");
        Debug.Log($"[STROKE] Arousal: {oldValue} → {logic.currentArousal} (max: {maxArousal})");

        if (logic.playerBarFillText != null)
            logic.playerBarFillText.text = $"{Mathf.FloorToInt(logic.currentArousal)} / {Mathf.FloorToInt(maxArousal)}";

        // Handle partner bar fill
        bool isCrit = false;
        float strokeDamage = logic.playerStats.GetSexualDamage(out isCrit);

        float partnerOldValue = logic.partnerBar.value;
        float partnerIncrease = logic.arouselPerStroke + strokeDamage;
        logic.partnerBar.value += partnerIncrease;
        logic.partnerBar.value = Mathf.Min(logic.partnerBar.value, logic.partnerMaxArousalValue);

        Debug.Log($"[PARTNER] Bar: {partnerOldValue} → {logic.partnerBar.value} (added: {partnerIncrease})");

        logic.UpdateBarText();

        // Handle climax
        if (logic.playerBar.value >= maxArousal || logic.partnerBar.value >= logic.partnerMaxArousalValue)
        {
            logic.cumReached = true;
            logic.cumTimeElapsed = 0f;
            stateMachine.ChangeState(new Sex_ClimaxState(logic, stateMachine));
        }

        if (logic.playerBar.value >= maxArousal && !logic.playerBarReachedOnce)
        {
            logic.playerBarReachedOnce = true;
            logic.OnPlayerBarFull?.Invoke();
        }

        if (logic.partnerBar.value >= logic.partnerMaxArousalValue && !logic.partnerBarReachedOnce)
        {
            logic.partnerBarReachedOnce = true;
            logic.OnPartnerBarFull?.Invoke();
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


