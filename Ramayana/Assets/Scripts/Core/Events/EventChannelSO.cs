// Generic base for payload event channels. Publishers raise, listeners register, and neither knows the other.
// For cross-system and cross-scene signals only. Not for per-frame data or same-prefab communication.
using System;
using UnityEngine;

namespace JourneyToLanka.Core.Events
{
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        [SerializeField, TextArea(2, 4)] private string _description;

        private Action<T> _raised;

        public string Description => _description;

        public void Register(Action<T> listener)
        {
            if (listener == null)
            {
                return;
            }

            _raised -= listener;
            _raised += listener;
        }

        public void Unregister(Action<T> listener)
        {
            if (listener == null)
            {
                return;
            }

            _raised -= listener;
        }

        public void Raise(T value)
        {
            _raised?.Invoke(value);
        }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#endif
        }

        protected virtual void OnDisable()
        {
            _raised = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void HandlePlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                _raised = null;
            }
        }
#endif
    }
}
