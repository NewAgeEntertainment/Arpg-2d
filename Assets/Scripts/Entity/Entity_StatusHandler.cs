using UnityEngine;

public class Entity_StatusHandler : MonoBehaviour
{
    private Entity entity;
    private ElementType currentEffect = ElementType.None;


    private void Awake()
    {
        entity = GetComponent<Entity>();
    }

    public void ApplyChilledEffect(float duration, float slowMultiplier)
    {
        entity.SlowDownEntity(duration, slowMultiplier);
    }

    public bool CanBeApplied(ElementType element)
    {
        return currentEffect == ElementType.None;
    }
}
