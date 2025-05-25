using UnityEngine;

public interface IDamagable // This interface defines a contract for any class that can take damage.
{

    public void TakeDamage(float damage, Transform damageDealer);

}
