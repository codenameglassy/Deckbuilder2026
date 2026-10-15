// Event channel with no payload, for signals such as run started or pause requested.
// Separate from EventChannelSO<T> because Action<void> is not valid C#.
using System;
using UnityEngine;

namespace JourneyToLanka.Core.Events
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Events/Void Event Channel", fileName = "VoidEventChannel")]
    public sealed class VoidEventChannelSO : ScriptableObject
    {
        [SerializeField, TextArea(2, 4)] private string _description;

        private Action _raised;

        public string Description => _description;

        public void Register(Action listener)
        {
            if (listener == null)
            {
                return;
            }

            _raised -= listener;
            _raised += listener;
        }

        public void Unregister(Action listener)
        {
            if (listener == null)
            {
                return;
            }

            _raised -= listener;
        }

        public void Raise()
        {
            _raised?.Invoke();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#endif
        }

        private void OnDisable()
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
