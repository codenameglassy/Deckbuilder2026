// Every visual value of the combat HUD and its cards: canvas reference resolution, hand layout, card size, colours,
// fonts and text sizes, panel and button placement, and the result banner. Sprites are optional; without one an
// element draws as a flat colour. Read-only at runtime. In the Editor, changing a value raises Changed so the HUD
// re-applies it in Play mode without rebuilding.
using System;
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
        [SerializeField] private float _cardSpacing = 12f;
        [SerializeField, Range(0f, 1f)] private float _handInactiveAlpha = 0.6f;

        [Header("Card")]
        [SerializeField] private Vector2 _cardSize = new Vector2(200f, 280f);
        [SerializeField] private Sprite _cardSprite;
        [SerializeField] private Color _cardColor = new Color(0.22f, 0.17f, 0.12f);
        [SerializeField, Min(0f)] private float _cardPadding = 12f;
        [SerializeField] private Color _cardTextColor = new Color(0.96f, 0.91f, 0.8f);
        [SerializeField, Min(1f)] private float _cardNameFontSize = 24f;
        [SerializeField, Min(1f)] private float _cardTypeFontSize = 18f;
        [SerializeField, Min(1f)] private float _cardDescriptionFontSize = 22f;
        [SerializeField, Min(1f)] private float _costBadgeSize = 48f;
        [SerializeField] private Sprite _costBadgeSprite;
        [SerializeField] private Color _costBadgeColor = new Color(0.95f, 0.7f, 0.25f);
        [SerializeField] private Color _costTextColor = new Color(0.15f, 0.1f, 0.05f);
        [SerializeField, Min(1f)] private float _costFontSize = 30f;
        [SerializeField, Range(0f, 1f)] private float _unplayableAlpha = 0.45f;

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

        [Header("End Turn")]
        [SerializeField] private Vector2 _endTurnSize = new Vector2(240f, 80f);
        [SerializeField] private Vector2 _endTurnOffset = new Vector2(-40f, 150f);
        [SerializeField] private Sprite _endTurnSprite;
        [SerializeField] private Color _endTurnColor = new Color(0.55f, 0.2f, 0.12f);
        [SerializeField] private Color _endTurnTextColor = new Color(0.98f, 0.94f, 0.85f);
        [SerializeField, Min(1f)] private float _endTurnFontSize = 30f;

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
        public float CardSpacing => _cardSpacing;
        public float HandInactiveAlpha => _handInactiveAlpha;

        public Vector2 CardSize => _cardSize;
        public Sprite CardSprite => _cardSprite;
        public Color CardColor => _cardColor;
        public float CardPadding => _cardPadding;
        public Color CardTextColor => _cardTextColor;
        public float CardNameFontSize => _cardNameFontSize;
        public float CardTypeFontSize => _cardTypeFontSize;
        public float CardDescriptionFontSize => _cardDescriptionFontSize;
        public float CostBadgeSize => _costBadgeSize;
        public Sprite CostBadgeSprite => _costBadgeSprite;
        public Color CostBadgeColor => _costBadgeColor;
        public Color CostTextColor => _costTextColor;
        public float CostFontSize => _costFontSize;
        public float UnplayableAlpha => _unplayableAlpha;

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

        public Vector2 EndTurnSize => _endTurnSize;
        public Vector2 EndTurnOffset => _endTurnOffset;
        public Sprite EndTurnSprite => _endTurnSprite;
        public Color EndTurnColor => _endTurnColor;
        public Color EndTurnTextColor => _endTurnTextColor;
        public float EndTurnFontSize => _endTurnFontSize;

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
