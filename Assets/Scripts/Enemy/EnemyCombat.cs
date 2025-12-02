using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : Entity_Combat
{
    [Header("Target Checks (like Player)")]
    [SerializeField] private Transform _targetCheck_Left;
    [SerializeField] private Transform _targetCheck_Right;
    [SerializeField] private Transform _targetCheck_Up;
    [SerializeField] private Transform _targetCheck_Down;

    private Enemy enemy;

    private void Awake()
    {
        _entity = GetComponent<Entity>();
        vfx = GetComponent<Entity_VFX>();
        sfx = GetComponent<Entity_SFX>();
        mana = GetComponent<Entity_Mana>();

        // First try on the same GameObject
        stats = GetComponent<Entity_Stats>();

        // Fallback: look in children if not found on root
        if (stats == null)
        {
            stats = GetComponentInChildren<Entity_Stats>();
        }

        if (stats == null)
        {
            Debug.LogError($"[Entity_Combat] No Entity_Stats found for {name}. " +
                           $"This enemy will do 0 damage until you add Enemy_Stats/Entity_Stats.");
        }
    }


    private void Update()
    {
        // If you want to debug/visualize, keep this empty or add extra logic here.
        // Targeting is handled by GetTargetTransform() like Player_Combat.
    }

    public override Collider2D[] GetDetectedCollider()
    {
        List<Collider2D> detected = new List<Collider2D>();

        Transform origin = GetTargetTransform();
        if (origin == null)
            return detected.ToArray();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            origin.position,
            targetCheckRadius,
            whatIsTarget
        );

        detected.AddRange(colliders);
        return detected.ToArray();
    }

    private Transform GetTargetTransform()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        Vector2 dir = enemy.currentDir;

        // If enemy is “idle”, fall back to last facing
        if (dir.sqrMagnitude < 0.0001f)
            dir = enemy.LastDir;

        // Horizontal dominant
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x < 0 ? _targetCheck_Left : _targetCheck_Right;

        // Vertical dominant
        if (Mathf.Abs(dir.y) > 0)
            return dir.y < 0 ? _targetCheck_Down : _targetCheck_Up;

        // Fallback
        return _targetCheck_Down;
    }

    private void OnDrawGizmos()
    {
        Transform[] tChecks = { _targetCheck_Left, _targetCheck_Right, _targetCheck_Up, _targetCheck_Down };

        if (tChecks == null) return;

        Gizmos.color = Color.red;
        foreach (Transform check in tChecks)
        {
            if (check != null)
                Gizmos.DrawWireSphere(check.position, targetCheckRadius);
        }
    }
}
