// Visual values for Rama's quiver on the combat HUD: the screen corner the rail is pinned to (top-left by default) and
// its offset from it, the rail's height, end padding, colours and gold trim, the socket size and spacing, the socket
// frame and its resting rim, the arrow disc (an arrow without an icon is drawn as the fallback disc in its colour), the
// empty-socket tint, the highlight ring and punch on the considered socket, how faded the quiver is outside the
// player's turn, the 1 to 3 key hints centred under each socket, the small localized caption above the rail, and the
// tooltip that names and describes the considered arrow (below the rail when it is pinned to the top half of the
// screen, above it otherwise). Fonts, the tooltip panel and the rounded rail material come from the Combat HUD style.
// Read-only at runtime; in the Editor, Changed lets the HUD re-apply it live.
using System;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Quiver Style", fileName = "QuiverStyle")]
    public sealed class QuiverStyleSO : ScriptableObject
    {
        [Header("Placement")]
        [Tooltip("Screen corner the rail is pinned to: (0, 1) is top-left, (0, 0) bottom-left. It is also the rail's pivot.")]
        [SerializeField] private Vector2 _anchor = new Vector2(0f, 1f);
        [Tooltip("Offset of the rail's pinned corner from the screen corner, in canvas units.")]
        [SerializeField] private Vector2 _offset = new Vector2(40f, -40f);

        [Header("Rail")]
        [Tooltip("Rail length beyond the first and last socket.")]
        [SerializeField, Min(0f)] private float _padding = 10f;
        [Tooltip("Rail thickness; sockets taller than this stand proud of it.")]
        [SerializeField, Min(4f)] private float _railHeight = 32f;
        [SerializeField] private Color _railColor = new Color(0.1f, 0.075f, 0.055f, 0.92f);
        [SerializeField] private Color _railTrimColor = new Color(0.95f, 0.78f, 0.42f, 0.85f);
        [SerializeField, Min(0f)] private float _railTrimWidth = 3f;

        [Header("Slots")]
        [SerializeField, Min(16f)] private float _slotSize = 64f;
        [SerializeField, Min(0f)] private float _slotSpacing = 12f;
        [SerializeField] private Sprite _slotSprite;
        [SerializeField] private Color _slotColor = new Color(0.16f, 0.13f, 0.1f, 0.95f);
        [SerializeField] private Color _emptySlotColor = new Color(0.16f, 0.13f, 0.1f, 0.45f);
        [Tooltip("Socket rim when the socket is not considered; the highlight colour replaces it when it is.")]
        [SerializeField] private Color _socketRimColor = new Color(0.95f, 0.78f, 0.42f, 0.55f);
        [SerializeField, Range(0.1f, 1f)] private float _arrowIconFraction = 0.72f;
        [SerializeField] private Sprite _fallbackArrowSprite;
        [SerializeField, Range(0f, 1f)] private float _inactiveAlpha = 0.55f;

        [Header("Caption")]
        [SerializeField] private bool _showCaption = true;
        [SerializeField, Min(1f)] private float _captionFontSize = 18f;
        [SerializeField] private Color _captionColor = new Color(0.95f, 0.82f, 0.55f, 0.9f);
        [Tooltip("Space between the caption and the top of the sockets.")]
        [SerializeField, Min(0f)] private float _captionGap = 4f;

        [Header("Considered Slot")]
        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.45f, 0.9f);
        [SerializeField, Min(0f)] private float _highlightThickness = 5f;
        [SerializeField, Min(0f)] private float _punchScale = 0.12f;
        [SerializeField, Min(0.01f)] private float _punchDuration = 0.18f;

        [Header("Key Hints")]
        [SerializeField] private bool _showKeyHints = true;
        [SerializeField, Min(1f)] private float _keyHintFontSize = 16f;
        [SerializeField] private Color _keyHintColor = new Color(0.96f, 0.91f, 0.8f, 0.8f);
        [Tooltip("Space between a socket's bottom and its key hint.")]
        [SerializeField, Min(0f)] private float _keyHintGap = 4f;

        [Header("Tooltip")]
        [SerializeField, Min(50f)] private float _tooltipWidth = 340f;
        [Tooltip("X shifts the tooltip sideways; Y is its gap from the rail (and the key hints, when it sits below).")]
        [SerializeField] private Vector2 _tooltipOffset = new Vector2(0f, 10f);
        [SerializeField, Min(0f)] private float _tooltipPadding = 12f;
        [SerializeField, Min(0f)] private float _tooltipSpacing = 4f;
        [SerializeField, Min(1f)] private float _tooltipNameFontSize = 26f;
        [SerializeField, Min(1f)] private float _tooltipDescriptionFontSize = 20f;

        public event Action Changed;

        public Vector2 Anchor => _anchor;
        public Vector2 Offset => _offset;
        public float Padding => _padding;
        public float RailHeight => _railHeight;
        public Color RailColor => _railColor;
        public Color RailTrimColor => _railTrimColor;
        public float RailTrimWidth => _railTrimWidth;
        public float SlotSize => _slotSize;
        public float SlotSpacing => _slotSpacing;
        public Sprite SlotSprite => _slotSprite;
        public Color SlotColor => _slotColor;
        public Color EmptySlotColor => _emptySlotColor;
        public Color SocketRimColor => _socketRimColor;
        public float ArrowIconFraction => _arrowIconFraction;
        public Sprite FallbackArrowSprite => _fallbackArrowSprite;
        public float InactiveAlpha => _inactiveAlpha;
        public bool ShowCaption => _showCaption;
        public float CaptionFontSize => _captionFontSize;
        public Color CaptionColor => _captionColor;
        public float CaptionGap => _captionGap;
        public Color HighlightColor => _highlightColor;
        public float HighlightThickness => _highlightThickness;
        public float PunchScale => _punchScale;
        public float PunchDuration => _punchDuration;
        public bool ShowKeyHints => _showKeyHints;
        public float KeyHintFontSize => _keyHintFontSize;
        public Color KeyHintColor => _keyHintColor;
        public float KeyHintGap => _keyHintGap;
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
