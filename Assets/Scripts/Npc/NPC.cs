using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[DisallowMultipleComponent]
public class NPC : Entity
{
    // ───── Movement
    [Header("Movement")]
    public float moveSpeed = 3f;

    // ───── Patrol (EXACT Enemy-style: Vector2[] editable in Inspector)
    [Header("Patrol")]
    public Vector2[] patrolPoints;                 // edit X/Y like Enemy
    public int currentPatrolIndex = 0;
    public bool loopPatrol = true;
    public float waitAtWaypoint = 0.25f;
    public bool autoStartPatrol = false;

    private bool _resumePatrolAfterInteraction;

    [HideInInspector] public bool isPaused { get; set; } = false; // used by PatrolState like Enemy
    [HideInInspector] public Vector2 target;                      // used by PatrolState like Enemy

    // ───── Follow (command-only via Dialogue)
    [Header("Follow (commanded only)")]
    public bool followCommanded = false;
    public Transform followTarget = null; // usually PlayerLocator.Current
    public float followStopDistance = 1.25f;

    // ───── States
    [HideInInspector] public NPC_IdleState idleState;
    [HideInInspector] public NPC_PatrolState patrolState;
    [HideInInspector] public NPC_FollowState followState;
    [HideInInspector] public Vector2 lastFacing = Vector2.down;

    [Header("Interaction")]
    public bool isInteracting = false;
    public Transform currentInteractor = null;

    [Header("Interaction Facing")]
    public bool faceInteractorWhileTalking = true;

    [Header("Interaction")]
    public bool faceOnlyOnUse = true;      // just for clarity in inspector
    public bool IsInteracting { get; private set; }
    private Transform _interactor;

    // Back-compat wrappers (if any other script calls these names)
    public void BeginInteraction(Transform actor) => BeginInteractionLock(actor);
    public void EndInteraction() => EndInteractionLock();

    // ───── Timeline (optional)
    [Header("Timeline (optional)")]
    public PlayableDirector timelineDirector;
    [Tooltip("Track name that drives the root transform / motion.")]
    public string rootTrackName;
    [Tooltip("Track name that drives the model (body/animation).")]
    public string modelTrackName;
    [Tooltip("Bind this NPC to its Timeline tracks automatically on RebindFollow.")]
    public bool bindToTimelineOnRebind = true;
    [Tooltip("Play the Timeline automatically after binding.")]
    public bool timelineAutoPlayAfterBind = false;
    [Range(0, 4)] public int timelineBindDelayFrames = 1;

    [Tooltip("If true, the NPC will attempt to auto-find which PlayableDirector/Timeline it belongs to by scanning scene directors for matching tracks/bindings.")]
    public bool autoFindTimeline = true;

    private readonly Dictionary<TrackAsset, Object> _savedTimelineBindings = new();

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new StateMachine();

        idleState = new NPC_IdleState(this, stateMachine, "idle");
        patrolState = new NPC_PatrolState(this, stateMachine, "move");
        followState = new NPC_FollowState(this, stateMachine, "move");
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);

        // Enemy-style convenience: if designer left point 0 at (0,0), replace with spawn position
        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] == Vector2.zero)
            patrolPoints[0] = transform.position;

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            target = patrolPoints[currentPatrolIndex];
            StartCoroutine(SetPatrolPoint()); // same as Enemy.Start()
        }

        if (autoStartPatrol && patrolPoints != null && patrolPoints.Length > 0)
            stateMachine.ChangeState(patrolState);

        // Follow-manager bootstrap (unchanged)
        var ident = GetComponent<NPCIdentity>();
        if (ident != null && !string.IsNullOrEmpty(ident.id))
        {
            bool shouldFollow = false;
            if (NPCFollowManager.Instance != null && NPCFollowManager.Instance.IsFollowing(ident.id))
                shouldFollow = true;
            else if (NPCFollowManager.PendingFollowingIds != null &&
                     NPCFollowManager.PendingFollowingIds.Contains(ident.id))
                shouldFollow = true;

            if (shouldFollow)
            {
                if (NPCFollowManager.Instance != null)
                    NPCFollowManager.Instance.TryStartFollowOnScenePublic(ident.id);

                StartCoroutine(DelayedTryStartFollow(ident.id, 0.05f));
            }
        }
    }

    private System.Collections.IEnumerator DelayedTryStartFollow(string id, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (NPCFollowManager.Instance != null && NPCFollowManager.Instance.IsFollowing(id))
            NPCFollowManager.Instance.TryStartFollowOnScenePublic(id);
    }

    // ─────────────────────────────────────────────
    // PATROL (Enemy-exact)
    // ─────────────────────────────────────────────
    public System.Collections.IEnumerator SetPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            yield break;

        isPaused = true;
        yield return new WaitForSeconds(waitAtWaypoint);

        if (currentPatrolIndex < 0 || currentPatrolIndex >= patrolPoints.Length)
            currentPatrolIndex = 0;
        else
            currentPatrolIndex = loopPatrol
                ? (currentPatrolIndex + 1) % patrolPoints.Length
                : Mathf.Min(currentPatrolIndex + 1, patrolPoints.Length - 1);

        target = patrolPoints[currentPatrolIndex];

        // Update facing like Enemy
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        currentDir = dir;
        UpdateFacing(dir);

        isPaused = false;
    }

    // ─────────────────────────────────────────────
    // FOLLOW / TELEPORT / TIMELINE
    // ─────────────────────────────────────────────
    public void RebindFollow(Transform newTarget, bool startFollowing = true)
    {
        if (newTarget == null)
        {
            followTarget = null;
            followCommanded = false;
            if (stateMachine != null) stateMachine.ChangeState(idleState);
            return;
        }

        followTarget = newTarget;
        TeleportNextToPlayer();

        if (bindToTimelineOnRebind && autoFindTimeline && timelineDirector == null)
            TryAutoFindTimelineForThisNPC();

        if (bindToTimelineOnRebind)
            StartCoroutine(BindThisNPCToTimelineRoutine());

        if (startFollowing)
        {
            followCommanded = true;
            if (stateMachine != null && stateMachine.currentState != followState)
                stateMachine.ChangeState(followState);
        }
        else
        {
            Vector2 dir = (newTarget.position - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
                UpdateFacing(dir.normalized);   // ✅ stores lastFacing + sets anim
        }

    }

    private void TeleportNextToPlayer()
    {
        if (followTarget == null) return;

        Vector3 npcPos = transform.position;
        Vector3 playerPos = followTarget.position;

        Vector2 dir = (Vector2)(npcPos - playerPos);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.left;
        else dir.Normalize();

        Vector3 targetPos = new Vector3(
            playerPos.x + dir.x * followStopDistance,
            playerPos.y + dir.y * followStopDistance,
            npcPos.z);

        transform.position = targetPos;
    }

    public void AdvancePatrolIndexAndTarget()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (currentPatrolIndex < 0 || currentPatrolIndex >= patrolPoints.Length)
            currentPatrolIndex = 0;
        else
            currentPatrolIndex = loopPatrol
                ? (currentPatrolIndex + 1) % patrolPoints.Length
                : Mathf.Min(currentPatrolIndex + 1, patrolPoints.Length - 1);

        target = patrolPoints[currentPatrolIndex];

        Vector2 dir = (target - (Vector2)transform.position).normalized;
        currentDir = dir;
        UpdateFacing(dir);
    }


    private System.Collections.IEnumerator BindThisNPCToTimelineRoutine()
    {
        if (timelineDirector == null || timelineDirector.playableAsset == null)
            yield break;

        for (int i = 0; i < Mathf.Max(0, timelineBindDelayFrames); i++)
            yield return null;

        var asset = timelineDirector.playableAsset as TimelineAsset;
        if (asset == null) yield break;

        Animator rootAnimator = anim != null ? anim : GetComponent<Animator>();
        Animator modelAnimator = null;

        if (rootAnimator != null)
        {
            var children = GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < children.Length; i++)
            {
                var c = children[i];
                if (c != null && c != rootAnimator) { modelAnimator = c; break; }
            }
        }

        var outputs = asset.GetOutputTracks();
        foreach (var track in outputs)
        {
            if (track is not AnimationTrack) continue;

            if (!string.IsNullOrEmpty(rootTrackName) && track.name == rootTrackName && rootAnimator != null)
                SaveAndBind(track, rootAnimator);

            if (!string.IsNullOrEmpty(modelTrackName) && track.name == modelTrackName && modelAnimator != null)
                SaveAndBind(track, modelAnimator);
        }

        timelineDirector.RebuildGraph();
        timelineDirector.Evaluate();

        if (timelineAutoPlayAfterBind)
            timelineDirector.Play();
    }

    private void SaveAndBind(TrackAsset track, Object targetObj)
    {
        var old = timelineDirector.GetGenericBinding(track);
        if (!_savedTimelineBindings.ContainsKey(track))
            _savedTimelineBindings.Add(track, old);
        timelineDirector.SetGenericBinding(track, targetObj);
    }

    // ─────────────────────────────────────────────
    // AUTO-FIND THE NPC'S TIMELINE/DIRECTOR
    // ─────────────────────────────────────────────
    private void TryAutoFindTimelineForThisNPC()
    {
        var all = FindObjectsOfType<PlayableDirector>(true);
        if (all == null || all.Length == 0) return;

        var ident = GetComponent<NPCIdentity>();
        string id = (ident != null) ? ident.id : null;
        string goName = gameObject.name;

        string[] rootCandidates = BuildCandidates(rootTrackName, id, goName);
        string[] modelCandidates = BuildCandidates(modelTrackName, id != null ? id + "Model" : null, goName + "Model");

        string[] moreRoot = BuildCandidates(null, id != null ? id + "Root" : null, goName + "Root");
        rootCandidates = MergeArrays(rootCandidates, moreRoot);

        Animator rootAnimator = anim != null ? anim : GetComponent<Animator>();
        Animator modelAnimator = null;
        if (rootAnimator != null)
        {
            var children = GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < children.Length; i++)
            {
                var c = children[i];
                if (c != null && c != rootAnimator) { modelAnimator = c; break; }
            }
        }

        PlayableDirector best = null;
        string bestRoot = null;
        string bestModel = null;
        int bestScore = 0;

        for (int i = 0; i < all.Length; i++)
        {
            var d = all[i];
            if (d == null || d.playableAsset == null) continue;
            var asset = d.playableAsset as TimelineAsset;
            if (asset == null) continue;

            int score = 0;
            string foundRoot = null;
            string foundModel = null;

            var outputs = asset.GetOutputTracks();
            foreach (var track in outputs)
            {
                if (track is not AnimationTrack) continue;

                if (NameInList(track.name, rootCandidates)) { score += 2; foundRoot = track.name; }
                if (NameInList(track.name, modelCandidates)) { score += 2; foundModel = track.name; }

                var bound = d.GetGenericBinding(track);
                if (bound != null)
                {
                    if (rootAnimator != null && bound == rootAnimator) { score += 4; foundRoot = track.name; }
                    if (modelAnimator != null && bound == modelAnimator) { score += 4; foundModel = track.name; }
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = d;
                bestRoot = foundRoot;
                bestModel = foundModel;
            }
        }

        if (best != null && bestScore > 0)
        {
            timelineDirector = best;

            if (string.IsNullOrEmpty(rootTrackName) && !string.IsNullOrEmpty(bestRoot))
                rootTrackName = bestRoot;
            if (string.IsNullOrEmpty(modelTrackName) && !string.IsNullOrEmpty(bestModel))
                modelTrackName = bestModel;
        }
    }

    private static string[] BuildCandidates(string explicitName, string alt1, string alt2)
    {
        var list = new List<string>(4);
        if (!string.IsNullOrEmpty(explicitName)) list.Add(explicitName);
        if (!string.IsNullOrEmpty(alt1)) list.Add(alt1);
        if (!string.IsNullOrEmpty(alt2)) list.Add(alt2);
        return list.ToArray();
    }

    private static string[] MergeArrays(string[] a, string[] b)
    {
        if (a == null || a.Length == 0) return b ?? System.Array.Empty<string>();
        if (b == null || b.Length == 0) return a;
        var list = new List<string>(a.Length + b.Length);
        for (int i = 0; i < a.Length; i++) if (!string.IsNullOrEmpty(a[i])) list.Add(a[i]);
        for (int i = 0; i < b.Length; i++) if (!string.IsNullOrEmpty(b[i])) list.Add(b[i]);
        return list.ToArray();
    }

    private static bool NameInList(string name, string[] list)
    {
        if (list == null) return false;
        for (int i = 0; i < list.Length; i++)
            if (name == list[i]) return true;
        return false;
    }

    // ─────────────────────────────────────────────
    // ANIMATION & FACING
    // ─────────────────────────────────────────────
    public void UpdateFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
        {
            lastFacing = dir.normalized;

            if (anim)
            {
                anim.SetFloat("xInput", lastFacing.x);
                anim.SetFloat("yInput", lastFacing.y);
            }
        }
    }

    public void ApplyLastFacing()
    {
        if (anim)
        {
            anim.SetFloat("xInput", lastFacing.x);
            anim.SetFloat("yInput", lastFacing.y);
        }
    }

    // ─────────────────────────────────────────────
    // PUBLIC API (Follow)
    // ─────────────────────────────────────────────
    public void StartFollowing(Transform targetT)
    {
        followCommanded = true;
        followTarget = targetT;
        if (stateMachine.currentState != followState)
            stateMachine.ChangeState(followState);
    }

    public void StartFollow(Transform targetT) => StartFollowing(targetT);
    public void StopFollow() => StopFollowing();

    public void StopFollowing()
    {
        followCommanded = false;
        followTarget = null;

        // ✅ keep current facing when follow ends
        ApplyLastFacing(); // (lastFacing was kept updated by UpdateFacing during follow)

        if (autoStartPatrol && patrolPoints != null && patrolPoints.Length > 0)
            stateMachine.ChangeState(patrolState);
        else
            stateMachine.ChangeState(idleState);
    }


#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Vector3 p = patrolPoints[i];
            Gizmos.DrawSphere(p, 0.12f);

            int next = i + 1;
            if (next >= patrolPoints.Length)
            {
                if (!loopPatrol) break;
                next = 0;
            }

            Vector3 q = patrolPoints[next];
            Gizmos.DrawLine(p, q);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] == Vector2.zero)
                patrolPoints[0] = transform.position;
        }
    }
#endif


    // Called by InteractionTooltipTrigger2D via SendMessage("OnSelect", actor)
    public void OnSelect(Transform actor)
    {
        // DO NOTHING. We do not face on select.
        _interactor = actor; // optional: store who is inside
    }

    // Called by InteractionTooltipTrigger2D via SendMessage("OnUse", actor)
    public void OnUse(Transform actor)
    {
        _interactor = actor;

        // remember if we were patrolling when interaction started
        _resumePatrolAfterInteraction = (stateMachine != null && stateMachine.currentState == patrolState);

        BeginInteractionLock(actor);
    }


    // Called by InteractionTooltipTrigger2D via SendMessage("OnDeselect")
    public void OnDeselect()
    {
        // Do NOT unlock here. Dialogue System will end the conversation later.
        // If you want to unlock when leaving trigger without talking, you'd do it here,
        // but you said only face on use, so we leave it alone.
    }

    private void BeginInteractionLock(Transform actor)
    {
        IsInteracting = true;
        SetZeroVelocity();

        // ✅ Face ONLY here
        if (actor != null)
            FaceTarget(actor);

        // Stop patrol/follow movement
        if (stateMachine != null && idleState != null)
            stateMachine.ChangeState(idleState);
    }

    private void EndInteractionLock()
    {
        IsInteracting = false;
        _interactor = null;
        SetZeroVelocity();

        // keep lastFacing as-is so idle faces correctly
        ApplyLastFacing();

        // ✅ Resume patrol toward CURRENT target (do not advance index)
        if (_resumePatrolAfterInteraction &&
            !followCommanded &&
            autoStartPatrol &&
            patrolPoints != null && patrolPoints.Length > 0)
        {
            // ensure target is valid; if not, rebuild it from current index
            if (target == Vector2.zero && patrolPoints.Length > 0)
                target = patrolPoints[Mathf.Clamp(currentPatrolIndex, 0, patrolPoints.Length - 1)];

            isPaused = false;                    // make sure patrol state moves
            stateMachine.ChangeState(patrolState);
        }
        else
        {
            stateMachine.ChangeState(idleState);
        }

        _resumePatrolAfterInteraction = false;
    }


    public void FaceTarget(Transform t)
    {
        if (t == null) return;

        Vector2 dir = (Vector2)(t.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        UpdateFacing(dir.normalized); // updates lastFacing + anim params
    }

    // ===================== Dialogue System Hooks =====================
    // If you want: conversation start does NOT rotate; OnUse already did.
    public void OnConversationStart(Transform actor)
    {
        IsInteracting = true;
        SetZeroVelocity();
    }

    public void OnConversationEnd(Transform actor)
    {
        EndInteractionLock();
    }


  

}
