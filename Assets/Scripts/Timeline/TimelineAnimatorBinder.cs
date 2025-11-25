using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[DefaultExecutionOrder(-50)]
public class TimelineAnimatorBinder : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector director;
    [Tooltip("Set by the spawner at runtime (the Player root GameObject).")]
    public GameObject playerRoot;

    [Header("Playback Options")]
    [Tooltip("If true, Play() is called right after a successful rebind.")]
    public bool autoPlayAfterRebind = true;

    [Tooltip("Freeze Rigidbody2D on the player while Timeline is playing (prevents physics from fighting animation).")]
    public bool freezePhysicsDuringPlay = true;

    [Tooltip("Frames to wait before binding (lets prefab & graph finish initializing).")]
    [Range(0, 4)] public int bindDelayFrames = 2;

    [Header("Track Names (exact match)")]
    [Tooltip("Animation Track name that drives the ROOT (dummy) Animator for motion.")]
    public string rootTrackName = "Lioncard";

    [Tooltip("Animation Track name that drives the MODEL (gameplay) Animator for clips.")]
    public string modelTrackName = "LioncardModel";

    [Header("Completion Behavior")]
    [Tooltip("If true, the PlayableDirector GameObject will be SetActive(false) when the Timeline stops.")]
    public bool deactivateDirectorOnStop = true;

    [Tooltip("Optional delay before deactivating the PlayableDirector GameObject.")]
    [Min(0f)] public float deactivateDelay = 0f;

    [Header("Activation / Lifetime")]
    [Tooltip("If false, the director GameObject will be re-disabled after RebindNow " +
         "if it was originally inactive (useful for cutscenes you only want active when triggered).")]
    public bool keepDirectorActiveAfterRebind = true;


    // internals
    private TimelineOnlyAnimator _dual;                       // holds both animators
    private readonly Dictionary<TrackAsset, Object> _saved = new();
    private bool _bound;

    // physics cache
    private Rigidbody2D _rb2d;
    private bool _hadRb2d;
    private bool _prevSimulated;
    private bool _frozenByBinder;      // <— NEW: only unfreeze if we froze it

    // completion
    private Coroutine _deactivateCo;

    void Reset() => director = GetComponent<PlayableDirector>();

    void Awake()
    {
        if (director == null) director = GetComponent<PlayableDirector>();
        CachePhysicsFrom(playerRoot);
        EnsureDualOn(playerRoot);
    }

    void OnEnable()
    {
        if (director != null)
        {
            director.played += OnPlayed;
            director.stopped += OnStopped;
        }
    }

    void OnDisable()
    {
        if (director != null)
        {
            director.played -= OnPlayed;
            director.stopped -= OnStopped;
        }

        RestoreOriginalBindings();
        _bound = false;

        // Failsafe: always unfreeze if we had frozen
        UnfreezePhysics();

        if (_dual != null) _dual.DisableWhenIdle();
    }

    void LateUpdate()
    {
        // ---------- Failsafe watchdog ----------
        if (_frozenByBinder && freezePhysicsDuringPlay)
        {
            bool shouldUnfreeze = false;

            if (director == null)
            {
                shouldUnfreeze = true;
            }
            else
            {
                var isPlaying = director.state == PlayState.Playing;
                double dur = director.duration;
                double t = director.time;
                bool atOrPastEnd = (dur > 0.0001) && (t >= dur - 0.0001);

                if (!isPlaying || atOrPastEnd)
                    shouldUnfreeze = true;
            }

            if (shouldUnfreeze)
                UnfreezePhysics();
        }
    }

    // ========= Public API (call from PlayerSpawner) =========

    /// Call this after you spawn/select the Player.
    public void SetPlayerRoot(GameObject root)
    {
        playerRoot = root;
        CachePhysicsFrom(playerRoot);
        EnsureDualOn(playerRoot);
    }

    /// Rebind tracks now (after a small delay), then optionally auto-play.
    public void RebindNow()
    {
        if (director == null) return;

        // Remember whether this GO was active before we touched it:
        bool wasActive = director.gameObject.activeSelf;

        // Make sure it's active so coroutines & bindings can run:
        if (!wasActive)
            director.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(RebindRoutine(wasActive));
    }

    private IEnumerator RebindRoutine(bool wasActive)
    {
        // Wait the configured number of frames:
        for (int i = 0; i < bindDelayFrames; i++) yield return null;

        TryBindTracks();

        if (_dual != null) _dual.EnableForTimeline();

        if (autoPlayAfterRebind && director != null)
        {
            director.RebuildGraph();
            director.time = 0;
            director.Evaluate();   // snap to first frame pose
            director.Play();
        }

        // If it *started* inactive and you don't want it active at start,
        // put it back the way it was (only when we're not auto-playing).
        if (!keepDirectorActiveAfterRebind &&
            !wasActive &&
            director != null &&
            !autoPlayAfterRebind)
        {
            director.gameObject.SetActive(false);
        }
    }


    // ========= Director lifecycle =========

    private void OnPlayed(PlayableDirector d)
    {
        if (_dual != null) _dual.EnableForTimeline();
        if (freezePhysicsDuringPlay) FreezePhysics();
    }

    private void OnStopped(PlayableDirector d)
    {
        if (_dual != null) _dual.DisableWhenIdle();
        if (freezePhysicsDuringPlay) UnfreezePhysics();

        if (deactivateDirectorOnStop && director != null)
        {
            if (_deactivateCo != null) StopCoroutine(_deactivateCo);
            _deactivateCo = StartCoroutine(DeactivateDirectorAfter(deactivateDelay));
        }
    }

    private IEnumerator DeactivateDirectorAfter(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (director != null) director.gameObject.SetActive(false);
        _deactivateCo = null;
    }

    // ========= Binding logic =========

    private void TryBindTracks()
    {
        if (_bound) return;
        if (director == null || director.playableAsset == null || _dual == null) return;

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) return;

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is not AnimationTrack) continue;

            // Root transform track -> dummy/root animator
            if (!string.IsNullOrEmpty(rootTrackName) &&
                track.name == rootTrackName &&
                _dual.timelineAnimator != null)
            {
                SaveAndBind(track, _dual.timelineAnimator);
            }

            // Model animation track -> gameplay/model animator
            if (!string.IsNullOrEmpty(modelTrackName) &&
                track.name == modelTrackName &&
                _dual.gameplayAnimator != null)
            {
                SaveAndBind(track, _dual.gameplayAnimator);
            }
        }

        _bound = true;
    }

    private void SaveAndBind(TrackAsset track, Object target)
    {
        var old = director.GetGenericBinding(track);
        if (!_saved.ContainsKey(track))
            _saved.Add(track, old);

        director.SetGenericBinding(track, target);
    }

    private void RestoreOriginalBindings()
    {
        if (director == null || _saved.Count == 0) return;

        foreach (var kvp in _saved)
        {
            var track = kvp.Key;
            if (track != null) director.SetGenericBinding(track, kvp.Value);
        }

        _saved.Clear();
    }

    // ========= Helpers =========

    private void EnsureDualOn(GameObject root)
    {
        if (root == null) { _dual = null; return; }

        _dual = root.GetComponent<TimelineOnlyAnimator>();
        if (_dual == null) _dual = root.AddComponent<TimelineOnlyAnimator>();

        if (_dual.timelineAnimator != null)
        {
            _dual.timelineAnimator.runtimeAnimatorController = null;
            _dual.timelineAnimator.avatar = null;
            _dual.timelineAnimator.applyRootMotion = false;
            _dual.DisableWhenIdle(); // stays off until Timeline plays
        }
    }

    private void CachePhysicsFrom(GameObject root)
    {
        _rb2d = root ? root.GetComponent<Rigidbody2D>() : null;
        _hadRb2d = _rb2d != null;
    }

    private void FreezePhysics()
    {
        if (!_hadRb2d || _rb2d == null || _frozenByBinder) return;

        _prevSimulated = _rb2d.simulated;
        _rb2d.velocity = Vector2.zero;
        _rb2d.angularVelocity = 0f;
        _rb2d.simulated = false;
        _frozenByBinder = true;
    }

    private void UnfreezePhysics()
    {
        if (!_hadRb2d || _rb2d == null || !_frozenByBinder) return;

        _rb2d.simulated = _prevSimulated;
        _rb2d.velocity = Vector2.zero;
        _rb2d.angularVelocity = 0f;
        _frozenByBinder = false;
    }

    // ================= OPTIONAL: Auto-pick a director + tracks =================
    // Not used automatically; call this if you want the binder (for player) to self-pick.
    public bool TryAutoPickDirectorAndTracks(GameObject root, string[] rootNameCandidates, string[] modelNameCandidates)
    {
        var all = FindObjectsOfType<PlayableDirector>(true);
        if (all == null || all.Length == 0) return false;

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

            // scan tracks
            foreach (var track in asset.GetOutputTracks())
            {
                if (track is not AnimationTrack) continue;

                // name matches
                if (rootNameCandidates != null)
                {
                    for (int r = 0; r < rootNameCandidates.Length; r++)
                    {
                        var n = rootNameCandidates[r];
                        if (!string.IsNullOrEmpty(n) && track.name == n)
                        {
                            score += 2;
                            foundRoot = n;
                        }
                    }
                }
                if (modelNameCandidates != null)
                {
                    for (int m = 0; m < modelNameCandidates.Length; m++)
                    {
                        var n = modelNameCandidates[m];
                        if (!string.IsNullOrEmpty(n) && track.name == n)
                        {
                            score += 2;
                            foundModel = n;
                        }
                    }
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
            director = best;
            if (!string.IsNullOrEmpty(bestRoot)) rootTrackName = bestRoot;
            if (!string.IsNullOrEmpty(bestModel)) modelTrackName = bestModel;
            return true;
        }
        return false;
    }
}
