using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("NPC/Follow/NPC Follow Trigger")]
[RequireComponent(typeof(Collider2D))]
public class NPCFollowTrigger : MonoBehaviour
{
    public enum FollowAction { Start, Stop, Toggle }
    public enum FirePhase { OnEnter, OnExit, Both }

    [Header("Trigger Filter")]
    [Tooltip("Only fire when a collider with this tag enters/exits. Usually 'Player'. Leave empty to accept anyone.")]
    public string requiredTag = "Player";

    [Header("When To Fire")]
    public FirePhase firePhase = FirePhase.OnEnter;

    [Header("What To Do")]
    public FollowAction action = FollowAction.Start;

    [Header("Who Follows (choose one)")]
    [Tooltip("Optional: drag the NPCIdentity from the scene/prefab to use its id.")]
    public NPCIdentity npcIdentity;
    [Tooltip("Fallback / direct id. If npcIdentity is assigned, that id is used instead.")]
    public string npcId;

    [Header("Options")]
    [Tooltip("Only allow this trigger to fire once.")]
    public bool oneShot = false;
    [Tooltip("Minimum time (seconds) between firings to prevent spam.")]
    [Min(0f)] public float cooldown = 0f;

    [Header("Debug")]
    public bool log = false;

    private float _nextAllowedTime = 0f;
    private bool _hasFiredOnce = false;

    private void Reset()
    {
        // Ensure it's a trigger collider (2D)
        var col2d = GetComponent<Collider2D>();
        if (col2d) col2d.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (firePhase == FirePhase.OnEnter || firePhase == FirePhase.Both)
            TryFire(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (firePhase == FirePhase.OnExit || firePhase == FirePhase.Both)
            TryFire(other.gameObject);
    }

    private void TryFire(GameObject other)
    {
        if (oneShot && _hasFiredOnce) return;
        if (Time.time < _nextAllowedTime) return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        string id = ResolveId();
        if (string.IsNullOrEmpty(id))
        {
            if (log) Debug.LogWarning("[NPCFollowTrigger] No NPC id resolved.");
            return;
        }

        var mgr = NPCFollowManager.Instance; // auto-creates if needed
        switch (action)
        {
            case FollowAction.Start:
                mgr.StartFollow(id);
                if (log) Debug.Log($"[NPCFollowTrigger] StartFollow('{id}')");
                break;

            case FollowAction.Stop:
                mgr.StopFollow(id);
                if (log) Debug.Log($"[NPCFollowTrigger] StopFollow('{id}')");
                break;

            case FollowAction.Toggle:
                if (mgr.IsFollowing(id))
                {
                    mgr.StopFollow(id);
                    if (log) Debug.Log($"[NPCFollowTrigger] Toggle -> StopFollow('{id}')");
                }
                else
                {
                    mgr.StartFollow(id);
                    if (log) Debug.Log($"[NPCFollowTrigger] Toggle -> StartFollow('{id}')");
                }
                break;
        }

        _hasFiredOnce = true;
        _nextAllowedTime = Time.time + cooldown;
    }

    private string ResolveId()
    {
        if (npcIdentity != null && !string.IsNullOrEmpty(npcIdentity.id))
            return npcIdentity.id;
        return npcId;
    }
}
