using System.Collections;
using UnityEngine;

public class Skill_AirPunch : Skill_Base
{
    [Header("Air Punch")]
    [SerializeField] private GameObject airPunchProjectilePrefab;
    [SerializeField] private float projectileSpeed = 9f;
    [SerializeField] private float spawnDistance = 0.75f;

    [Header("Ability Duration")]
    [SerializeField] private float activeDuration = 20f;
    private bool isActive;
    private Coroutine activeTimerCo;

    [Header("VFX")]
    [SerializeField] private GameObject castVfxPrefab;
    [SerializeField] private Vector3 castVfxOffset;

    public override void TryUseSkill()
    {
        if (!CanUseSkill())
            return;

        if (!Unlocked(SkillUpgradeType.AirPunch))
            return;

        ActivateAirPunch();
        SetSkillOnCooldown();
    }

    private void ActivateAirPunch()
    {
        isActive = true;

        if (activeTimerCo != null)
            StopCoroutine(activeTimerCo);

        activeTimerCo = StartCoroutine(AirPunchActiveTimerCo());

        Debug.Log($"[Skill_AirPunch] Air Punch active for {activeDuration} seconds.");
    }

    private IEnumerator AirPunchActiveTimerCo()
    {
        yield return new WaitForSeconds(activeDuration);

        isActive = false;
        activeTimerCo = null;

        Debug.Log("[Skill_AirPunch] Air Punch expired. Projectiles stopped.");
    }

    public void TryCreateProjectile(Vector2 direction)
    {
        if (!isActive)
            return;

        if (!Unlocked(SkillUpgradeType.AirPunch))
            return;

        if (airPunchProjectilePrefab == null)
        {
            Debug.LogWarning("[Skill_AirPunch] Missing airPunchProjectilePrefab.");
            return;
        }

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.down;

        direction.Normalize();

        Vector3 spawnPosition = player.transform.position + (Vector3)(direction * spawnDistance);

        if (castVfxPrefab != null)
        {
            Quaternion castRot = Quaternion.FromToRotation(Vector3.right, direction);
            Instantiate(castVfxPrefab, spawnPosition + castVfxOffset, castRot);
        }

        GameObject projectile = Instantiate(
            airPunchProjectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        SkillObject_AirPunch airPunch = projectile.GetComponent<SkillObject_AirPunch>();

        if (airPunch == null)
        {
            Debug.LogWarning("[Skill_AirPunch] Projectile prefab is missing SkillObject_AirPunch.");
            Destroy(projectile);
            return;
        }

        airPunch.SetupAirPunch(this, direction, projectileSpeed);
    }

    public bool IsActive()
    {
        return isActive;
    }
}