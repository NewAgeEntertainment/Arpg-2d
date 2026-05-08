using UnityEngine;

public class SkillObject_AirPunch : SkillObject_Base
{
    private Vector2 moveDirection;
    private float speed;
    private float lifeTimer;

    [Header("Air Punch Object")]
    [SerializeField] private float lifeTime = 1.25f;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private bool destroyOnHit = true;

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
        IDamageable damageable = collision.GetComponent<IDamageable>();

        if (damageable == null)
            return;

        DamageEnemiesInRadius(transform, hitRadius);
        SpawnHitVfx();

        if (destroyOnHit)
            Destroy(gameObject);
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