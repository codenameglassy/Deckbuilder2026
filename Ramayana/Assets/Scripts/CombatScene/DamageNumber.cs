// One floating damage number: pops in, rises and fades over the style's duration. Pooled by DamageNumberSpawner,
// which advances every active number from one tick, so numbers have no Update of their own.
using JourneyToLanka.Core.Pooling;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    public sealed class DamageNumber : MonoBehaviour, IPoolable
    {
        [SerializeField] private TextMeshPro _label;

        private Transform _transform;
        private Vector3 _start;
        private float _elapsed;
        private float _duration = 1f;
        private float _rise;
        private float _pop = 1f;

        internal void Initialize(TextMeshPro label)
        {
            _label = label;
        }

        private void Awake()
        {
            _transform = transform;
        }

        public void Play(Vector3 worldPosition, int value, Color color, CombatFeedbackStyleSO style)
        {
            if (style.NumberFont != null)
            {
                _label.font = style.NumberFont;
            }

            _label.fontSize = style.NumberFontSize;
            _label.color = color;
            _label.SetText("{0}", (float)value);

            _start = worldPosition;
            _duration = style.NumberDuration;
            _rise = style.NumberRise;
            _pop = style.NumberPopScale;
            _elapsed = 0f;
            Apply(0f);
        }

        public bool Advance(float deltaTime)
        {
            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            Apply(t);
            return t >= 1f;
        }

        public void OnTakenFromPool()
        {
            _elapsed = 0f;
            _label.alpha = 1f;
            _transform.localScale = Vector3.one;
        }

        public void OnReturnedToPool()
        {
            _elapsed = 0f;
        }

        private void Apply(float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            _transform.position = _start + Vector3.up * (_rise * eased);
            _transform.localScale = Vector3.one * Mathf.Lerp(_pop, 1f, Mathf.Clamp01(t * 4f));
            _label.alpha = 1f - t * t;
        }
    }
}
