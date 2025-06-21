using UnityEngine;

public class Sex_StrokingState : SexyTimeState
{
    private float strokeCooldown = 0.2f;
    private float lastStrokeTime;

    public Sex_StrokingState(SexyTimeLogic logic, SexyTimeStateMachine stateMachine)
        : base(logic, stateMachine)
    {
    }

    public override void EnterState()
    {
        
        lastStrokeTime = Time.time;
    }

    public override void HandleStroke()
    {
        if (Time.time - lastStrokeTime < strokeCooldown)
            return;

        lastStrokeTime = Time.time;

        AnimatorStateInfo stateInfo = logic.anim.GetCurrentAnimatorStateInfo(0);

        // Restart "fuck" animation if not playing or completed
        if (!stateInfo.IsName("fuck") || stateInfo.normalizedTime >= 1f)
        {
            logic.anim.Play("fuck", 0, 0f);
        }

        ApplyStrokeBars();
    }

    private void ApplyStrokeBars()
    {
        logic.anim.SetFloat("speed", GetSpeed());

        float maxArousal = logic.playerStats.sex.maxArousal.GetValue();
        float strokeGain = logic.playerStats.GetBlueBarStrokeValue();
        Debug.Log($"partnerBar is {(logic.partnerBar == null ? "NULL ❌" : "set ✅")}");

        // Apply resilience mitigation
        float resilience = logic.playerStats.GetResilienceMitigation(0f);
        float reductionMultiplier = 1f - (resilience / 100f);

        float blueIncrease = logic.arouselPerStroke * reductionMultiplier;

        // Total increase to player bar this stroke
        float totalGain = strokeGain + blueIncrease;

        float oldArousal = logic.currentArousal;
        logic.currentArousal = Mathf.Min(logic.currentArousal + totalGain, maxArousal);
        logic.playerBar.value = logic.currentArousal;

        

        if (logic.playerBarFillText != null)
            logic.playerBarFillText.text = $"{Mathf.FloorToInt(logic.currentArousal)} / {Mathf.FloorToInt(maxArousal)}";

        // Handle partner bar fill
        bool isCrit = false;
        float strokeDamage = logic.playerStats.GetSexualDamage(out isCrit);

        float partnerOldValue = logic.partnerBar.value;
        float partnerResilience = logic.partnerStats?.GetResilienceMitigation(0f) ?? 0f;
        float resilienceMultiplier = 1f - (partnerResilience / 100f);

        
        //// Optional: Sensitivity boost
        //float sensitivityBonus = logic.partnerStats?.sex.sensitivity.GetValue() ?? 1f;

        float baseGain = logic.arouselPerStroke + strokeDamage;
        float partnerIncrease = baseGain * resilienceMultiplier;// * sensitivityBonus;

        logic.partnerBar.value = Mathf.Min(logic.partnerBar.value + partnerIncrease, logic.partnerBar.maxValue);
        

        //Debug.Log($"[PARTNER] Bar: {partnerOldValue} → {logic.partnerBar.value} (added: {partnerIncrease})");

        if (logic.partnerBarFillText != null)
        {
            float partnerMax = logic.partnerStats?.sex.maxArousal.GetValue() ?? logic.partnerBar.maxValue;
            

            logic.partnerBarFillText.text = $"{Mathf.FloorToInt(logic.partnerBar.value)} / {Mathf.FloorToInt(partnerMax)}";
            Debug.Log($"📢 FillText Updated: {logic.partnerBarFillText.text}");
        }


        // Check for climax
        if (logic.playerBar.value >= maxArousal || logic.partnerBar.value >= logic.partnerMaxArousalValue)
        {
            logic.cumReached = true;
            logic.cumTimeElapsed = 0f;
            stateMachine.ChangeState(new Sex_ClimaxState(logic, stateMachine));
        }

        // Milestone events
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
        // Example speed logic based on cooldown
        float timeSinceLastStroke = Time.time - lastStrokeTime;

        if (timeSinceLastStroke >= 0.5f)
            return 1f;
        else if (timeSinceLastStroke >= 0.25f)
            return 1.25f;
        else if (timeSinceLastStroke >= 0.15f)
            return 1.5f;
        else
            return 2f;
    }

    public override void UpdateState()
    {
        // Add any continuous behavior here if needed
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Stroking State");
    }
}
