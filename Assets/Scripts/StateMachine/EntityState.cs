using UnityEngine;
using UnityEngine.Windows;
using Rewired; // Ensure you have the Rewired package installed for input handling

public abstract class EntityState 
{
    protected StateMachine stateMachine;
    protected string animBoolName;

    protected float xInput; // Use 'new' keyword to explicitly hide the inherited member
    protected float yInput; // Use 'new' keyword to explicitly hide the inherited member

    protected Animator anim;
    protected Rigidbody2D rb;
    protected Entity_Stats stats;



    protected float stateTimer;
    protected bool triggerCalled;

    public EntityState(StateMachine stateMachine, string animBoolName)
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        anim.SetBool(animBoolName, true);
        triggerCalled = false;
    }

    public virtual void Update()
    {
        
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();
    }

    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);
    }

    public void AnimationTrigger()
    {
        triggerCalled = true;
    }

    public virtual void UpdateAnimationParameters()
    {
        
        
    }

     public virtual void SyncAttackSpeed()
    {
        if (anim == null)
        {
            Debug.LogWarning("[SyncAttackSpeed] Animator is null!");
            return;
        }

        if (stats == null)
        {
            Debug.LogWarning("[SyncAttackSpeed] Stats is null!");
            anim.SetFloat("attackSpeedMultiplier", 1f);
            return;
        }

        float attackSpeed = stats.offense.attackSpeed.GetValue();

        if (attackSpeed <= 0f)
        {
            Debug.LogWarning($"[SyncAttackSpeed] attackSpeed was {attackSpeed}, forcing to 1");
            attackSpeed = 1f;
        }

        anim.SetFloat("attackSpeedMultiplier", attackSpeed);
        Debug.Log($"[SyncAttackSpeed] Set to: {attackSpeed}");
    }
}
