using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[RequireComponent(typeof(PlayableDirector))]
public class PlayerTimelineHandoff : MonoBehaviour
{
    [Header("Director & Actor (scene placeholder)")]
    public PlayableDirector director;          // Assign your director
    public Transform playerActorRoot;          // The scene Actor used for Timeline authoring
    [Tooltip("Relative path from actor root to the child Animator (e.g., \"Model\")")]
    public string childAnimatorPath = "Model";

    [Header("Prefab handoff")]
    public string playerTag = "Player";        // Spawned player tag
    public bool snapPlayerToActorPose = true;
    public bool disableActorAfterHandoff = true;
    public float waitForPlayerSeconds = 2f;

    [Header("Debug")]
    public bool logDebug = false;

    // Cached tracks discovered from actor bindings (no names required)
    private AnimationTrack _rootTrack;   // bound to actor root Animator
    private AnimationTrack _childTrack;  // bound to actor child Animator

    void Awake()
    {
        if (!director) director = GetComponent<PlayableDirector>();
        CacheTracksFromActor();                  // find which track is root vs child
        // Bind to the actor up front so Timeline scrubs in editor as expected
        if (playerActorRoot) BindToRootAndChild(playerActorRoot);
    }

    void OnEnable() => director.played += OnDirectorPlayed;
    void OnDisable() => director.played -= OnDirectorPlayed;

    void OnDirectorPlayed(PlayableDirector d)
    {
        StartCoroutine(HandoffWhenPlayerSpawns());
    }

    IEnumerator HandoffWhenPlayerSpawns()
    {
        float t = 0f;
        Transform player = null;

        while (t < waitForPlayerSeconds)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) { player = go.transform; break; }
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!player) yield break;

        if (snapPlayerToActorPose && playerActorRoot)
            player.SetPositionAndRotation(playerActorRoot.position, playerActorRoot.rotation);

        BindToRootAndChild(player);

        if (disableActorAfterHandoff && playerActorRoot)
            playerActorRoot.gameObject.SetActive(false);

        if (logDebug) Debug.Log("[PlayerTimelineHandoff] Rebound tracks to Player.");
    }

    void CacheTracksFromActor()
    {
        _rootTrack = null;
        _childTrack = null;

        if (!(director.playableAsset is TimelineAsset tl) || !playerActorRoot) return;

        var actorRootAnimator = playerActorRoot.GetComponent<Animator>();
        var actorChild = string.IsNullOrEmpty(childAnimatorPath) ? null : playerActorRoot.Find(childAnimatorPath);
        var actorChildAnim = actorChild ? actorChild.GetComponent<Animator>() : null;

        foreach (var track in tl.GetOutputTracks())
        {
            if (track is not AnimationTrack animTrack) continue;

            var bound = director.GetGenericBinding(track) as Object;

            if (bound == actorRootAnimator)
                _rootTrack = animTrack;
            else if (bound == actorChildAnim)
                _childTrack = animTrack;
        }

        if (logDebug)
        {
            Debug.Log($"[PlayerTimelineHandoff] Cached tracks: root={_rootTrack != null}, child={_childTrack != null}");
        }
    }

    void BindToRootAndChild(Transform root)
    {
        if (!(director.playableAsset is TimelineAsset) || root == null) return;

        var rootAnim = root.GetComponent<Animator>();
        var child = string.IsNullOrEmpty(childAnimatorPath) ? null : root.Find(childAnimatorPath);
        var childAnim = child ? child.GetComponent<Animator>() : null;

        if (_rootTrack && rootAnim)
            director.SetGenericBinding(_rootTrack, rootAnim);
        if (_childTrack && childAnim)
            director.SetGenericBinding(_childTrack, childAnim);

        director.RebuildGraph();
        director.Evaluate();
    }
}
