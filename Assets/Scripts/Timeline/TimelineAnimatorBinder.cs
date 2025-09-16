using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// Attach to the same GameObject as your PlayableDirector.
/// Your PlayerSpawner should call SetPlayerRoot() and RebindNow() after the player is instantiated.
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

    // internals
    private TimelineOnlyAnimator _dual;                       // holds both animators
    private readonly Dictionary<TrackAsset, Object> _saved = new();
    private bool _bound;

    // physics cache
    private Rigidbody2D _rb2d;
    private bool _hadRb2d;
    private bool _prevSimulated;

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
        UnfreezePhysics();
        if (_dual != null) _dual.DisableWhenIdle();
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
        StopAllCoroutines();
        StartCoroutine(RebindRoutine());
    }

    private IEnumerator RebindRoutine()
    {
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
                // Debug.Log($"[Binder] Bound '{track.name}' -> ROOT ({_dual.timelineAnimator.gameObject.name})");
            }

            // Model animation track -> gameplay/model animator
            if (!string.IsNullOrEmpty(modelTrackName) &&
                track.name == modelTrackName &&
                _dual.gameplayAnimator != null)
            {
                SaveAndBind(track, _dual.gameplayAnimator);
                // Debug.Log($"[Binder] Bound '{track.name}' -> MODEL ({_dual.gameplayAnimator.gameObject.name})");
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

        // Safety on the root Animator
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
        if (!_hadRb2d) return;
        _prevSimulated = _rb2d.simulated;
        _rb2d.velocity = Vector2.zero;
        _rb2d.angularVelocity = 0f;
        _rb2d.simulated = false;
    }

    private void UnfreezePhysics()
    {
        if (!_hadRb2d) return;
        _rb2d.simulated = _prevSimulated;
        _rb2d.velocity = Vector2.zero;
        _rb2d.angularVelocity = 0f;
    }
}
