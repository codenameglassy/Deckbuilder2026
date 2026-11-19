// Visual values for the combo chain bead string at the top centre of the screen: where it hangs (from the top centre),
// the width of the bead row and an optional backing behind the whole widget; the chain's name above the string; the
// beads (one per ladder step, threaded on a thin line, lit in the chain's colour, the next one half lit, the finisher
// bead larger) and the ring that marks the bead just played; the current multiplier under the string; show and hide
// fades; the tier flash (a halo behind the bead just played plus a punch on that bead and on the multiplier, all
// growing with the multiplier); the Finisher tag; and the "Broken at N in a row" message, with the shortest broken
// chain worth announcing. Font and text colour come from the Combat HUD style. Read-only at runtime; in the Editor,
// Changed lets the HUD re-apply it live.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Chain Bar Style", fileName = "ChainBarStyle")]
    public sealed class ChainBarStyleSO : ScriptableObject
    {
        [Header("Placement (from the top centre of the screen)")]
        [SerializeField] private Vector2 _topOffset = new Vector2(0f, -28f);
        [SerializeField, Min(40f)] private float _width = 300f;
        [SerializeField] private Sprite _backingSprite;
        [SerializeField] private Color _backingColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField, Min(0f)] private float _backingPadding = 10f;

        [Header("Name")]
        [SerializeField, Min(1f)] private float _nameFontSize = 22f;
        [SerializeField, Min(0f)] private float _nameGap = 2f;

        [Header("Beads")]
        [SerializeField] private Sprite _beadSprite;
        [SerializeField, Min(4f)] private float _beadSize = 22f;
        [SerializeField, Min(1f)] private float _finisherBeadScale = 1.4f;
        [SerializeField] private Color _beadEmptyColor = new Color(0.37f, 0.37f, 0.35f, 0.9f);
        [SerializeField, Range(0f, 1f)] private float _nextBeadAlpha = 0.45f;
        [SerializeField, Min(0f)] private float _threadThickness = 2f;
        [SerializeField] private Color _threadColor = new Color(0.53f, 0.53f, 0.5f, 0.8f);
        [SerializeField, Min(1f)] private float _currentRingScale = 1.45f;
        [SerializeField] private Color _currentRingColor = new Color(0.98f, 0.93f, 0.85f, 0.9f);

        [Header("Multiplier")]
        [SerializeField, Min(1f)] private float _multiplierFontSize = 34f;
        [SerializeField, Min(0f)] private float _multiplierGap = 0f;

        [Header("Show and Hide")]
        [SerializeField, Min(0.01f)] private float _fadeIn = 0.15f;
        [SerializeField, Min(0.01f)] private float _fadeOut = 0.25f;

        [Header("Tier Flash (grows with the multiplier above x1)")]
        [SerializeField] private Sprite _haloSprite;
        [SerializeField, Min(0f)] private float _haloScale = 2.2f;
        [SerializeField, Min(0f)] private float _haloScalePerMultiplier = 0.8f;
        [SerializeField, Min(0f)] private float _flashAlphaPerMultiplier = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _flashMaxAlpha = 0.9f;
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.4f;
        [SerializeField, Min(0f)] private float _beadPunchBase = 0.25f;
        [SerializeField, Min(0f)] private float _beadPunchPerMultiplier = 0.2f;
        [SerializeField, Min(0f)] private float _multiplierPunchBase = 0.08f;
        [SerializeField, Min(0f)] private float _multiplierPunchPerMultiplier = 0.12f;
        [SerializeField, Min(0.01f)] private float _punchDuration = 0.28f;

        [Header("Finisher")]
        [SerializeField, Min(1f)] private float _finisherFontSize = 30f;
        [SerializeField, Min(0f)] private float _finisherGap = 0f;
        [SerializeField, Range(0.1f, 1f)] private float _finisherStartScale = 0.6f;
        [SerializeField, Min(0.01f)] private float _finisherPopDuration = 0.22f;
        [SerializeField, Min(0f)] private float _finisherHold = 0.7f;

        [Header("Broken Message (from the top centre of the screen)")]
        [SerializeField, Min(1)] private int _brokenMinLength = 2;
        [SerializeField] private Vector2 _brokenTopOffset = new Vector2(0f, -150f);
        [SerializeField] private Vector2 _brokenSize = new Vector2(520f, 44f);
        [SerializeField, Min(1f)] private float _brokenFontSize = 26f;
        [SerializeField] private Color _brokenColor = new Color(1f, 0.55f, 0.45f, 1f);
        [SerializeField] private float _brokenRise = 16f;
        [SerializeField, Min(0.01f)] private float _brokenFadeIn = 0.1f;
        [SerializeField, Min(0f)] private float _brokenHold = 0.8f;
        [SerializeField, Min(0.01f)] private float _brokenFadeOut = 0.3f;

        public event Action Changed;

        public Vector2 TopOffset => _topOffset;
        public float Width => _width;
        public Sprite BackingSprite => _backingSprite;
        public Color BackingColor => _backingColor;
        public float BackingPadding => _backingPadding;
        public float NameFontSize => _nameFontSize;
        public float NameGap => _nameGap;
        public Sprite BeadSprite => _beadSprite;
        public float BeadSize => _beadSize;
        public float FinisherBeadScale => _finisherBeadScale;
        public Color BeadEmptyColor => _beadEmptyColor;
        public float NextBeadAlpha => _nextBeadAlpha;
        public float ThreadThickness => _threadThickness;
        public Color ThreadColor => _threadColor;
        public float CurrentRingScale => _currentRingScale;
        public Color CurrentRingColor => _currentRingColor;
        public float MultiplierFontSize => _multiplierFontSize;
        public float MultiplierGap => _multiplierGap;
        public float FadeIn => _fadeIn;
        public float FadeOut => _fadeOut;
        public Sprite HaloSprite => _haloSprite;
        public float HaloScale => _haloScale;
        public float HaloScalePerMultiplier => _haloScalePerMultiplier;
        public float FlashAlphaPerMultiplier => _flashAlphaPerMultiplier;
        public float FlashMaxAlpha => _flashMaxAlpha;
        public float FlashDuration => _flashDuration;
        public float BeadPunchBase => _beadPunchBase;
        public float BeadPunchPerMultiplier => _beadPunchPerMultiplier;
        public float MultiplierPunchBase => _multiplierPunchBase;
        public float MultiplierPunchPerMultiplier => _multiplierPunchPerMultiplier;
        public float PunchDuration => _punchDuration;
        public float FinisherFontSize => _finisherFontSize;
        public float FinisherGap => _finisherGap;
        public float FinisherStartScale => _finisherStartScale;
        public float FinisherPopDuration => _finisherPopDuration;
        public float FinisherHold => _finisherHold;
        public int BrokenMinLength => _brokenMinLength;
        public Vector2 BrokenTopOffset => _brokenTopOffset;
        public Vector2 BrokenSize => _brokenSize;
        public float BrokenFontSize => _brokenFontSize;
        public Color BrokenColor => _brokenColor;
        public float BrokenRise => _brokenRise;
        public float BrokenFadeIn => _brokenFadeIn;
        public float BrokenHold => _brokenHold;
        public float BrokenFadeOut => _brokenFadeOut;

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
