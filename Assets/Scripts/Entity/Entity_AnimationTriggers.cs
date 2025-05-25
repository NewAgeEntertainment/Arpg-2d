using UnityEngine;

public class Entity_AnimationTriggers : MonoBehaviour
{
    private Entity entity;
    private Entity_Combat entityCombat;
    private GameObject targetCheck; // Changed from Transform[] to GameObject for proper usage with SetActive

    protected virtual void Awake()
    {
        entity = GetComponentInParent<Entity>();
        entityCombat = GetComponentInParent<Entity_Combat>();

        
        
    }

    private void CurrentStateTrigger()
    {
        entity.CurrentStateAnimationTrigger();
    }

    private void AttackTrigger()
    {
        
        // This method is called when the attack animation is triggered
       entityCombat.PerformAttack();
        
        





    }
}
