// Every visual value for a combatant view: label font, size, colours and offsets, the tint for a fallen fighter,
// the hit reaction (flash and shake), the Kavach gain pulse, and where damage numbers appear. Read-only at runtime.
// In the Editor, changing a value raises Changed so views in Play mode re-apply it at once without rebuilding.
using System;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Combatant View Style", fileName = "CombatantViewStyle")]
    public sealed class CombatantViewStyleSO : ScriptableObject
    {
        [Header("Labels")]
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField, Min(0.1f)] private float _fontSize = 5f;
        [SerializeField] private Vector2 _labelSize = new Vector2(4f, 1f);
        [SerializeField] private Color _healthColor = new Color(0.95f, 0.4f, 0.35f);
        [SerializeField] private Vector3 _healthOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private Color _blockColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Vector3 _blockOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private Color _fallenTint = new Color(0.25f, 0.25f, 0.25f);

        [Header("Hit Reaction")]
        [SerializeField] private Color _flashColor = new Color(1f, 0.35f, 0.3f);
        [SerializeField, Min(0f)] private float _flashDuration = 0.18f;
        [SerializeField, Min(0f)] private float _shakeDuration = 0.25f;
        [SerializeField, Min(0f)] private float _shakeStrength = 0.12f;
        [SerializeField, Min(0f)] private float _shakeFrequency = 40f;

        [Header("Kavach Gain")]
        [SerializeField, Min(0f)] private float _blockPulseScale = 0.12f;
        [SerializeField, Min(0f)] private float _blockPulseDuration = 0.2f;

        [Header("Damage Numbers")]
        [SerializeField] private Vector3 _damageNumberOffset = new Vector3(0f, 0.6f, -0.6f);

        public event Action Changed;

        public TMP_FontAsset Font => _font;
        public float FontSize => _fontSize;
        public Vector2 LabelSize => _labelSize;
        public Color HealthColor => _healthColor;
        public Vector3 HealthOffset => _healthOffset;
        public Color BlockColor => _blockColor;
        public Vector3 BlockOffset => _blockOffset;
        public Color FallenTint => _fallenTint;

        public Color FlashColor => _flashColor;
        public float FlashDuration => _flashDuration;
        public float ShakeDuration => _shakeDuration;
        public float ShakeStrength => _shakeStrength;
        public float ShakeFrequency => _shakeFrequency;

        public float BlockPulseScale => _blockPulseScale;
        public float BlockPulseDuration => _blockPulseDuration;

        public Vector3 DamageNumberOffset => _damageNumberOffset;

        private void OnDisable()
        {
            Changed = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}
