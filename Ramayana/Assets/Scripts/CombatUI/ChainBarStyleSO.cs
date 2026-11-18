// Visual values for the combo chain bar above the hand: where it sits (from the bottom centre of the screen), its
// width, padding and row spacing; the title row (chain icon, with a fallback disc tinted in the chain's colour when
// the chain kind has no icon, the chain's name and the count of cards in a row); the hint line; the pips (one per
// ladder step, lit in the chain's colour, the next one half lit, the finisher pip larger) and the multiplier label
// under each; show and hide fades; the tier flash (a panel punch and a colour flash that grow with the multiplier);
// the Finisher tag; and the "Broken at N in a row" message, with the shortest broken chain worth announcing. Panel
// background, font and text colour come from the Combat HUD style so the bar matches the other panels. Read-only at
// runtime; in the Editor, Changed lets the HUD re-apply it live.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Chain Bar Style", fileName = "ChainBarStyle")]
    public sealed class ChainBarStyleSO : ScriptableObject
    {
        [Header("Panel")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 330f);
        [SerializeField, Min(100f)] private float _width = 440f;
        [SerializeField, Min(0f)] private float _padding = 12f;
        [SerializeField, Min(0f)] private float _rowSpacing = 4f;

        [Header("Title")]
        [SerializeField, Min(8f)] private float _iconSize = 34f;
        [SerializeField] private Sprite _fallbackIconSprite;
        [SerializeField, Min(0f)] private float _iconGap = 8f;
        [SerializeField, Min(1f)] private float _titleFontSize = 26f;
        [SerializeField, Min(1f)] private float _countFontSize = 34f;
        [SerializeField, Min(10f)] private float _countWidth = 64f;

        [Header("Hint")]
        [SerializeField, Min(1f)] private float _hintFontSize = 18f;
        [SerializeField] private Color _hintColor = new Color(0.96f, 0.91f, 0.8f, 0.75f);

        [Header("Pips")]
        [SerializeField, Min(4f)] private float _pipSize = 28f;
        [SerializeField] private Sprite _pipSprite;
        [SerializeField] private Color _pipEmptyColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField, Range(0f, 1f)] private float _nextPipAlpha = 0.45f;
        [SerializeField, Min(1f)] private float _finisherPipScale = 1.3f;
        [SerializeField, Min(1f)] private float _pipLabelFontSize = 16f;
        [SerializeField, Min(0f)] private float _pipLabelGap = 2f;
        [SerializeField] private Color _pipLabelColor = new Color(0.96f, 0.91f, 0.8f, 0.45f);
        [SerializeField] private Color _pipLabelLitColor = new Color(1f, 0.97f, 0.9f, 1f);
        [SerializeField, Min(0f)] private float _pipPunchScale = 0.4f;
        [SerializeField, Min(0.01f)] private float _pipPunchDuration = 0.25f;

        [Header("Show and Hide")]
        [SerializeField, Min(0.01f)] private float _fadeIn = 0.15f;
        [SerializeField, Min(0.01f)] private float _fadeOut = 0.25f;

        [Header("Tier Flash (grows with the multiplier above x1)")]
        [SerializeField, Min(0f)] private float _punchBase = 0.04f;
        [SerializeField, Min(0f)] private float _punchPerMultiplier = 0.06f;
        [SerializeField, Min(0.01f)] private float _punchDuration = 0.28f;
        [SerializeField, Min(0f)] private float _flashAlphaPerMultiplier = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _flashMaxAlpha = 0.8f;
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.35f;

        [Header("Finisher")]
        [SerializeField, Min(1f)] private float _finisherFontSize = 40f;
        [SerializeField] private Vector2 _finisherOffset = new Vector2(0f, 6f);
        [SerializeField, Range(0.1f, 1f)] private float _finisherStartScale = 0.6f;
        [SerializeField, Min(0.01f)] private float _finisherPopDuration = 0.22f;
        [SerializeField, Min(0f)] private float _finisherHold = 0.7f;

        [Header("Broken Message")]
        [SerializeField, Min(1)] private int _brokenMinLength = 2;
        [SerializeField] private Vector2 _brokenOffset = new Vector2(0f, 470f);
        [SerializeField] private Vector2 _brokenSize = new Vector2(520f, 48f);
        [SerializeField, Min(1f)] private float _brokenFontSize = 28f;
        [SerializeField] private Color _brokenColor = new Color(1f, 0.55f, 0.45f, 1f);
        [SerializeField, Min(0f)] private float _brokenRise = 24f;
        [SerializeField, Min(0.01f)] private float _brokenFadeIn = 0.1f;
        [SerializeField, Min(0f)] private float _brokenHold = 0.8f;
        [SerializeField, Min(0.01f)] private float _brokenFadeOut = 0.3f;

        public event Action Changed;

        public Vector2 Offset => _offset;
        public float Width => _width;
        public float Padding => _padding;
        public float RowSpacing => _rowSpacing;
        public float IconSize => _iconSize;
        public Sprite FallbackIconSprite => _fallbackIconSprite;
        public float IconGap => _iconGap;
        public float TitleFontSize => _titleFontSize;
        public float CountFontSize => _countFontSize;
        public float CountWidth => _countWidth;
        public float HintFontSize => _hintFontSize;
        public Color HintColor => _hintColor;
        public float PipSize => _pipSize;
        public Sprite PipSprite => _pipSprite;
        public Color PipEmptyColor => _pipEmptyColor;
        public float NextPipAlpha => _nextPipAlpha;
        public float FinisherPipScale => _finisherPipScale;
        public float PipLabelFontSize => _pipLabelFontSize;
        public float PipLabelGap => _pipLabelGap;
        public Color PipLabelColor => _pipLabelColor;
        public Color PipLabelLitColor => _pipLabelLitColor;
        public float PipPunchScale => _pipPunchScale;
        public float PipPunchDuration => _pipPunchDuration;
        public float FadeIn => _fadeIn;
        public float FadeOut => _fadeOut;
        public float PunchBase => _punchBase;
        public float PunchPerMultiplier => _punchPerMultiplier;
        public float PunchDuration => _punchDuration;
        public float FlashAlphaPerMultiplier => _flashAlphaPerMultiplier;
        public float FlashMaxAlpha => _flashMaxAlpha;
        public float FlashDuration => _flashDuration;
        public float FinisherFontSize => _finisherFontSize;
        public Vector2 FinisherOffset => _finisherOffset;
        public float FinisherStartScale => _finisherStartScale;
        public float FinisherPopDuration => _finisherPopDuration;
        public float FinisherHold => _finisherHold;
        public int BrokenMinLength => _brokenMinLength;
        public Vector2 BrokenOffset => _brokenOffset;
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
