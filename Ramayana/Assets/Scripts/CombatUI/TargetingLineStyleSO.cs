// Every value for the card-to-enemy targeting line: the material carrying the Targeting Line shader, the strip's
// segment count, the ribbon and arrowhead shape, colours (and how much dimmer the line is while a dragged card points
// at empty space instead of an enemy), the flowing chevrons and shimmer, how the curve bends (how high it rises from
// the card, how steeply it dives onto the target and how long that approach is), how far above the enemy's top it
// lands, and the reveal, fade, snap and pulse timings. The presenter copies these onto its own runtime copy of the
// material, so this asset and the material asset are never changed at runtime. Read-only at runtime; in the Editor,
// changing a value raises Changed so the line re-applies it live in Play mode.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Targeting Line Style", fileName = "TargetingLineStyle")]
    public sealed class TargetingLineStyleSO : ScriptableObject
    {
        [Header("Material")]
        [SerializeField] private Material _material;
        [SerializeField, Range(8, 128)] private int _segments = 48;

        [Header("Shape (canvas units)")]
        [SerializeField, Min(0.5f)] private float _widthAtCard = 8f;
        [SerializeField, Min(0.5f)] private float _widthAtTarget = 20f;
        [SerializeField, Min(1f)] private float _glowScale = 3f;
        [SerializeField, Range(0.05f, 1f)] private float _coreWidth = 0.35f;
        [SerializeField, Min(0.1f)] private float _glowFalloff = 2.2f;
        [SerializeField, Min(1f)] private float _headLength = 60f;
        [SerializeField, Min(1f)] private float _headWidth = 56f;
        [SerializeField, Range(0f, 0.5f)] private float _startFade = 0.08f;

        [Header("Colour")]
        [SerializeField] private Color _bodyColorAtCard = new Color(1f, 0.55f, 0.15f, 0.95f);
        [SerializeField] private Color _bodyColorAtTarget = new Color(1f, 0.8f, 0.35f, 1f);
        [SerializeField] private Color _coreColor = new Color(1f, 0.97f, 0.86f, 1f);
        [SerializeField] private Color _glowColor = new Color(1f, 0.5f, 0.1f, 0.55f);
        [SerializeField] private Color _chevronColor = new Color(1f, 1f, 0.9f, 1f);
        [SerializeField, Min(0f)] private float _intensity = 1f;
        [Tooltip("Intensity multiplier while a dragged card points at empty space rather than an enemy.")]
        [SerializeField, Range(0f, 1f)] private float _unlockedIntensity = 0.55f;

        [Header("Flow")]
        [SerializeField, Min(1f)] private float _chevronSpacing = 34f;
        [SerializeField, Range(0.05f, 0.95f)] private float _chevronDuty = 0.35f;
        [SerializeField, Min(0f)] private float _chevronSlant = 0.9f;
        [SerializeField] private float _flowSpeed = 1.6f;
        [SerializeField, Min(0f)] private float _chevronIntensity = 0.8f;
        [SerializeField, Range(0f, 0.5f)] private float _shimmerAmount = 0.12f;

        [Header("Curve")]
        [SerializeField, Min(0f)] private float _curveRise = 0.45f;
        [SerializeField, Range(0f, 2f)] private float _arrivalDrop = 1.15f;
        [SerializeField, Range(0.05f, 1f)] private float _arrivalPull = 0.4f;
        [SerializeField] private float _targetLift = 0.15f;

        [Header("Motion")]
        [SerializeField, Min(0.01f)] private float _revealDuration = 0.14f;
        [SerializeField, Min(0.01f)] private float _fadeOutDuration = 0.08f;
        [SerializeField, Min(0f)] private float _snapSmoothTime = 0.06f;
        [SerializeField, Min(0.01f)] private float _pulseDuration = 0.25f;
        [SerializeField, Min(0f)] private float _pulseWiden = 0.35f;
        [SerializeField, Min(0f)] private float _pulseBrighten = 0.6f;

        public event Action Changed;

        public Material Material => _material;
        public int Segments => _segments;

        public float WidthAtCard => _widthAtCard;
        public float WidthAtTarget => _widthAtTarget;
        public float GlowScale => _glowScale;
        public float CoreWidth => _coreWidth;
        public float GlowFalloff => _glowFalloff;
        public float HeadLength => _headLength;
        public float HeadWidth => _headWidth;
        public float StartFade => _startFade;

        public Color BodyColorAtCard => _bodyColorAtCard;
        public Color BodyColorAtTarget => _bodyColorAtTarget;
        public Color CoreColor => _coreColor;
        public Color GlowColor => _glowColor;
        public Color ChevronColor => _chevronColor;
        public float Intensity => _intensity;
        public float UnlockedIntensity => _unlockedIntensity;

        public float ChevronSpacing => _chevronSpacing;
        public float ChevronDuty => _chevronDuty;
        public float ChevronSlant => _chevronSlant;
        public float FlowSpeed => _flowSpeed;
        public float ChevronIntensity => _chevronIntensity;
        public float ShimmerAmount => _shimmerAmount;

        public float CurveRise => _curveRise;
        public float ArrivalDrop => _arrivalDrop;
        public float ArrivalPull => _arrivalPull;
        public float TargetLift => _targetLift;

        public float RevealDuration => _revealDuration;
        public float FadeOutDuration => _fadeOutDuration;
        public float SnapSmoothTime => _snapSmoothTime;
        public float PulseDuration => _pulseDuration;
        public float PulseWiden => _pulseWiden;
        public float PulseBrighten => _pulseBrighten;

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
