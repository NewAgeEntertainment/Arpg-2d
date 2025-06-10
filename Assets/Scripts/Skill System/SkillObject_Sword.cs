
using UnityEngine;

public class SkillObject_Sword : SkillObject_Base
{
    protected Skill_Sword swordmanager;
    private Transform target;
    private float speed;
    public Transform playerTransform { get; private set; }
    private Skill_Sword swordManager;

    private int maxDistance;
    private float attacksPerSecond;
    private float attackTimer;

    protected bool shouldComeback;
    protected float comebackSpeed = 20;
    protected float maxAllowedDistance = 25;

    protected virtual void Update()
    {
        //transform.right = rb.linearVelocity;
        //HandleComeback();

        if (target == null)
            return;

        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        HandleAttack();
        HandleStopping();
        HandleComeback();
    }




    public void MoveTowardsClosestTarget(float speed)
    {
        target = FindClosestTarget();
        this.speed = speed;
    }

    public void SetupSword(Skill_Sword swordManager)
    {
        this.swordManager = swordManager;

        playerStats = swordManager.player.stats;
        damageScaleData = swordManager.damageScaleData;

        anim?.SetTrigger("spin");

        maxDistance = swordManager.maxDistance;
        attacksPerSecond = swordManager.attacksPerSecond;

        Invoke(nameof(GetSwordBackToPlayer), swordManager.maxSpinDuration);
        

    }

    public void SetupSword(Skill_Sword swordManager, bool canMove, float swordSpeed)
    {
        this.swordManager = swordManager;
        playerStats = swordManager.player.stats;
        damageScaleData = swordManager.damageScaleData;
        
        anim?.SetTrigger("spin");

        maxDistance = swordManager.maxDistance;
        attacksPerSecond = swordManager.attacksPerSecond;

        Invoke(nameof(GetSwordBackToPlayer), swordManager.maxSpinDuration);



       
    }


    public void GetSwordBackToPlayer() => shouldComeback = true;

    protected void HandleComeback()
    {
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance > maxAllowedDistance)
            GetSwordBackToPlayer();

        if (shouldComeback == false)
            return;

        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, comebackSpeed * Time.deltaTime);

        if (distance < .5f)
            Destroy(gameObject);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        StopSword(collision);
        DamageEnemiesInRadius(transform, 1);
    }

    protected void StopSword(Collider2D collision)
    {
        rb.simulated = false;
        transform.parent = collision.transform;
    }

    private void HandleStopping()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > maxDistance && rb.simulated == true)
            rb.simulated = false;
    }

    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer < 0)
        {
            DamageEnemiesInRadius(transform, 1);
            attackTimer = 1 / attacksPerSecond;
        }
    }

    
}
