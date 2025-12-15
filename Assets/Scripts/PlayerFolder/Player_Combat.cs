using System.Collections.Generic;
using UnityEngine;

public class Player_Combat : Entity_Combat
{
    [Header("Counter Attack details")]
    [SerializeField] private float counterRecovery = .1f;

    [Header("Hit Check Points")]
    [SerializeField] private Transform _targetCheck_Left;
    [SerializeField] private Transform _targetCheck_Right;
    [SerializeField] private Transform _targetCheck_Up;
    [SerializeField] private Transform _targetCheck_Down;

    public bool CounterAttackPerformed()
    {
        bool hasPerformedCounter = false;

        foreach (Collider2D target in GetDetectedCollider())
        {
            if (!target.TryGetComponent(out ICounterable counterable))
                continue;

            if (counterable.CanBeCountered)
            {
                counterable.HandleCounter();
                hasPerformedCounter = true;
            }
        }

        return hasPerformedCounter;
    }

    public float GetCounterRecoveryDuration() => counterRecovery;

    // =====================================================================
    // Hit detection used by PerformAttack() (called from animation event)
    // =====================================================================
    public override Collider2D[] GetDetectedCollider()
    {
        List<Collider2D> detected = new List<Collider2D>();

        Transform hitOrigin = GetTargetTransform();
        if (hitOrigin == null)
            hitOrigin = transform;

        var colliders = Physics2D.OverlapCircleAll(
            hitOrigin.position,
            targetCheckRadius,
            whatIsTarget
        );

        if (colliders != null && colliders.Length > 0)
        {
            foreach (var c in colliders)
            {
                if (c == null) continue;
                if (!detected.Contains(c))
                    detected.Add(c);
            }
        }

        return detected.ToArray();
    }

    private Transform GetTargetTransform()
    {
        Vector2 dir = _entity.currentDir;

        if (dir.sqrMagnitude < 0.001f)
            return _targetCheck_Down != null ? _targetCheck_Down : transform;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) // Horizontal
            return dir.x < 0f ? _targetCheck_Left : _targetCheck_Right;
        else                                     // Vertical
            return dir.y < 0f ? _targetCheck_Down : _targetCheck_Up;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Transform[] targetChecks =
        {
            _targetCheck_Left,
            _targetCheck_Right,
            _targetCheck_Up,
            _targetCheck_Down
        };

        if (targetChecks == null) return;

        foreach (Transform check in targetChecks)
        {
            if (check == null) continue;
            Gizmos.DrawWireSphere(check.position, targetCheckRadius);
        }
    }

    public Transform GetSoftAimTarget(Vector2 origin, Vector2 forward, float maxAngleDeg, float maxRange)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, maxRange, whatIsTarget);
        if (hits == null || hits.Length == 0) return null;

        Transform best = null;
        float bestScore = float.NegativeInfinity;

        foreach (var h in hits)
        {
            if (h == null) continue;

            Vector2 to = (Vector2)h.transform.position - origin;
            float dist = to.magnitude;
            if (dist < 0.001f) continue;

            to /= dist;

            float dot = Vector2.Dot(forward, to);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

            if (angle > maxAngleDeg)
                continue;

            float score = dot * 2f + (1f - Mathf.Clamp01(dist / maxRange));

            if (score > bestScore)
            {
                bestScore = score;
                best = h.transform;
            }
        }

        return best;
    }

    // 🔹 Player-only mana restoration on successful hit
    // 🔹 Player-only mana restoration on successful hit + debug logging
    protected override void OnSuccessfulHit(Collider2D target, AttackData attackData)
    {
        base.OnSuccessfulHit(target, attackData);

        // Only the Player should restore mana on hit
        if (_entity is Player player && player.mana != null)
        {
            float before = player.mana.GetCurrentMana();

            player.mana.RestoreManaOnHitWithScaling(player.Level);

            float after = player.mana.GetCurrentMana();

            Debug.Log(
                $"[Player_Combat] Successful hit on '{target.name}'. " +
                $"Mana: {before} -> {after} (Level {player.Level})"
            );
        }
        else
        {
            Debug.Log("[Player_Combat] OnSuccessfulHit fired, but no Player/mana found. " +
                      $"Entity type: {_entity?.GetType().Name}");
        }
    }

}
