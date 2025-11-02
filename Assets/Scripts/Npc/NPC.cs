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

    // ───── Patrol (enemy-like)
    [Header("Patrol")]
    public List<Transform> patrolPoints = new List<Transform>();
    public bool loopPatrol = true;
    public float waypointTolerance = 0.10f;
    public float waitAtWaypoint = 0.25f;
    public bool autoStartPatrol = false;

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

        // Auto-find the Timeline/Director if desired and not set yet
        if (bindToTimelineOnRebind && autoFindTimeline && timelineDirector == null)
        {
            TryAutoFindTimelineForThisNPC();
        }

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
            UpdateFacing((newTarget.position - transform.position).normalized);
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

    private void SaveAndBind(TrackAsset track, Object target)
    {
        var old = timelineDirector.GetGenericBinding(track);
        if (!_savedTimelineBindings.ContainsKey(track))
            _savedTimelineBindings.Add(track, old);
        timelineDirector.SetGenericBinding(track, target);
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

        // Build candidate track names for both root/model if not specified.
        // Priority list: explicit field -> id/id+"Model" -> goName/goName+"Model"
        // (We still use explicit rootTrackName/modelTrackName if you set them.)
        string[] rootCandidates = BuildCandidates(rootTrackName, id, goName, suffix: null);
        string[] modelCandidates = BuildCandidates(modelTrackName, id != null ? id + "Model" : null, goName + "Model", suffix: null);

        // Also consider "Root" suffix variants
        string[] moreRoot = BuildCandidates(null, id != null ? id + "Root" : null, goName + "Root", suffix: null);
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

        // Scan each director and score matches
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

                // Name matches give +2
                if (NameInList(track.name, rootCandidates)) { score += 2; foundRoot = track.name; }
                if (NameInList(track.name, modelCandidates)) { score += 2; foundModel = track.name; }

                // Binding already points at our animators? Strong signal +4
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

            // If you didn't explicitly set names, adopt the matched ones
            if (string.IsNullOrEmpty(rootTrackName) && !string.IsNullOrEmpty(bestRoot))
                rootTrackName = bestRoot;
            if (string.IsNullOrEmpty(modelTrackName) && !string.IsNullOrEmpty(bestModel))
                modelTrackName = bestModel;
        }
    }

    private static string[] BuildCandidates(string explicitName, string alt1, string alt2, string suffix)
    {
        // Build a small list without null/empty entries; keep order
        // suffix is unused for now but kept for extension parity
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
        {
            if (name == list[i]) return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────
    // ANIMATION & FACING
    // ─────────────────────────────────────────────
    public void UpdateFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
        {
            lastFacing = dir;
            if (anim)
            {
                anim.SetFloat("xInput", dir.x);
                anim.SetFloat("yInput", dir.y);
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

    public void SetIdleAnim()
    {
        if (!anim) return;
        anim.SetFloat("xinput", 0f);
        anim.SetFloat("yinput", 0f);
    }

    // ─────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────
    public void StartFollowing(Transform target)
    {
        followCommanded = true;
        followTarget = target;
        if (stateMachine.currentState != followState)
            stateMachine.ChangeState(followState);
    }

    public void StartFollow(Transform target) => StartFollowing(target);
    public void StopFollow() => StopFollowing();

    public void StopFollowing()
    {
        followCommanded = false;
        followTarget = null;

        if (autoStartPatrol && patrolPoints != null && patrolPoints.Count > 0)
            stateMachine.ChangeState(patrolState);
        else
            stateMachine.ChangeState(idleState);
    }
}
