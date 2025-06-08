using UnityEngine;

public interface IDamageable // This interface defines a contract for any class that can take damage.
{

    public bool TakeDamage(float damage, float elementalDamage,ElementType element, Transform damageDealer);

}
