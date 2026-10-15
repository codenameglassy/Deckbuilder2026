// Generic base for runtime anchors: a shared slot one scene object provides and others read, replacing Find calls.
// Readers must null check Value on every read, since the provider can be disabled or destroyed at any time.
using UnityEngine;

namespace JourneyToLanka.Core.Anchors
{
    public abstract class RuntimeAnchorSO<T> : ScriptableObject where T : Object
    {
        [SerializeField, TextArea(2, 4)] private string _description;

        private T _value;

        public string Description => _description;
        public T Value => _value;
        public bool IsSet => _value != null;

        public bool TryGetValue(out T value)
        {
            value = _value;
            return value != null;
        }

        public void Provide(T value)
        {
            if (value == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"{name}: Provide was called with null. Use Unset to clear the anchor.", this);
#endif
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_value != null && !ReferenceEquals(_value, value))
            {
                Debug.LogWarning($"{name}: value '{_value.name}' was replaced by '{value.name}'. Only one provider should be active at a time.", this);
            }
#endif

            _value = value;
        }

        public void Unset(T provider)
        {
            if (ReferenceEquals(_value, provider))
            {
                _value = null;
            }
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
            _value = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void HandlePlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                _value = null;
            }
        }
#endif
    }
}
