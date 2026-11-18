// Visual values for Rama's quiver on the combat HUD: where the panel sits (from the bottom-left corner, above the
// threat panel by default), the slot size, spacing and padding, the slot frame and the arrow disc (an arrow without
// an icon is drawn as the fallback disc in its colour), the empty-slot tint, the highlight ring and punch on the
// considered slot, how faded the quiver is outside the player's turn, the 1 to 3 key hints, and the tooltip that
// names and describes the considered arrow above the panel. Panel background, caption font and text colours come
// from the Combat HUD style so the quiver matches the other panels. Read-only at runtime; in the Editor, Changed
// lets the HUD re-apply it live.
using System;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Quiver Style", fileName = "QuiverStyle")]
    public sealed class QuiverStyleSO : ScriptableObject
    {
        [Header("Panel")]
        [SerializeField] private Vector2 _offset = new Vector2(40f, 418f);
        [SerializeField, Min(0f)] private float _padding = 10f;
        [SerializeField, Min(0f)] private float _captionGap = 4f;

        [Header("Slots")]
        [SerializeField, Min(16f)] private float _slotSize = 72f;
        [SerializeField, Min(0f)] private float _slotSpacing = 12f;
        [SerializeField] private Sprite _slotSprite;
        [SerializeField] private Color _slotColor = new Color(0.16f, 0.13f, 0.1f, 0.95f);
        [SerializeField] private Color _emptySlotColor = new Color(0.16f, 0.13f, 0.1f, 0.45f);
        [SerializeField, Range(0.1f, 1f)] private float _arrowIconFraction = 0.72f;
        [SerializeField] private Sprite _fallbackArrowSprite;
        [SerializeField, Range(0f, 1f)] private float _inactiveAlpha = 0.55f;

        [Header("Considered Slot")]
        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.45f, 0.9f);
        [SerializeField, Min(0f)] private float _highlightThickness = 5f;
        [SerializeField, Min(0f)] private float _punchScale = 0.12f;
        [SerializeField, Min(0.01f)] private float _punchDuration = 0.18f;

        [Header("Key Hints")]
        [SerializeField] private bool _showKeyHints = true;
        [SerializeField, Min(1f)] private float _keyHintFontSize = 16f;
        [SerializeField] private Color _keyHintColor = new Color(0.96f, 0.91f, 0.8f, 0.8f);
        [SerializeField] private Vector2 _keyHintOffset = new Vector2(4f, -2f);

        [Header("Tooltip")]
        [SerializeField, Min(50f)] private float _tooltipWidth = 340f;
        [SerializeField] private Vector2 _tooltipOffset = new Vector2(0f, 10f);
        [SerializeField, Min(0f)] private float _tooltipPadding = 12f;
        [SerializeField, Min(0f)] private float _tooltipSpacing = 4f;
        [SerializeField, Min(1f)] private float _tooltipNameFontSize = 26f;
        [SerializeField, Min(1f)] private float _tooltipDescriptionFontSize = 20f;

        public event Action Changed;

        public Vector2 Offset => _offset;
        public float Padding => _padding;
        public float CaptionGap => _captionGap;
        public float SlotSize => _slotSize;
        public float SlotSpacing => _slotSpacing;
        public Sprite SlotSprite => _slotSprite;
        public Color SlotColor => _slotColor;
        public Color EmptySlotColor => _emptySlotColor;
        public float ArrowIconFraction => _arrowIconFraction;
        public Sprite FallbackArrowSprite => _fallbackArrowSprite;
        public float InactiveAlpha => _inactiveAlpha;
        public Color HighlightColor => _highlightColor;
        public float HighlightThickness => _highlightThickness;
        public float PunchScale => _punchScale;
        public float PunchDuration => _punchDuration;
        public bool ShowKeyHints => _showKeyHints;
        public float KeyHintFontSize => _keyHintFontSize;
        public Color KeyHintColor => _keyHintColor;
        public Vector2 KeyHintOffset => _keyHintOffset;
        public float TooltipWidth => _tooltipWidth;
        public Vector2 TooltipOffset => _tooltipOffset;
        public float TooltipPadding => _tooltipPadding;
        public float TooltipSpacing => _tooltipSpacing;
        public float TooltipNameFontSize => _tooltipNameFontSize;
        public float TooltipDescriptionFontSize => _tooltipDescriptionFontSize;

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
