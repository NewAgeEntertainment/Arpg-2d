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

    public float GetCounterRecoveryDuration()
    {
        return counterRecovery;
    }

    // =====================================================================
    // Hit detection used by PerformAttack() (called from animation event)
    // =====================================================================
    public override Collider2D[] GetDetectedCollider()
    {
        List<Collider2D> detected = new List<Collider2D>();

        Transform hitOrigin = GetTargetTransform();
        if (hitOrigin == null)
            hitOrigin = transform; // very defensive fallback

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

    /// <summary>
    /// Picks which hit point to use based on the entity's current facing.
    /// </summary>
    private Transform GetTargetTransform()
    {
        Vector2 dir = _entity.currentDir;

        // If currentDir somehow ended up zero, be forgiving and use "down" as default.
        if (dir.sqrMagnitude < 0.001f)
            return _targetCheck_Down != null ? _targetCheck_Down : transform;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) // Horizontal dominant
        {
            return dir.x < 0f ? _targetCheck_Left : _targetCheck_Right;
        }
        else                                   // Vertical dominant
        {
            return dir.y < 0f ? _targetCheck_Down : _targetCheck_Up;
        }
    }

    // Just for editor visualization of all four possible hit origins.
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, maxRange, whatIsTarget); // use your combat layer mask
        if (hits == null || hits.Length == 0) return null;

        Transform best = null;
        float bestScore = float.NegativeInfinity;

        foreach (var h in hits)
        {
            if (h == null) continue;

            Vector2 to = (Vector2)h.transform.position - origin;
            float dist = to.magnitude;
            if (dist < 0.001f) continue;

            to /= dist; // normalize

            // angle between our forward and this target
            float dot = Vector2.Dot(forward, to); // cos(theta)
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

            if (angle > maxAngleDeg)
                continue; // too far off to the side

            // Score = "how straight ahead + how close"
            float score = dot * 2f + (1f - Mathf.Clamp01(dist / maxRange));

            if (score > bestScore)
            {
                bestScore = score;
                best = h.transform;
            }
        }

        return best;
    }

}
