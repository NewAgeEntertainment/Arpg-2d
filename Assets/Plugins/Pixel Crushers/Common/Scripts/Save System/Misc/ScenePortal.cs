// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.SceneManagement; // NEW: only needed if you want to re-arm on each scene load.

namespace PixelCrushers
{
    [AddComponentMenu("")]
    public class ScenePortal : MonoBehaviour
    {
        [SerializeField] private string m_requiredTag = "Player";
        [SerializeField] private string m_destinationSceneName;
        [SerializeField] private string m_spawnpointNameInDestinationScene;
        [SerializeField] private UnityEngine.Events.UnityEvent m_onUsePortal = new UnityEngine.Events.UnityEvent();

        private bool m_isLoadingScene = false;

        [Header("Trigger Arming")]
        [SerializeField, Min(0f)] private float triggerActivationDelaySeconds = 0f;
        [SerializeField] private bool blockUsePortalUntilArmed = false;
        [SerializeField] private bool startDisarmedOnEnable = true;

        [SerializeField, Tooltip("Read-only at runtime; shows whether the portal is currently armed.")]
        private bool m_isArmed = true;

        private Coroutine _armCo;

        // NEW: Track whether the player is currently overlapping the trigger.
        private bool _playerInside = false;

        public string requiredTag { get => m_requiredTag; set => m_requiredTag = value; }
        public string destinationSceneName { get => m_destinationSceneName; set => m_destinationSceneName = value; }
        public string spawnpointNameInDestinationScene { get => m_spawnpointNameInDestinationScene; set => m_spawnpointNameInDestinationScene = value; }
        public bool isLoadingScene { get => m_isLoadingScene; set => m_isLoadingScene = value; }
        public UnityEngine.Events.UnityEvent onUsePortal => m_onUsePortal;
        public bool isArmed => m_isArmed;

        private void OnEnable()
        {
            // Optional: if this object persists across scenes, re-arm after each load:
            // SceneManager.sceneLoaded += OnSceneLoaded;

            if (startDisarmedOnEnable || triggerActivationDelaySeconds > 0f)
            {
                Disarm();
                StartArmingRoutine();
            }
            else
            {
                m_isArmed = true;
            }
        }

        private void OnDisable()
        {
            if (_armCo != null) StopCoroutine(_armCo);
            // SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Optional: if using DontDestroyOnLoad portals.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Disarm();
            StartArmingRoutine();
        }

        private void StartArmingRoutine()
        {
            if (_armCo != null) StopCoroutine(_armCo);
            _armCo = StartCoroutine(ArmAfterDelayAndClear(triggerActivationDelaySeconds));
        }

        public void ArmNow() => m_isArmed = true;
        public void Disarm() => m_isArmed = false;

        // NEW: Wait for both time delay *and* for the player to leave the trigger.
        private System.Collections.IEnumerator ArmAfterDelayAndClear(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // Wait until player is no longer overlapping.
            while (_playerInside)
            {
                yield return null;
            }

            m_isArmed = true;
            _armCo = null;
        }

        public virtual void UsePortal()
        {
            if (isLoadingScene) return;
            if (blockUsePortalUntilArmed && !m_isArmed) return;

            isLoadingScene = true;
            onUsePortal.Invoke();
            LoadScene();
        }

        protected void LoadScene()
        {
            SaveSystem.LoadScene(string.IsNullOrEmpty(spawnpointNameInDestinationScene)
                ? destinationSceneName
                : destinationSceneName + "@" + spawnpointNameInDestinationScene);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(requiredTag)) return;

            // NEW: always update overlap state, even when disarmed.
            _playerInside = true;

            if (!m_isArmed) return;
            UsePortal();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(requiredTag)) return;
            _playerInside = false; // NEW: we can arm once this becomes false.
        }

#if USE_PHYSICS2D
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(requiredTag)) return;

            _playerInside = true;

            if (!m_isArmed) return;
            UsePortal();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(requiredTag)) return;
            _playerInside = false;
        }
#endif
    }
}
