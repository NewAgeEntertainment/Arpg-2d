// PlayerSpawner.cs
// Drop-in spawner with 2D-safe spawn clearance + portal immunity window.
// Works in 2D projects (physics clearance) and will also ignore 3D portal layers during immunity.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_CINEMACHINE
using Unity.Cinemachine; // Cinemachine v3
#endif

[DefaultExecutionOrder(-100)]
public class PlayerSpawner : MonoBehaviour
{
    [Header("Assign Player Prefab")]
    [SerializeField] private Player playerPrefab;

    [Header("Spawn point (if null, use this GameObject position)")]
    [SerializeField] private Transform spawnPoint;

    [Header("Spawn Policy")]
    [Tooltip("If true, ALWAYS spawn the prefab even if a Player exists (replaces existing).")]
    [SerializeField] private bool preferPrefabOverExisting = true;

    [Tooltip("If replacing, spawn the prefab at the existing Player's pose (instead of spawn point).")]
    [SerializeField] private bool copyPoseFromExisting = false;

    [Tooltip("If replacing, destroy any existing Players after the prefab is spawned.")]
    [SerializeField] private bool destroyExistingOnReplace = true;

    [Header("Safe Spawn (2D)")]
    [Tooltip("Layers to avoid on spawn (e.g., your ScenePortal trigger layer).")]
    [SerializeField] private LayerMask avoidLayers;

    [Tooltip("Minimum gap to keep from avoided colliders (meters).")]
    [SerializeField, Min(0f)] private float clearance2D = 0.12f;

    [Tooltip("Extra push factor when separating from overlaps.")]
    [SerializeField, Min(1f)] private float nudgeMultiplier = 1.15f;

    [Tooltip("Max solver iterations to get fully clear.")]
    [SerializeField, Min(1)] private int maxResolveIterations = 8;

    [Tooltip("Temporarily disable the player's colliders while solving spawn clearance.")]
    [SerializeField] private bool disablePlayerCollidersWhileSolving = true;

    [Header("Portal Immunity On Spawn/Teleport")]
    [Tooltip("Portal/door trigger layers to ignore briefly after spawn/teleport.")]
    [SerializeField] private LayerMask portalTriggerLayers;

    [SerializeField, Min(0f)] private float portalImmunitySeconds = 0.40f;

    [Header("Cinemachine v3 Auto-Bind")]
    [SerializeField] private bool bindCinemachine = true;
    [SerializeField] private bool alsoSetLookAt = true;
    [SerializeField, Min(0f)] private float cameraBindRetrySeconds = 2f;

    [Header("Gizmos")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);
    [SerializeField, Min(0f)] private float gizmoRadius = 0.25f;

    private Player _player;

    // -------------------------- Lifecycle --------------------------

    private void Awake()
    {
        if (!playerPrefab)
        {
            Debug.LogError("[PlayerSpawner] No player prefab assigned!");
            return;
        }

        SpawnOrAdoptPlayer();
        RegisterPlayer();

        // Ensure safe spawn clearance (2D)
        if (_player)
            ResolveSpawnClearance2D(_player, avoidLayers, clearance2D, nudgeMultiplier, maxResolveIterations, disablePlayerCollidersWhileSolving);

        // Briefly ignore portals to prevent immediate re-trigger
        if (_player)
            StartCoroutine(TemporaryIgnorePortalsCo(_player, portalTriggerLayers, portalImmunitySeconds));
    }

    private void Start()
    {
        if (bindCinemachine)
            StartCoroutine(BindCameraRetryCo(cameraBindRetrySeconds));
    }

    private void OnValidate()
    {
        // Help catch common setup mistakes in the editor
        if (!playerPrefab) return;

        if (preferPrefabOverExisting == false && copyPoseFromExisting)
        {
            // copyPoseFromExisting only matters when replacing
            copyPoseFromExisting = false;
        }

        if (gizmoRadius < 0f) gizmoRadius = 0f;
        if (clearance2D < 0f) clearance2D = 0f;
        if (portalImmunitySeconds < 0f) portalImmunitySeconds = 0f;
        if (nudgeMultiplier < 1f) nudgeMultiplier = 1f;
        if (maxResolveIterations < 1) maxResolveIterations = 1;
    }

    // -------------------------- Public API --------------------------

    /// <summary>
    /// Repositions the current player to the given transform (applies 2D-safe clearance and portal immunity).
    /// </summary>
    public void RespawnAt(Transform t)
    {
        if (!_player || !t) return;
        Teleport2D(_player, t.position, t.rotation);

        ResolveSpawnClearance2D(_player, avoidLayers, clearance2D, nudgeMultiplier, maxResolveIterations, disablePlayerCollidersWhileSolving);
        StartCoroutine(TemporaryIgnorePortalsCo(_player, portalTriggerLayers, portalImmunitySeconds));
    }

    /// <summary>
    /// Returns the last spawned/adopted player.
    /// </summary>
    public Player GetPlayer() => _player;

    // -------------------------- Core Spawn Logic --------------------------

    private void SpawnOrAdoptPlayer()
    {
        var existingPlayers = FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool haveExisting = existingPlayers != null && existingPlayers.Length > 0;

        // Decide target pose
        Vector3 targetPos = spawnPoint ? spawnPoint.position : transform.position;
        Quaternion targetRot = spawnPoint ? spawnPoint.rotation : transform.rotation;
        Vector3 targetScale = Vector3.one;

        if (preferPrefabOverExisting)
        {
            // Replace existing, optionally copying their pose
            if (haveExisting && copyPoseFromExisting)
            {
                var first = existingPlayers[0].transform;
                targetPos = first.position;
                targetRot = first.rotation;
                targetScale = first.localScale;
            }

            _player = Instantiate(playerPrefab, targetPos, targetRot);
            _player.transform.localScale = targetScale;
            _player.name = playerPrefab.name;

            if (destroyExistingOnReplace && haveExisting)
            {
                foreach (var p in existingPlayers)
                    if (p && p.gameObject != _player.gameObject)
                        Destroy(p.gameObject);
            }
        }
        else
        {
            // Keep existing if found; otherwise spawn new at spawnPoint/this
            if (haveExisting)
            {
                _player = existingPlayers[0];
                Teleport2D(_player, targetPos, targetRot);
            }
            else
            {
                _player = Instantiate(playerPrefab, targetPos, targetRot);
                _player.name = playerPrefab.name;
            }
        }
    }

    private void RegisterPlayer()
    {
        // Optional: Register with your own GameManager, if available
        // Safely skip if no GM exists in your project
        try
        {
            GameManager.Instance?.RegisterPlayer(_player);
        }
        catch { /* ignore if no GameManager */ }
    }

    // -------------------------- Safe spawn solver (2D) --------------------------

    private static void ResolveSpawnClearance2D(Player player, LayerMask avoid, float minClear, float nudgeMul, int maxIters, bool disableColliders)
    {
        if (!player) return;

        // Collect player's 2D colliders
        var cols = player.GetComponentsInChildren<Collider2D>(true);
        if (cols == null || cols.Length == 0) return;

        // (Optional) temporarily disable to avoid triggering things while solving
        bool[] prevEnabled = null;
        if (disableColliders)
        {
            prevEnabled = new bool[cols.Length];
            for (int i = 0; i < cols.Length; i++)
            {
                prevEnabled[i] = cols[i].enabled;
                cols[i].enabled = false;
            }
        }

        // We'll work with the player's Rigidbody2D position for proper 2D physics semantics
        var rb = player.GetComponent<Rigidbody2D>();
        if (!rb)
        {
            // If no RB, fall back to transform adjustments
            Vector3 pos = player.transform.position;
            SolveClearanceLoops2D(cols, ref pos, null, avoid, minClear, nudgeMul, maxIters);
            player.transform.position = pos;
        }
        else
        {
            Vector2 pos = rb.position;
            SolveClearanceLoops2D(cols, ref pos, rb, avoid, minClear, nudgeMul, maxIters);
            rb.position = pos;
            rb.velocity = Vector2.zero; // avoid drift
            rb.angularVelocity = 0f;
        }

        // Re-enable colliders
        if (disableColliders && prevEnabled != null)
        {
            for (int i = 0; i < cols.Length; i++)
                cols[i].enabled = prevEnabled[i];
        }
    }

    // Overload for Rigidbody2D usage
    private static void SolveClearanceLoops2D(Collider2D[] playerCols, ref Vector2 rbPos, Rigidbody2D rb,
                                              LayerMask avoid, float minClear, float nudgeMul, int maxIters)
    {
        int iter = 0;
        while (iter++ < maxIters)
        {
            bool anyOverlap = false;

            foreach (var pc in playerCols)
            {
                if (!pc) continue;

                // Find potential contacts around this collider
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = avoid, useTriggers = true };
                Collider2D[] hits = new Collider2D[16];
                int count = Physics2D.OverlapCollider(pc, filter, hits);

                for (int i = 0; i < count; i++)
                {
                    var other = hits[i];
                    if (!other || other.attachedRigidbody == rb) continue;

                    // Measure signed distance
                    var d = Physics2D.Distance(pc, other);
                    if (d.isOverlapped || d.distance < minClear)
                    {
                        anyOverlap = true;

                        // Move along separation normal by required amount
                        float needed = (d.isOverlapped ? d.distance : (minClear - d.distance)) * nudgeMul;
                        Vector2 delta = d.normal * needed;

                        rbPos += delta;
                        if (rb) rb.position = rbPos; // update now so subsequent distances use fresh pose
                    }
                }
            }

            if (!anyOverlap) break;
        }
    }

    // Overload for transform usage (no Rigidbody2D)
    private static void SolveClearanceLoops2D(Collider2D[] playerCols, ref Vector3 trPos, Rigidbody2D rb,
                                              LayerMask avoid, float minClear, float nudgeMul, int maxIters)
    {
        int iter = 0;
        while (iter++ < maxIters)
        {
            bool anyOverlap = false;

            foreach (var pc in playerCols)
            {
                if (!pc) continue;

                var filter = new ContactFilter2D { useLayerMask = true, layerMask = avoid, useTriggers = true };
                Collider2D[] hits = new Collider2D[16];
                int count = Physics2D.OverlapCollider(pc, filter, hits);

                for (int i = 0; i < count; i++)
                {
                    var other = hits[i];
                    if (!other) continue;

                    var d = Physics2D.Distance(pc, other);
                    if (d.isOverlapped || d.distance < minClear)
                    {
                        anyOverlap = true;
                        float needed = (d.isOverlapped ? d.distance : (minClear - d.distance)) * nudgeMul;
                        Vector3 delta = (Vector3)(d.normal * needed);

                        trPos += delta;
                        pc.transform.root.position = trPos; // move entire player root
                    }
                }
            }

            if (!anyOverlap) break;
        }
    }

    private static void Teleport2D(Player p, Vector3 pos, Quaternion rot)
    {
        var rb = p.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.position = pos;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            p.transform.rotation = rot;
        }
        else
        {
            p.transform.SetPositionAndRotation(pos, rot);
        }
    }

    // -------------------------- Portal Immunity --------------------------

    private static IEnumerable<int> EnumerateLayers(LayerMask mask)
    {
        int v = mask.value;
        for (int i = 0; i < 32; i++)
            if ((v & (1 << i)) != 0) yield return i;
    }

    private IEnumerator TemporaryIgnorePortalsCo(Player p, LayerMask portalMask, float seconds)
    {
        if (!p || portalMask == 0 || seconds <= 0f) yield break;

        int playerLayer = p.gameObject.layer;
        var portalLayers = new List<int>(EnumerateLayers(portalMask));

        // 2D + 3D, in case your portals use 3D triggers in a 2D project.
        foreach (int l in portalLayers)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, l, true);
            Physics.IgnoreLayerCollision(playerLayer, l, true);
        }

        // Let physics settle a frame, then hold a short window
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(seconds);

        foreach (int l in portalLayers)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, l, false);
            Physics.IgnoreLayerCollision(playerLayer, l, false);
        }
    }

    // -------------------------- Cinemachine binding (optional) --------------------------

    private IEnumerator BindCameraRetryCo(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            var p = _player ? _player : FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            if (TryBindCameraOnce(p))
                yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

#if UNITY_CINEMACHINE
        Debug.LogWarning("[PlayerSpawner] Timed out binding Cinemachine camera to Player.");
#endif
    }

    private bool TryBindCameraOnce(Player p)
    {
        if (!p) return false;

#if UNITY_CINEMACHINE
        var vcam = PickBestVcam();
        if (vcam != null)
        {
            vcam.Follow = p.transform;
            if (alsoSetLookAt) vcam.LookAt = p.transform;
            return true;
        }
#endif
        return false;
    }

#if UNITY_CINEMACHINE
    private CinemachineCamera PickBestVcam()
    {
        var cams = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CinemachineCamera best = null;
        int bestPriority = int.MinValue;

        foreach (var cam in cams)
        {
            if (!cam || !cam.isActiveAndEnabled) continue;
            if (cam.Priority > bestPriority)
            {
                bestPriority = cam.Priority;
                best = cam;
            }
        }

        if (!best)
            best = FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Include);

        return best;
    }
#endif

    // -------------------------- Gizmos --------------------------

    private void OnDrawGizmosSelected()
    {
        if (!enabled) return;
        Gizmos.color = gizmoColor;
        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        Gizmos.DrawSphere(pos, gizmoRadius);
    }
}
