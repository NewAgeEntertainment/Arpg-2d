using UnityEngine;
using UnityEngine.Events;
using PixelCrushers; // for MessageSystem (Quest Machine)

namespace PixelCrushers.EntityHealthSupport
{
    /// <summary>
    /// Exposes UnityEvents for OnDeath / OnRevive from Entity_Health.
    /// (Optional) can also send a Quest Machine Message on death.
    /// </summary>
    [RequireComponent(typeof(Entity_Health))]
    public class OnDeathEvent : MonoBehaviour
    {
        [Header("Unity Events")]
        public UnityEvent OnDeath = new UnityEvent();
        public UnityEvent OnRevive = new UnityEvent();

        [Header("Quest Machine (optional)")]
        [Tooltip("If true, send a Quest Machine Message on death.")]
        public bool sendQuestMessageOnDeath = false;

        [Tooltip("Message name listened to by Quest conditions (e.g., \"killed\").")]
        public string deathMessage = "killed";

        [Tooltip("Parameter value; if empty and UseGOName is true, uses this GameObject's name.")]
        public string deathParameter = "";

        [Tooltip("If parameter is empty, use this GameObject's name.")]
        public bool useGONameIfParameterEmpty = true;

        [Tooltip("Optional target for MessageSystem.SendMessage. Leave null to broadcast.")]
        public Transform messageTarget;

        private Entity_Health _health;

        private void Awake()
        {
            _health = GetComponent<Entity_Health>();
        }

        private void OnEnable()
        {
            if (_health == null) _health = GetComponent<Entity_Health>();
            _health.OnDied += InvokeOnDeathEvent;
            _health.OnRevived += InvokeOnReviveEvent; // requires step #1 above
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.OnDied -= InvokeOnDeathEvent;
            _health.OnRevived -= InvokeOnReviveEvent;
        }

        private void InvokeOnDeathEvent()
        {
            OnDeath.Invoke();

            if (sendQuestMessageOnDeath)
            {
                var param = (!string.IsNullOrEmpty(deathParameter))
                            ? deathParameter
                            : (useGONameIfParameterEmpty ? gameObject.name : string.Empty);

                MessageSystem.SendMessage(this, deathMessage, param, messageTarget);
                // Example Quest Machine condition:
                //   Message = "killed", Parameter = "Bandit" (or this GO's name)
            }
        }

        private void InvokeOnReviveEvent()
        {
            OnRevive.Invoke();
        }
    }
}
