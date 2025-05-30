using System.Collections;
using UnityEngine;

public class Entity_StatusHandler : MonoBehaviour
{
    private Entity entity;
    private Entity_VFX entityVfx;
    private Entity_Stats entityStats;
    private Entity_Health entityHealth;
    private ElementType currentEffect = ElementType.None;

    [Header("Electrifiy effect details")]
    [SerializeField] private GameObject lightningStrikeVfx;
    [SerializeField] private float currentCharge;
    [SerializeField] private float maximumCharge = 1f;
    private Coroutine electrifyCo;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        entityHealth = GetComponent<Entity_Health>();
        entityStats = GetComponent<Entity_Stats>();
        entityVfx = GetComponent<Entity_VFX>();
    }

    public void ApplyElectrifyEffect(float duration, float damage, float Charge)
    {
        float lightningResistance = entityStats.GetElementalResistance(ElementType.Lightning);
        float finalCharge = Charge * (1 - lightningResistance); // Reduce the charge based on the lightning resistance

        currentCharge = currentCharge + finalCharge;
        
        if (currentCharge >= maximumCharge)
        {
            DoLightningStrike(damage); // Apply the damage to the entity's health
            StopElectrifyEffect();
            return; // Stop the effect if the charge is full
        }

        if (electrifyCo != null)
        {
            StopCoroutine(electrifyCo); // Stop the previous electrify effect coroutine if it's running
        }

        electrifyCo = StartCoroutine(ElectrifyEffectCo(duration)); // Start the electrify effect coroutine
    }

    private void StopElectrifyEffect()
    {
        currentEffect = ElementType.None; // Reset the current effect to None
        currentCharge = 0; // Reset the current charge to 0
        entityVfx.StopAllVfx(); // Stop all visual effects related to electrify
    }

    private void DoLightningStrike(float damage)
    {
        Instantiate(lightningStrikeVfx, entity.transform.position, Quaternion.identity);
        entityHealth.ReduceHealth(damage);
    }

    private IEnumerator ElectrifyEffectCo(float duration)
    {
        currentEffect = ElementType.Lightning; // Set the current effect to Lightning
        entityVfx.PlayOnStatusVfx(duration, ElementType.Lightning); // Play the lightning effect visual
        // Wait for the duration of the electrify effect
        yield return new WaitForSeconds(duration);
        StopElectrifyEffect(); // Stop the electrify effect after the duration is over

    }

    public void ApplyBurnEffect(float duration, float fireDamage)
    {
        // Get the fire resistance from the entity stats
        float fireResistance = entityStats.GetElementalResistance(ElementType.Fire);
        // Reduce the duration based on the fire resistance
        float reducedDuration = duration * (1 - fireResistance);
        float finalDamage = fireDamage * (1 - fireResistance); // Reduce the damage based on the fire resistance
        StartCoroutine(BurnEffectorCo(reducedDuration, finalDamage));
    }

    private IEnumerator BurnEffectorCo(float duration, float totalDamage)
    {
        // current affect is fire so no other effect can be applied
        currentEffect = ElementType.Fire;
        // Play the burn effect visual
        entityVfx.PlayOnStatusVfx(duration, ElementType.Fire);

        // Calculate the number of ticks and damage per tick(seconds)
        int ticksPerSecond = 4; // How many times the effect should tick per second
        // Calculate the total number of ticks based on the duration and ticks per second
        int tickCount = Mathf.RoundToInt(ticksPerSecond * duration); // Total number of ticks

        // "damagePerTick" is the total damage divided by the number of ticks
        float damagePerTick = totalDamage / tickCount; // Calculate damage per tick
        // Calculate the time interval between each tick
        float tickInterval = 1f / ticksPerSecond; // Time between each tick

        // Apply the burn effect over the duration using for each loop
        // for loop to apply the burn effect over the duration
        for (int i = 0; i < tickCount; i++)
        {// int i(tickInterval) = 0; i < tickCount; i++)
            
            // ReduceHp(damagePerTick);
            entityHealth.ReduceHealth(damagePerTick);
            // Wait for the tick interval(fire damage) before applying the next tick
            yield return new WaitForSeconds(tickInterval);
            // if tickCount is 0, break the loop
            // if tickCount Is not 0, countue the loop
        }

        currentEffect = ElementType.None; // Reset the effect after the duration
    
    }

    public void ApplyPoisonEffect(float duration, float poisonDamage)
    {
        // Get the poison resistance from the entity stats
        float poisonResistance = entityStats.GetElementalResistance(ElementType.Poison);
        // Reduce the duration based on the poison resistance
        float reducedDuration = duration * (1 - poisonResistance);
        float finalDamage = poisonDamage * (1 - poisonResistance); // Reduce the damage based on the poison resistance
        StartCoroutine(PoisonEffectCo(reducedDuration, finalDamage));
    }

    private IEnumerator PoisonEffectCo(float duration, float totalDamage)
    {
        currentEffect = ElementType.Poison;
        entityVfx.PlayOnStatusVfx(duration, ElementType.Poison);

        // Calculate the number of ticks and damage per tick(seconds)
        int ticksPerSecond = 4; // How many times the effect should tick per second
        // Calculate the total number of ticks based on the duration and ticks per second
        int tickCount = Mathf.RoundToInt(ticksPerSecond * duration); // Total number of ticks

        // "damagePerTick" is the total damage divided by the number of ticks
        float damagePerTick = totalDamage / tickCount; // Calculate damage per tick
        // Calculate the time interval between each tick
        float tickInterval = 1f / ticksPerSecond; // Time between each tick

        // Apply the burn effect over the duration using for each loop
        // for loop to apply the burn effect over the duration
        for (int i = 0; i < tickCount; i++)
        {// int i(tickInterval) = 0; i < tickCount; i++)

            // ReduceHp(damagePerTick);
            entityHealth.ReduceHealth(damagePerTick);
            // Wait for the tick interval(fire damage) before applying the next tick
            yield return new WaitForSeconds(tickInterval);
            // if tickCount is 0, break the loop
            // if tickCount Is not 0, countue the loop
        }

        currentEffect = ElementType.None; // Reset the effect after the duration

    }

    public void ApplyChillEffect(float duration, float slowMultiplier)
    {

        float iceResistance = entityStats.GetElementalResistance(ElementType.Ice);
        float reducedDuration = duration * (1 - iceResistance);

        StartCoroutine(ChillEffectCo(reducedDuration, slowMultiplier));
    }

    private IEnumerator ChillEffectCo(float duration, float slowMultiplier)
    {
        entity.SlowDownEntity(duration, slowMultiplier);
        currentEffect = ElementType.Ice;
        entityVfx.PlayOnStatusVfx(duration, ElementType.Ice);
        yield return new WaitForSeconds(duration);

        currentEffect = ElementType.None;
    }

    public bool CanBeApplied(ElementType element)
    {
        if (element == ElementType.Lightning && currentEffect == ElementType.Lightning)
            return true;

        return currentEffect == ElementType.None;
    }
}
