using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.Events;
using PixelCrushers.DialogueSystem;

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

    [Header("Skip Settings")]
    [Tooltip("If true, the player can skip this Timeline while it's playing.")]
    public bool allowSkip = true;

    [Tooltip("Key used to skip the cutscene. You can also call SkipCutscene() from UI.")]
    public KeyCode skipKey = KeyCode.Escape;

    [Tooltip("How long (in seconds) the skip key must be held to skip.")]
    public float skipHoldDuration = 1.0f;

    [Tooltip("How fast the Timeline plays when skipping (e.g. 5 = 5x speed).")]
    public float skipFastForwardSpeed = 5f;

    [Header("Skip UI")]
    [Tooltip("If true, a UI bar will show while the player is holding the skip key.")]
    public bool showSkipUI = true;

    [Tooltip("Root GameObject (panel) for the skip UI. Will be SetActive(true/false).")]
    public GameObject skipUIPanel;

    [Tooltip("Fill Image used to display skip progress (fillAmount 0–1).")]
    public Image skipFillImage;

    [Header("Skip Fade")]
    [Tooltip("Full-screen CanvasGroup used to fade to black when skipping.")]
    public CanvasGroup skipFadeCanvasGroup;

    [Tooltip("Seconds to fade in/out when skipping.")]
    public float skipFadeDuration = 0.4f;

    [Header("Dialogue System (optional)")]
    [Tooltip("If true, skipping the cutscene will also skip any active Dialogue System conversation.")]
    public bool alsoSkipDialogue = true;

    [Tooltip("Optional ConversationControl used to skip Dialogue System conversations.")]
    public ConversationControl conversationControl;

    [Header("Events")]
    [Tooltip("Invoked when the Timeline stops (either naturally or by skip).")]
    public UnityEvent onTimelineEnded;

    [Tooltip("If true, skipping will fast-forward the conversation to the end. " +
             "If false, skipping will immediately stop the conversation.")]
    public bool fastForwardDialogueOnSkip = false;

    


    // internals
    private TimelineOnlyAnimator _dual;                       // holds both animators
    private readonly Dictionary<TrackAsset, Object> _saved = new();
    private bool _bound;
    // global skip lock so only one binder handles skipping at a time
    private static TimelineAnimatorBinder _activeSkipBinder;


    // physics cache
    private Rigidbody2D _rb2d;
    private bool _hadRb2d;
    private bool _prevSimulated;
    private bool _frozenByBinder;

    // completion
    private Coroutine _deactivateCo;
    private bool _timelineStopHandled = false;

    // skip internals
    private float _skipHeldTime = 0f;
    private bool _skipInProgress = false;

    // convenience properties
    private bool IsTimelinePlaying =>
        (director != null && director.state == PlayState.Playing);

    private bool IsConversationActive =>
        DialogueManager.instance != null && DialogueManager.isConversationActive;

    void Reset() => director = GetComponent<PlayableDirector>();

    void Awake()
    {
        if (director == null) director = GetComponent<PlayableDirector>();
        CachePhysicsFrom(playerRoot);
        EnsureDualOn(playerRoot);

        // Ensure skip UI starts hidden
        ResetSkipUI();

        // Ensure fade starts transparent
        if (skipFadeCanvasGroup != null)
            skipFadeCanvasGroup.alpha = 0f;

        // Cache ConversationControl if not assigned
        if (conversationControl == null)
        {
            conversationControl = FindObjectOfType<ConversationControl>();
        }
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

        // Release global skip lock if this binder owned it.
        if (_activeSkipBinder == this)
            _activeSkipBinder = null;
    }


    void LateUpdate()
    {
        // ---------- Hold-to-skip check + UI ----------
        // Only the active skip binder (or the first one that needs it)
        // is allowed to process skip input. Others will ignore skip.
        bool someOtherBinderIsActive =
            _activeSkipBinder != null && _activeSkipBinder != this;

        if (!someOtherBinderIsActive)
        {
            bool canSkipNow = allowSkip && (IsTimelinePlaying || IsConversationActive);

            if (canSkipNow)
            {
                // Claim the global skip lock if nobody has it yet.
                if (_activeSkipBinder == null)
                    _activeSkipBinder = this;

                if (Input.GetKey(skipKey))
                {
                    _skipHeldTime += Time.unscaledDeltaTime;

                    float needed = Mathf.Max(0.0001f, skipHoldDuration);
                    float progress = Mathf.Clamp01(_skipHeldTime / needed);

                    UpdateSkipUI(progress);

                    if (_skipHeldTime >= needed)
                    {
                        _skipHeldTime = 0f;
                        ResetSkipUI();

                        // Start skip flow (fade + fast-forward Timeline)
                        SkipCutscene();
                    }
                }
                else
                {
                    _skipHeldTime = 0f;
                    ResetSkipUI();
                }
            }
            else
            {
                // If we were the active binder but there's nothing left to skip,
                // release the global lock so another cutscene can claim it.
                if (_activeSkipBinder == this &&
                    !IsTimelinePlaying &&
                    !IsConversationActive)
                {
                    _activeSkipBinder = null;
                }

                _skipHeldTime = 0f;
                ResetSkipUI();
            }
        }
        else
        {
            // We're *not* the active skip binder. Make sure our UI is hidden
            // and we don't track skip timing.
            _skipHeldTime = 0f;
            ResetSkipUI();
        }

        // ---------- Physics failsafe watchdog ----------
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


    // ---------- Skip UI helpers ----------

    private void UpdateSkipUI(float progress)
    {
        if (!showSkipUI) return;

        if (skipUIPanel != null && !skipUIPanel.activeSelf)
            skipUIPanel.SetActive(true);

        if (skipFillImage != null)
            skipFillImage.fillAmount = progress;
    }

    private void ResetSkipUI()
    {
        if (!showSkipUI) return;

        if (skipUIPanel != null && skipUIPanel.activeSelf)
            skipUIPanel.SetActive(false);

        if (skipFillImage != null)
            skipFillImage.fillAmount = 0f;
    }

    // ---------- Dialogue skip helper ----------

    /// <summary>
    /// Handles what happens to the Dialogue System conversation when we skip.
    /// - If fastForwardDialogueOnSkip is true, it tries to fast-forward (SkipAll).
    /// - Otherwise it just stops all conversations immediately.
    /// Called from the skip coroutine as soon as skipping starts.
    /// </summary>
    /// <summary>
    /// If a Dialogue System conversation is active, either fast-forward it
    /// (SkipAll) or stop it immediately, based on fastForwardDialogueOnSkip.
    /// Safe to call every frame while skipping.
    /// </summary>
    private void SkipDialogueConversationIfActive()
    {
        if (!alsoSkipDialogue) return;
        if (!DialogueManager.isConversationActive) return;

        // Try to get a ConversationControl (from field or find in scene)
        ConversationControl control = conversationControl;
        if (control == null)
            control = FindObjectOfType<ConversationControl>();

        if (fastForwardDialogueOnSkip)
        {
            // Let Dialogue System burn through all remaining entries & sequences.
            if (control != null)
            {
                control.SkipAll();
            }
            else
            {
                // Fallback if no ConversationControl is present.
                DialogueManager.StopAllConversations();
            }
        }
        else
        {
            // Simple behavior: instantly close all conversations & UI.
            DialogueManager.StopAllConversations();
        }
    }




    // ---------- Timeline stopped handling ----------

    private void HandleTimelineStoppedInternal()
    {
        if (_timelineStopHandled) return;
        _timelineStopHandled = true;

        // Always ensure fade is gone when the timeline is finished
        if (skipFadeCanvasGroup != null)
            skipFadeCanvasGroup.alpha = 0f;

        if (_dual != null) _dual.DisableWhenIdle();
        if (freezePhysicsDuringPlay) UnfreezePhysics();

        if (onTimelineEnded != null)
            onTimelineEnded.Invoke();

        if (deactivateDirectorOnStop && director != null)
        {
            if (_deactivateCo != null) StopCoroutine(_deactivateCo);
            _deactivateCo = StartCoroutine(DeactivateDirectorAfter(deactivateDelay));
        }

        // If we owned the global skip lock and there's no conversation,
        // release it so other cutscenes can be skipped later.
        if (_activeSkipBinder == this && !IsConversationActive)
            _activeSkipBinder = null;
    }



    /// <summary>
    /// Public entry point for skipping. Starts the fade + fast-forward coroutine.
    /// </summary>
    public void SkipCutscene()
    {
        // Enforce global "only one cutscene can be skipped at a time".
        if (_activeSkipBinder != null && _activeSkipBinder != this)
            return;

        if (_activeSkipBinder == null)
            _activeSkipBinder = this;

        if (_skipInProgress) return;
        StartCoroutine(SkipCutsceneRoutine());
    }


    /// <summary>
    /// Fade to black while fast-forwarding the Timeline to the end
    /// (by increasing its playback speed), then clean up and fade back in.
    /// </summary>
    private IEnumerator SkipCutsceneRoutine()
    {
        _skipInProgress = true;

        // 0) Boost Timeline speed so it fast-forwards instead of playing at 1x.
        Playable rootPlayable = default;
        bool hasRootPlayable = false;
        double originalSpeed = 1.0;

        if (director != null && director.playableAsset != null)
        {
            var graph = director.playableGraph;
            if (graph.IsValid() && graph.GetRootPlayableCount() > 0)
            {
                rootPlayable = graph.GetRootPlayable(0);
                originalSpeed = rootPlayable.GetSpeed();
                rootPlayable.SetSpeed(skipFastForwardSpeed);
                hasRootPlayable = true;
            }
        }

        // 1) Fade to black while it’s fast-forwarding
        if (skipFadeCanvasGroup != null && skipFadeDuration > 0f)
        {
            float t = 0f;
            while (t < skipFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float norm = Mathf.Clamp01(t / skipFadeDuration);
                skipFadeCanvasGroup.alpha = norm;

                // While we’re in skip mode, keep any active conversations
                // skipped or stopped (covers multiple Start Conversation clips).
                SkipDialogueConversationIfActive();

                yield return null;
            }
            skipFadeCanvasGroup.alpha = 1f;
        }

        // 2) Wait until the Timeline finishes naturally (at high speed)
        if (director != null)
        {
            while (director.state == PlayState.Playing)
            {
                // New conversations might start as the Timeline scrubs past
                // more StartConversation clips; keep smashing them.
                SkipDialogueConversationIfActive();
                yield return null;
            }
        }

        // 3) Restore original Timeline speed
        if (hasRootPlayable)
        {
            rootPlayable.SetSpeed(originalSpeed);
        }

        // One more safety call after it’s stopped
        SkipDialogueConversationIfActive();

        // 4) Run the "timeline ended" logic while still black
        HandleTimelineStoppedInternal();

        // 5) Fade back in
        if (skipFadeCanvasGroup != null && skipFadeDuration > 0f)
        {
            float t = 0f;
            while (t < skipFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float norm = Mathf.Clamp01(t / skipFadeDuration);
                skipFadeCanvasGroup.alpha = 1f - norm;
                yield return null;
            }
        }

        // 6) Hard safety: make absolutely sure the fade is gone
        ForceHideFade();

        _skipInProgress = false;

        // If we owned the global lock and there's nothing left to skip,
        // release it so another cutscene can be skipped later.
        if (_activeSkipBinder == this &&
            !IsTimelinePlaying &&
            !IsConversationActive)
        {
            _activeSkipBinder = null;
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

    private void ForceHideFade()
    {
        if (skipFadeCanvasGroup != null)
            skipFadeCanvasGroup.alpha = 0f;
    }


    // ========= Director lifecycle =========

    private void OnPlayed(PlayableDirector d)
    {
        _timelineStopHandled = false;

        if (_dual != null) _dual.EnableForTimeline();
        if (freezePhysicsDuringPlay) FreezePhysics();
    }

    private void OnStopped(PlayableDirector d)
    {
        // No matter how the Timeline ended (natural or skip),
        // always make sure the fade overlay is hidden.
        ForceHideFade();

        if (_dual != null) _dual.DisableWhenIdle();
        if (freezePhysicsDuringPlay) UnfreezePhysics();

        // Fire event once.
        if (!_timelineStopHandled)
        {
            _timelineStopHandled = true;

            if (onTimelineEnded != null)
                onTimelineEnded.Invoke();

            if (deactivateDirectorOnStop && director != null)
            {
                if (_deactivateCo != null) StopCoroutine(_deactivateCo);
                _deactivateCo = StartCoroutine(DeactivateDirectorAfter(deactivateDelay));
            }
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
