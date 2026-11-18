// Every visual value of the combat HUD and its cards: canvas reference resolution, the hand area and its layout
// strategy (a swappable HandLayoutSO asset), how quickly cards ease into place and how far a targeted card rises,
// drag to play (the play line, the dragged card's scale, follow speed and tilt), hand life (the playable glow, the
// spring and stagger of the hand settling, the arc, flip and swell of a drawn card, the arc and spin of the
// end-of-turn sweep), card size, colours (including the cost badge when the card is unaffordable, and description
// numbers that are buffed or weakened), the card frame (the Card Frame shader's material and values, the visuals for
// each card type, the description panel and the type icon), fonts and text sizes, the lifted card's scale while
// targeting, card motion (draw, play, discard, reshuffle), the rejected-card wiggle and message, panel and button
// placement, the Prana orbs (their own PranaOrbsStyleSO), Rama's quiver (its own QuiverStyleSO), the End Turn glow
// when nothing is playable, the threat panel, the turn banner and its timing, and the result banner.
// Sprites are optional; without one an element draws as a flat colour. A Card Sprite, when set, replaces the shader
// frame with that sprite tinted by Card Color. Read-only at runtime. In the Editor,
// changing a value raises Changed so the HUD re-applies it in Play mode without rebuilding.
using System;
using JourneyToLanka.Combat.Cards;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Combat HUD Style", fileName = "CombatHudStyle")]
    public sealed class CombatHudStyleSO : ScriptableObject
    {
        [Header("Canvas")]
        [SerializeField] private Vector2 _referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] private TMP_FontAsset _font;

        [Header("Hand")]
        [SerializeField, Min(0f)] private float _handBottomMargin = 24f;
        [SerializeField, Min(100f)] private float _handMaxWidth = 1300f;
        [SerializeField, Range(0f, 1f)] private float _handInactiveAlpha = 0.6f;

        [Header("Hand Layout")]
        [SerializeField] private HandLayoutSO _handLayout;
        [SerializeField, Min(1f)] private float _handSharpness = 16f;
        [SerializeField] private float _liftedCardRise = 40f;

        [Header("Hand Life")]
        [SerializeField] private Material _cardGlowMaterial;
        [SerializeField] private Color _cardGlowColor = new Color(1f, 0.78f, 0.35f);
        [SerializeField, Min(0f)] private float _cardGlowSpread = 18f;
        [SerializeField, Range(0f, 3f)] private float _cardGlowStrength = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _cardGlowPulseAmount = 0.35f;
        [SerializeField, Min(0f)] private float _cardGlowPulseSpeed = 0.8f;
        [SerializeField, Min(0.01f)] private float _cardGlowFadeDuration = 0.2f;
        [Tooltip("How quickly hand cards spring into place, in oscillations per second.")]
        [SerializeField, Min(0.5f)] private float _handSpringFrequency = 4.5f;
        [Tooltip("Below 1 the hand overshoots slightly and settles; 1 settles without overshoot.")]
        [SerializeField, Range(0.3f, 1f)] private float _handSpringDamping = 0.72f;
        [Tooltip("Delay per card away from the gap or new card before it starts to move.")]
        [SerializeField, Min(0f)] private float _reflowStagger = 0.025f;
        [SerializeField] private float _drawArcHeight = 140f;
        [Tooltip("Point along a draw flight (0 to 1) where the card turns face up.")]
        [SerializeField, Range(0f, 1f)] private float _drawFlipPoint = 0.45f;
        [Tooltip("Share of the flight the flip takes on each side of the flip point.")]
        [SerializeField, Range(0.01f, 0.5f)] private float _drawFlipWidth = 0.16f;
        [SerializeField, Range(0f, 0.5f)] private float _drawLandSwell = 0.1f;
        [SerializeField] private float _sweepArcHeight = 90f;
        [SerializeField] private float _sweepSpin = 160f;

        [Header("Drag to Play")]
        [Tooltip("Height above the hand area's bottom, in canvas units, that a dragged card must pass to play on release.")]
        [SerializeField, Min(0f)] private float _playLineHeight = 360f;
        [SerializeField, Min(0.1f)] private float _dragScale = 1f;
        [SerializeField, Min(0.1f)] private float _dragArmedScale = 1.1f;
        [SerializeField, Min(1f)] private float _dragSharpness = 28f;
        [Tooltip("Degrees of tilt per canvas unit per second of sideways drag speed.")]
        [SerializeField, Min(0f)] private float _dragTiltPerSpeed = 0.01f;
        [SerializeField, Range(0f, 45f)] private float _dragMaxTilt = 12f;

        [Header("Card")]
        [SerializeField] private Vector2 _cardSize = new Vector2(200f, 280f);
        [SerializeField] private Sprite _cardSprite;
        [SerializeField] private Color _cardColor = new Color(0.22f, 0.17f, 0.12f);
        [SerializeField, Min(0f)] private float _cardPadding = 12f;
        [SerializeField] private Color _cardTextColor = new Color(0.96f, 0.91f, 0.8f);
        [SerializeField, Min(1f)] private float _cardNameFontSize = 24f;
        [SerializeField, Min(1f)] private float _cardTypeFontSize = 18f;
        [SerializeField, Min(1f)] private float _cardDescriptionFontSize = 22f;
        [SerializeField] private Color _cardValueBuffedColor = new Color(0.5f, 0.95f, 0.45f);
        [SerializeField] private Color _cardValueWeakenedColor = new Color(1f, 0.45f, 0.38f);
        [SerializeField, Min(1f)] private float _costBadgeSize = 48f;
        [SerializeField] private Sprite _costBadgeSprite;
        [SerializeField] private Color _costBadgeColor = new Color(0.95f, 0.7f, 0.25f);
        [SerializeField] private Color _costTextColor = new Color(0.15f, 0.1f, 0.05f);
        [SerializeField, Min(1f)] private float _costFontSize = 30f;
        [SerializeField] private Color _costUnaffordableBadgeColor = new Color(0.72f, 0.18f, 0.14f);
        [SerializeField] private Color _costUnaffordableTextColor = new Color(1f, 0.92f, 0.86f);
        [SerializeField, Range(0f, 1f)] private float _unplayableAlpha = 0.45f;
        [SerializeField, Min(1f)] private float _targetingCardScale = 1.12f;

        [Header("Card Frame (sizes are fractions of the card width)")]
        [SerializeField] private Material _cardFrameMaterial;
        [SerializeField] private CardTypeVisualsSO[] _cardTypeVisuals = Array.Empty<CardTypeVisualsSO>();
        [SerializeField] private CardTypeVisualsSO _defaultCardTypeVisuals;
        [SerializeField] private Color _frameBodyTopColor = new Color(0.25f, 0.2f, 0.15f);
        [SerializeField] private Color _frameBodyBottomColor = new Color(0.12f, 0.095f, 0.075f);
        [SerializeField, Range(0f, 1f)] private float _frameAccentTint = 0.14f;
        [SerializeField, Range(0f, 0.25f)] private float _frameCornerRadius = 0.07f;
        [SerializeField, Range(0f, 0.1f)] private float _frameBorderWidth = 0.03f;
        [SerializeField] private Color _frameTrimColor = new Color(0.95f, 0.78f, 0.42f, 0.85f);
        [SerializeField, Range(0f, 0.03f)] private float _frameTrimWidth = 0.006f;
        [SerializeField, Range(0f, 0.1f)] private float _frameTrimInset = 0.016f;
        [SerializeField, Range(0f, 1f)] private float _frameBandStrength = 0.9f;
        [SerializeField, Range(0.001f, 0.25f)] private float _frameGlowWidth = 0.06f;
        [SerializeField, Range(0f, 2f)] private float _frameGlowStrength = 0.4f;
        [SerializeField, Range(0f, 1f)] private float _frameSheen = 0.08f;
        [SerializeField] private Sprite _descriptionPanelSprite;
        [SerializeField] private Color _descriptionPanelColor = new Color(0f, 0f, 0f, 0.28f);
        [SerializeField] private float _descriptionPanelPadding = 2f;
        [SerializeField, Min(0f)] private float _typeIconSize = 22f;

        [Header("Card Motion")]
        [SerializeField] private Sprite _cardBackSprite;
        [SerializeField] private Color _cardBackColor = new Color(0.45f, 0.16f, 0.1f);
        [SerializeField, Min(0.01f)] private float _drawDuration = 0.35f;
        [SerializeField, Min(0f)] private float _drawStagger = 0.09f;
        [SerializeField, Range(0.05f, 1f)] private float _drawStartScale = 0.35f;
        [SerializeField] private Vector2 _playZoneOffset = new Vector2(0f, 80f);
        [SerializeField, Min(0.1f)] private float _playScale = 1.15f;
        [SerializeField, Min(0.01f)] private float _playRiseDuration = 0.18f;
        [SerializeField, Min(0f)] private float _playHold = 0.12f;
        [SerializeField, Min(0.01f)] private float _toPileDuration = 0.3f;
        [SerializeField, Range(0.05f, 1f)] private float _pileArrivalScale = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _pileArrivalAlpha = 0.3f;
        [SerializeField, Min(0.01f)] private float _sweepDuration = 0.3f;
        [SerializeField, Min(0f)] private float _sweepStagger = 0.04f;
        [SerializeField, Min(0.01f)] private float _exhaustDuration = 0.3f;
        [SerializeField, Min(0)] private int _reshuffleCardCount = 6;
        [SerializeField, Min(0.01f)] private float _reshuffleDuration = 0.3f;
        [SerializeField, Min(0f)] private float _reshuffleStagger = 0.05f;
        [SerializeField, Min(0f)] private float _pilePunchScale = 0.15f;
        [SerializeField, Min(0.01f)] private float _pilePunchDuration = 0.2f;

        [Header("Rejected Card")]
        [SerializeField, Min(0f)] private float _rejectAngle = 6f;
        [SerializeField, Min(0.01f)] private float _rejectDuration = 0.3f;
        [SerializeField] private Vector2 _toastOffset = new Vector2(0f, 360f);
        [SerializeField] private Vector2 _toastSize = new Vector2(520f, 64f);
        [SerializeField, Min(1f)] private float _toastFontSize = 30f;
        [SerializeField] private Color _toastTextColor = new Color(1f, 0.8f, 0.6f);
        [SerializeField, Min(0.01f)] private float _toastFadeIn = 0.1f;
        [SerializeField, Min(0f)] private float _toastHold = 0.9f;
        [SerializeField, Min(0.01f)] private float _toastFadeOut = 0.25f;

        [Header("Buttons (tint over the base colour)")]
        [SerializeField] private Color _highlightedTint = new Color(1f, 0.95f, 0.8f);
        [SerializeField] private Color _pressedTint = new Color(0.8f, 0.75f, 0.65f);
        [SerializeField] private Color _selectedTint = new Color(1f, 0.9f, 0.6f);
        [SerializeField] private Color _disabledTint = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        [SerializeField, Min(0f)] private float _tintFadeDuration = 0.08f;

        [Header("Panels")]
        [SerializeField] private Sprite _panelSprite;
        [SerializeField] private Color _panelColor = new Color(0.08f, 0.07f, 0.06f, 0.85f);
        [SerializeField] private Color _hudTextColor = new Color(0.96f, 0.91f, 0.8f);
        [SerializeField, Min(1f)] private float _captionFontSize = 22f;
        [SerializeField, Min(1f)] private float _valueFontSize = 40f;
        [SerializeField] private Vector2 _statPanelSize = new Vector2(160f, 100f);
        [SerializeField] private Vector2 _pranaOffset = new Vector2(40f, 150f);
        [SerializeField] private Vector2 _drawPileOffset = new Vector2(40f, 30f);
        [SerializeField] private Vector2 _discardPileOffset = new Vector2(-40f, 30f);

        [Header("Prana")]
        [SerializeField] private PranaOrbsStyleSO _pranaOrbs;

        [Header("Quiver")]
        [SerializeField] private QuiverStyleSO _quiver;

        [Header("End Turn")]
        [SerializeField] private Vector2 _endTurnSize = new Vector2(240f, 80f);
        [SerializeField] private Vector2 _endTurnOffset = new Vector2(-40f, 150f);
        [SerializeField] private Sprite _endTurnSprite;
        [SerializeField] private Color _endTurnColor = new Color(0.55f, 0.2f, 0.12f);
        [SerializeField] private Color _endTurnTextColor = new Color(0.98f, 0.94f, 0.85f);
        [SerializeField, Min(1f)] private float _endTurnFontSize = 30f;
        [SerializeField] private Color _endTurnGlowColor = new Color(0.95f, 0.55f, 0.2f);
        [SerializeField, Min(1f)] private float _endTurnPulseScale = 1.06f;
        [SerializeField, Min(0f)] private float _endTurnPulseDuration = 0.7f;

        [Header("Threat Panel")]
        [SerializeField] private Vector2 _threatPanelSize = new Vector2(260f, 132f);
        [SerializeField] private Vector2 _threatPanelOffset = new Vector2(40f, 270f);
        [SerializeField, Min(0f)] private float _threatPadding = 12f;
        [SerializeField, Min(1f)] private float _threatCaptionFontSize = 22f;
        [SerializeField, Min(1f)] private float _threatValueFontSize = 28f;
        [SerializeField] private Color _threatIncomingColor = new Color(1f, 0.45f, 0.35f);
        [SerializeField] private Color _threatKavachColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Color _threatTakenColor = new Color(0.96f, 0.91f, 0.8f);
        [SerializeField] private Color _threatLethalColor = new Color(1f, 0.2f, 0.15f);
        [SerializeField] private Color _threatPreviewColor = new Color(1f, 0.88f, 0.5f);
        [SerializeField, Min(0f)] private float _threatPunchScale = 0.3f;
        [SerializeField, Min(0.01f)] private float _threatPunchDuration = 0.25f;

        [Header("Turn Banner")]
        [SerializeField] private Vector2 _turnBannerSize = new Vector2(900f, 120f);
        [SerializeField] private Vector2 _turnBannerOffset = new Vector2(0f, 160f);
        [SerializeField] private Sprite _turnBannerSprite;
        [SerializeField] private Color _turnBannerColor = new Color(0.05f, 0.04f, 0.03f, 0.85f);
        [SerializeField, Min(1f)] private float _turnBannerFontSize = 56f;
        [SerializeField] private Color _playerTurnTextColor = new Color(0.98f, 0.85f, 0.45f);
        [SerializeField] private Color _enemyTurnTextColor = new Color(0.9f, 0.35f, 0.28f);
        [SerializeField, Range(0.1f, 1f)] private float _turnBannerStartScale = 0.85f;
        [SerializeField, Min(0.01f)] private float _turnBannerFadeIn = 0.18f;
        [SerializeField, Min(0f)] private float _turnBannerHold = 0.6f;
        [SerializeField, Min(0.01f)] private float _turnBannerFadeOut = 0.3f;

        [Header("Result Banner")]
        [SerializeField] private Vector2 _bannerSize = new Vector2(640f, 150f);
        [SerializeField] private Color _bannerColor = new Color(0.05f, 0.04f, 0.03f, 0.9f);
        [SerializeField, Min(1f)] private float _bannerFontSize = 72f;
        [SerializeField] private Color _victoryTextColor = new Color(0.98f, 0.8f, 0.3f);
        [SerializeField] private Color _defeatTextColor = new Color(0.85f, 0.25f, 0.2f);

        public event Action Changed;

        public Vector2 ReferenceResolution => _referenceResolution;
        public TMP_FontAsset Font => _font;

        public float HandBottomMargin => _handBottomMargin;
        public float HandMaxWidth => _handMaxWidth;
        public HandLayoutSO HandLayout => _handLayout;
        public float HandSharpness => _handSharpness;
        public float LiftedCardRise => _liftedCardRise;

        public Material CardGlowMaterial => _cardGlowMaterial;
        public Color CardGlowColor => _cardGlowColor;
        public float CardGlowSpread => _cardGlowSpread;
        public float CardGlowStrength => _cardGlowStrength;
        public float CardGlowPulseAmount => _cardGlowPulseAmount;
        public float CardGlowPulseSpeed => _cardGlowPulseSpeed;
        public float CardGlowFadeDuration => _cardGlowFadeDuration;
        public float HandSpringFrequency => _handSpringFrequency;
        public float HandSpringDamping => _handSpringDamping;
        public float ReflowStagger => _reflowStagger;
        public float DrawArcHeight => _drawArcHeight;
        public float DrawFlipPoint => _drawFlipPoint;
        public float DrawFlipWidth => _drawFlipWidth;
        public float DrawLandSwell => _drawLandSwell;
        public float SweepArcHeight => _sweepArcHeight;
        public float SweepSpin => _sweepSpin;

        public float PlayLineHeight => _playLineHeight;
        public float DragScale => _dragScale;
        public float DragArmedScale => _dragArmedScale;
        public float DragSharpness => _dragSharpness;
        public float DragTiltPerSpeed => _dragTiltPerSpeed;
        public float DragMaxTilt => _dragMaxTilt;
        public float HandInactiveAlpha => _handInactiveAlpha;

        public Vector2 CardSize => _cardSize;
        public Sprite CardSprite => _cardSprite;
        public Color CardColor => _cardColor;
        public float CardPadding => _cardPadding;
        public Color CardTextColor => _cardTextColor;
        public float CardNameFontSize => _cardNameFontSize;
        public float CardTypeFontSize => _cardTypeFontSize;
        public float CardDescriptionFontSize => _cardDescriptionFontSize;
        public Color CardValueBuffedColor => _cardValueBuffedColor;
        public Color CardValueWeakenedColor => _cardValueWeakenedColor;
        public float CostBadgeSize => _costBadgeSize;
        public Sprite CostBadgeSprite => _costBadgeSprite;
        public Color CostBadgeColor => _costBadgeColor;
        public Color CostTextColor => _costTextColor;
        public float CostFontSize => _costFontSize;
        public Color CostUnaffordableBadgeColor => _costUnaffordableBadgeColor;
        public Color CostUnaffordableTextColor => _costUnaffordableTextColor;
        public float UnplayableAlpha => _unplayableAlpha;
        public float TargetingCardScale => _targetingCardScale;

        public Material CardFrameMaterial => _cardFrameMaterial;
        public CardTypeVisualsSO DefaultCardTypeVisuals => _defaultCardTypeVisuals;
        public int CardTypeVisualsCount => _cardTypeVisuals != null ? _cardTypeVisuals.Length : 0;
        public Color FrameBodyTopColor => _frameBodyTopColor;
        public Color FrameBodyBottomColor => _frameBodyBottomColor;
        public float FrameAccentTint => _frameAccentTint;
        public float FrameCornerRadius => _frameCornerRadius;
        public float FrameBorderWidth => _frameBorderWidth;
        public Color FrameTrimColor => _frameTrimColor;
        public float FrameTrimWidth => _frameTrimWidth;
        public float FrameTrimInset => _frameTrimInset;
        public float FrameBandStrength => _frameBandStrength;
        public float FrameGlowWidth => _frameGlowWidth;
        public float FrameGlowStrength => _frameGlowStrength;
        public float FrameSheen => _frameSheen;
        public Sprite DescriptionPanelSprite => _descriptionPanelSprite;
        public Color DescriptionPanelColor => _descriptionPanelColor;
        public float DescriptionPanelPadding => _descriptionPanelPadding;
        public float TypeIconSize => _typeIconSize;

        public CardTypeVisualsSO GetCardTypeVisuals(int index)
        {
            return _cardTypeVisuals[index];
        }

        public CardTypeVisualsSO FindCardTypeVisuals(CardTypeSO type)
        {
            if (type != null && _cardTypeVisuals != null)
            {
                for (int i = 0; i < _cardTypeVisuals.Length; i++)
                {
                    CardTypeVisualsSO visuals = _cardTypeVisuals[i];
                    if (visuals != null && visuals.Type == type)
                    {
                        return visuals;
                    }
                }
            }

            return _defaultCardTypeVisuals;
        }

        public Sprite CardBackSprite => _cardBackSprite;
        public Color CardBackColor => _cardBackColor;
        public float DrawDuration => _drawDuration;
        public float DrawStagger => _drawStagger;
        public float DrawStartScale => _drawStartScale;
        public Vector2 PlayZoneOffset => _playZoneOffset;
        public float PlayScale => _playScale;
        public float PlayRiseDuration => _playRiseDuration;
        public float PlayHold => _playHold;
        public float ToPileDuration => _toPileDuration;
        public float PileArrivalScale => _pileArrivalScale;
        public float PileArrivalAlpha => _pileArrivalAlpha;
        public float SweepDuration => _sweepDuration;
        public float SweepStagger => _sweepStagger;
        public float ExhaustDuration => _exhaustDuration;
        public int ReshuffleCardCount => _reshuffleCardCount;
        public float ReshuffleDuration => _reshuffleDuration;
        public float ReshuffleStagger => _reshuffleStagger;
        public float PilePunchScale => _pilePunchScale;
        public float PilePunchDuration => _pilePunchDuration;

        public float RejectAngle => _rejectAngle;
        public float RejectDuration => _rejectDuration;
        public Vector2 ToastOffset => _toastOffset;
        public Vector2 ToastSize => _toastSize;
        public float ToastFontSize => _toastFontSize;
        public Color ToastTextColor => _toastTextColor;
        public float ToastFadeIn => _toastFadeIn;
        public float ToastHold => _toastHold;
        public float ToastFadeOut => _toastFadeOut;

        public Color HighlightedTint => _highlightedTint;
        public Color PressedTint => _pressedTint;
        public Color SelectedTint => _selectedTint;
        public Color DisabledTint => _disabledTint;
        public float TintFadeDuration => _tintFadeDuration;

        public Sprite PanelSprite => _panelSprite;
        public Color PanelColor => _panelColor;
        public Color HudTextColor => _hudTextColor;
        public float CaptionFontSize => _captionFontSize;
        public float ValueFontSize => _valueFontSize;
        public Vector2 StatPanelSize => _statPanelSize;
        public Vector2 PranaOffset => _pranaOffset;
        public Vector2 DrawPileOffset => _drawPileOffset;
        public Vector2 DiscardPileOffset => _discardPileOffset;
        public PranaOrbsStyleSO PranaOrbs => _pranaOrbs;
        public QuiverStyleSO Quiver => _quiver;

        public Vector2 EndTurnSize => _endTurnSize;
        public Vector2 EndTurnOffset => _endTurnOffset;
        public Sprite EndTurnSprite => _endTurnSprite;
        public Color EndTurnColor => _endTurnColor;
        public Color EndTurnTextColor => _endTurnTextColor;
        public float EndTurnFontSize => _endTurnFontSize;
        public Color EndTurnGlowColor => _endTurnGlowColor;
        public float EndTurnPulseScale => _endTurnPulseScale;
        public float EndTurnPulseDuration => _endTurnPulseDuration;

        public Vector2 ThreatPanelSize => _threatPanelSize;
        public Vector2 ThreatPanelOffset => _threatPanelOffset;
        public float ThreatPadding => _threatPadding;
        public float ThreatCaptionFontSize => _threatCaptionFontSize;
        public float ThreatValueFontSize => _threatValueFontSize;
        public Color ThreatIncomingColor => _threatIncomingColor;
        public Color ThreatKavachColor => _threatKavachColor;
        public Color ThreatTakenColor => _threatTakenColor;
        public Color ThreatLethalColor => _threatLethalColor;
        public Color ThreatPreviewColor => _threatPreviewColor;
        public float ThreatPunchScale => _threatPunchScale;
        public float ThreatPunchDuration => _threatPunchDuration;

        public Vector2 TurnBannerSize => _turnBannerSize;
        public Vector2 TurnBannerOffset => _turnBannerOffset;
        public Sprite TurnBannerSprite => _turnBannerSprite;
        public Color TurnBannerColor => _turnBannerColor;
        public float TurnBannerFontSize => _turnBannerFontSize;
        public Color PlayerTurnTextColor => _playerTurnTextColor;
        public Color EnemyTurnTextColor => _enemyTurnTextColor;
        public float TurnBannerStartScale => _turnBannerStartScale;
        public float TurnBannerFadeIn => _turnBannerFadeIn;
        public float TurnBannerHold => _turnBannerHold;
        public float TurnBannerFadeOut => _turnBannerFadeOut;

        public Vector2 BannerSize => _bannerSize;
        public Color BannerColor => _bannerColor;
        public float BannerFontSize => _bannerFontSize;
        public Color VictoryTextColor => _victoryTextColor;
        public Color DefeatTextColor => _defeatTextColor;

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
