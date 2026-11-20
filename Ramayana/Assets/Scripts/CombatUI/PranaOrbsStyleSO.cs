// Every value for Prana on the combat HUD: the Prana Orb shader's material, orb size and cap, the arc the orbs sit on
// around the medallion (radius, spacing, direction), the medallion's size, gold ring and face, the "current/per turn"
// number in it, the orb's colours and shape, how it swirls and pulses, how orbs drain when spent and light up when
// refilled (durations, stagger, flash), how quickly a card's cost preview fades in and out, and the shake and red flash
// when a card is rejected for lack of Prana. Referenced by the Combat HUD style. Read-only at runtime; in the Editor,
// changing a value raises Changed so the orbs re-apply it live in Play mode.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Prana Orbs Style", fileName = "PranaOrbsStyle")]
    public sealed class PranaOrbsStyleSO : ScriptableObject
    {
        [Header("Material")]
        [SerializeField] private Material _orbMaterial;

        [Header("Layout (canvas units)")]
        [SerializeField, Min(4f)] private float _orbDiameter = 34f;
        [SerializeField, Range(1, 20)] private int _maxOrbs = 10;
        [Tooltip("Distance from the medallion's centre to each orb's centre.")]
        [SerializeField, Min(0f)] private float _arcRadius = 70f;
        [Tooltip("Angle between neighbouring orbs. It shrinks on its own when bonus orbs would wrap past a full circle.")]
        [SerializeField, Range(5f, 90f)] private float _arcStepDegrees = 38f;
        [Tooltip("Direction of the middle of the arc: 0 points right (toward the hand), 90 up.")]
        [SerializeField, Range(-180f, 180f)] private float _arcCentreDegrees;

        [Header("Medallion")]
        [SerializeField, Min(8f)] private float _medallionDiameter = 84f;
        [SerializeField, Min(0f)] private float _medallionRingWidth = 4f;
        [SerializeField] private Color _medallionRingColor = new Color(0.95f, 0.78f, 0.42f, 1f);
        [SerializeField] private Color _medallionFaceColor = new Color(0.08f, 0.06f, 0.045f, 0.95f);
        [SerializeField, Min(1f)] private float _valueFontSize = 26f;
        [SerializeField] private Color _valueColor = new Color(0.96f, 0.91f, 0.8f);

        [Header("Look")]
        [SerializeField] private Color _coreColor = new Color(1f, 0.96f, 0.8f);
        [SerializeField] private Color _glowColor = new Color(1f, 0.62f, 0.18f);
        [SerializeField] private Color _socketColor = new Color(0.06f, 0.045f, 0.03f, 0.9f);
        [SerializeField] private Color _rimColor = new Color(0.95f, 0.78f, 0.42f, 0.75f);
        [SerializeField] private Color _previewColor = Color.white;
        [SerializeField] private Color _deficitColor = new Color(1f, 0.25f, 0.2f);
        [Tooltip("Orb radius as a fraction of its quad's half size; the rest of the quad holds the glow.")]
        [SerializeField, Range(0.2f, 0.95f)] private float _orbRadius = 0.62f;
        [SerializeField, Range(0f, 0.2f)] private float _rimWidth = 0.05f;
        [SerializeField, Range(0.01f, 0.5f)] private float _glowWidth = 0.22f;
        [SerializeField, Range(0f, 2f)] private float _glowStrength = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _swirlAmount = 0.2f;
        [SerializeField] private float _swirlSpeed = 1f;
        [SerializeField, Min(0f)] private float _pulseSpeed = 1.6f;

        [Header("Motion")]
        [SerializeField, Min(0.01f)] private float _drainDuration = 0.28f;
        [SerializeField, Min(0f)] private float _drainStagger = 0.07f;
        [SerializeField, Min(0.01f)] private float _refillDuration = 0.32f;
        [SerializeField, Min(0f)] private float _refillStagger = 0.09f;
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.4f;
        [SerializeField, Min(0.01f)] private float _previewFadeDuration = 0.12f;
        [SerializeField, Min(0f)] private float _rejectShakeStrength = 12f;
        [SerializeField, Min(0.01f)] private float _rejectShakeDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float _deficitFlashDuration = 0.5f;

        public event Action Changed;

        public Material OrbMaterial => _orbMaterial;

        public float OrbDiameter => _orbDiameter;
        public int MaxOrbs => _maxOrbs;
        public float ArcRadius => _arcRadius;
        public float ArcStepDegrees => _arcStepDegrees;
        public float ArcCentreDegrees => _arcCentreDegrees;

        public float MedallionDiameter => _medallionDiameter;
        public float MedallionRingWidth => _medallionRingWidth;
        public Color MedallionRingColor => _medallionRingColor;
        public Color MedallionFaceColor => _medallionFaceColor;
        public float ValueFontSize => _valueFontSize;
        public Color ValueColor => _valueColor;

        public Color CoreColor => _coreColor;
        public Color GlowColor => _glowColor;
        public Color SocketColor => _socketColor;
        public Color RimColor => _rimColor;
        public Color PreviewColor => _previewColor;
        public Color DeficitColor => _deficitColor;
        public float OrbRadius => _orbRadius;
        public float RimWidth => _rimWidth;
        public float GlowWidth => _glowWidth;
        public float GlowStrength => _glowStrength;
        public float SwirlAmount => _swirlAmount;
        public float SwirlSpeed => _swirlSpeed;
        public float PulseSpeed => _pulseSpeed;

        public float DrainDuration => _drainDuration;
        public float DrainStagger => _drainStagger;
        public float RefillDuration => _refillDuration;
        public float RefillStagger => _refillStagger;
        public float FlashDuration => _flashDuration;
        public float PreviewFadeDuration => _previewFadeDuration;
        public float RejectShakeStrength => _rejectShakeStrength;
        public float RejectShakeDuration => _rejectShakeDuration;
        public float DeficitFlashDuration => _deficitFlashDuration;

        public float OrbQuadSize => _orbDiameter / Mathf.Max(_orbRadius, 0.01f);

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
