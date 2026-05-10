using UnityEngine;

public class SkillObject_AirPunch : SkillObject_Base
{
    private Vector2 moveDirection;
    private float speed;
    private float lifeTimer;
    private bool alreadyHit;

    [Header("Air Punch Object")]
    [SerializeField] private float lifeTime = 1.25f;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Debug")]
    [SerializeField] private bool debugHits = true;

    [Header("VFX")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private GameObject destroyVfxPrefab;
    [SerializeField] private Vector3 hitVfxOffset;
    [SerializeField] private Vector3 destroyVfxOffset;

    public void SetupAirPunch(Skill_AirPunch airPunchSkill, Vector2 direction, float projectileSpeed)
    {
        playerStats = airPunchSkill.player.stats;
        damageScaleData = airPunchSkill.damageScaleData;

        moveDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.down;
        speed = projectileSpeed;
        lifeTimer = lifeTime;
        alreadyHit = false;

        transform.right = moveDirection;

        anim?.SetTrigger("airPunch");
    }

    private void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            SpawnDestroyVfx();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (debugHits)
        {
            Debug.Log(
                $"[AirPunch] Trigger hit: {collision.name}, " +
                $"Layer={LayerMask.LayerToName(collision.gameObject.layer)}, " +
                $"IsTrigger={collision.isTrigger}"
            );
        }

        if (alreadyHit)
            return;

        if (!IsInEnemyLayer(collision))
        {
            if (debugHits)
                Debug.Log($"[AirPunch] Ignored: {collision.name} is not in whatIsEnemy mask.");

            return;
        }

        IDamageable damageable =
            collision.GetComponent<IDamageable>() ??
            collision.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            if (debugHits)
                Debug.LogWarning($"[AirPunch] Hit {collision.name}, but no IDamageable found on it or parent.");

            return;
        }

        Entity_StatusHandler statusHandler =
            collision.GetComponent<Entity_StatusHandler>() ??
            collision.GetComponentInParent<Entity_StatusHandler>();

        AttackData attackData = playerStats.GetAttackData(damageScaleData);

        damageable.TakeDamage(
            attackData.physicalDamage,
            attackData.elementalDamage,
            attackData.element,
            transform
        );

        if (attackData.element != ElementType.None)
            statusHandler?.ApplyStatusEffect(attackData.element, attackData.effectData);

        alreadyHit = true;

        SpawnHitVfx();

        if (debugHits)
            Debug.Log($"[AirPunch] Damaged target through {collision.name}.");

        if (destroyOnHit)
            Destroy(gameObject);
    }

    private bool IsInEnemyLayer(Collider2D collision)
    {
        return (whatIsEnemy.value & (1 << collision.gameObject.layer)) != 0;
    }

    private void SpawnHitVfx()
    {
        if (hitVfxPrefab == null)
            return;

        Quaternion rot = Quaternion.FromToRotation(Vector3.right, moveDirection);
        Instantiate(hitVfxPrefab, transform.position + hitVfxOffset, rot);
    }

    private void SpawnDestroyVfx()
    {
        if (destroyVfxPrefab == null)
            return;

        Quaternion rot = Quaternion.FromToRotation(Vector3.right, moveDirection);
        Instantiate(destroyVfxPrefab, transform.position + destroyVfxOffset, rot);
    }
}