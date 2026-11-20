// The draw or discard pile on the combat HUD: a small stack of card backs (the Card Back shader, or the style's back
// sprite) that thickens as the pile grows (PileStackMath), each lower card offset, tilted alternately left and right
// and a little darker, with the count in a gold-ringed medallion on the top card and a short localized caption below
// it. An empty pile leaves a faint outline of a card. The whole stack can be tinted (the discard stack is dimmed so
// it reads apart from the draw stack). Card flights aim at Rect (the top card), and Punch gives the stack a short
// DOTween core scale punch when a card lands; a new punch completes the previous one first. The stack can be made
// clickable (the piles open the pile viewer); its button is mouse-only, with no navigation, because keyboard and
// gamepad open the viewer through their own bindings, and it tints the top card on hover and press. Its footprint
// (stack plus caption) is placed by a screen corner and offset; Height reports it so the HUD can stack the left column.
// The count uses TMP's numeric SetText, which does not allocate. Every value comes from the Combat HUD style. It only
// presents.
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class PileStackView
    {
        private const int LayerCapacity = 6;
        private const int PunchVibrato = 6;
        private const float PunchElasticity = 0.5f;
        private const float CaptionLineHeight = 1.3f;

        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        private readonly RectTransform _rect;
        private readonly RectTransform _stack;
        private readonly Image[] _layers;
        private readonly Image _badgeRing;
        private readonly Image _badgeFace;
        private readonly TextMeshProUGUI _count;
        private readonly TextMeshProUGUI _caption;
        private readonly LocalizedText _captionText;
        private readonly TweenCallback _clearPunch;
        private CombatHudStyleSO _style;
        private Color _tint = Color.white;
        private Button _button;
        private UnityAction _clicked;
        private Tween _punch;
        private int _cards;

        private PileStackView(RectTransform rect, RectTransform stack, Image[] layers, Image badgeRing, Image badgeFace,
            TextMeshProUGUI count, TextMeshProUGUI caption)
        {
            _rect = rect;
            _stack = stack;
            _layers = layers;
            _badgeRing = badgeRing;
            _badgeFace = badgeFace;
            _count = count;
            _caption = caption;
            _captionText = new LocalizedText(caption);
            _clearPunch = () => _punch = null;
            _count.SetText("{0}", 0f);
        }

        public RectTransform Rect => _stack;

        public float Height => _rect.sizeDelta.y;

        public static PileStackView Create(string name, Transform parent, string captionTable, string captionKey)
        {
            RectTransform root = UIBuildUtility.CreateRect(name, parent);
            RectTransform stack = UIBuildUtility.CreateRect("Stack", root);

            var layers = new Image[LayerCapacity];
            for (int depth = LayerCapacity - 1; depth >= 0; depth--)
            {
                layers[depth] = UIBuildUtility.CreateImage("Card" + depth, stack, false);
            }

            Image ring = UIBuildUtility.CreateImage("BadgeRing", stack, false);
            ring.gameObject.AddComponent<CardArtMeshEffect>();
            Image face = UIBuildUtility.CreateImage("BadgeFace", stack, false);
            face.gameObject.AddComponent<CardArtMeshEffect>();
            TextMeshProUGUI count = UIBuildUtility.CreateText("Count", face.transform, TextAlignmentOptions.Center);
            count.textWrappingMode = TextWrappingModes.NoWrap;
            UIBuildUtility.Stretch(count.rectTransform);

            TextMeshProUGUI caption = UIBuildUtility.CreateText("Caption", root, TextAlignmentOptions.Bottom);
            caption.textWrappingMode = TextWrappingModes.NoWrap;

            var view = new PileStackView(root, stack, layers, ring, face, count, caption);
            view._captionText.Set(captionTable, captionKey);
            return view;
        }

        public void ApplyStyle(CombatHudStyleSO style, Vector2 corner, Vector2 offset, Color tint)
        {
            _style = style;
            _tint = tint;
            if (style == null)
            {
                return;
            }

            StopPunch();
            Vector2 card = style.PileCardSize;
            float captionHeight = style.PileCaptionFontSize * CaptionLineHeight;
            float band = captionHeight + style.PileCaptionGap;
            UIBuildUtility.Place(_rect, corner, corner, offset, new Vector2(card.x, card.y + band));
            UIBuildUtility.Place(_stack, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, card);
            UIBuildUtility.Place(_caption.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero,
                new Vector2(card.x, captionHeight));
            UIBuildUtility.StyleText(_caption, style.Font, style.PileCaptionFontSize, style.PileCaptionColor);

            for (int depth = 0; depth < _layers.Length; depth++)
            {
                RectTransform layer = _layers[depth].rectTransform;
                UIBuildUtility.Place(layer, Centre, Centre, style.PileLayerOffset * depth, card);
                layer.localRotation = Quaternion.Euler(0f, 0f, LayerTilt(depth, style.PileLayerRotation));
                CardFrameMaterials.ApplyBack(_layers[depth], style, Color.white);
            }

            float badge = style.PileBadgeDiameter;
            float face = Mathf.Max(0f, badge - style.PileBadgeRingWidth * 2f);
            Material round = CardFrameMaterials.GetRound(style);
            UIBuildUtility.Place(_badgeRing.rectTransform, style.PileBadgeAnchor, Centre, style.PileBadgeOffset,
                new Vector2(badge, badge));
            UIBuildUtility.Place(_badgeFace.rectTransform, style.PileBadgeAnchor, Centre, style.PileBadgeOffset,
                new Vector2(face, face));
            UIBuildUtility.StyleImage(_badgeRing, null, style.PileBadgeRingColor);
            _badgeRing.material = round;
            UIBuildUtility.StyleImage(_badgeFace, null, style.PileBadgeFaceColor);
            _badgeFace.material = round;
            UIBuildUtility.StyleText(_count, style.Font, style.PileBadgeFontSize, style.PileBadgeTextColor);

            if (_button != null)
            {
                _button.colors = CardView.CreateTint(style);
            }

            Refresh();
        }

        public void EnableButton(UnityAction clicked)
        {
            if (_button != null || clicked == null)
            {
                return;
            }

            Image top = _layers[0];
            top.raycastTarget = true;
            _button = top.gameObject.AddComponent<Button>();
            _button.targetGraphic = top;
            _button.transition = Selectable.Transition.ColorTint;
            _button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (_style != null)
            {
                _button.colors = CardView.CreateTint(_style);
            }

            _clicked = clicked;
            _button.onClick.AddListener(_clicked);
        }

        public void SetActive(bool active)
        {
            _rect.gameObject.SetActive(active);
        }

        public void SetValue(int cards)
        {
            if (cards == _cards)
            {
                return;
            }

            _cards = cards;
            _count.SetText("{0}", (float)cards);
            Refresh();
        }

        public void Punch(float scale, float duration)
        {
            StopPunch();
            if (scale <= 0f || duration <= 0f || !_rect.gameObject.activeInHierarchy)
            {
                return;
            }

            _stack.localScale = Vector3.one;
            _punch = _stack.DOPunchScale(Vector3.one * scale, duration, PunchVibrato, PunchElasticity)
                .SetUpdate(true)
                .OnComplete(_clearPunch);
        }

        public void Dispose()
        {
            StopPunch();
            if (_button != null && _clicked != null)
            {
                _button.onClick.RemoveListener(_clicked);
            }

            _captionText.Clear();
        }

        private void Refresh()
        {
            if (_style == null)
            {
                return;
            }

            int shown = PileStackMath.LayerCount(_cards, Mathf.Min(_style.PileMaxLayers, LayerCapacity),
                _style.PileCardsPerLayer);
            for (int depth = 0; depth < _layers.Length; depth++)
            {
                Image layer = _layers[depth];
                bool visible = depth < Mathf.Max(shown, 1);
                layer.enabled = visible;
                if (!visible)
                {
                    continue;
                }

                Color tint;
                if (shown == 0)
                {
                    tint = _style.PileEmptyColor;
                }
                else
                {
                    float shade = Mathf.Max(0f, 1f - _style.PileLayerShade * depth);
                    tint = new Color(_tint.r * shade, _tint.g * shade, _tint.b * shade, _tint.a);
                }

                layer.color = CardFrameMaterials.BackColor(_style, tint);
            }
        }

        private static float LayerTilt(int depth, float step)
        {
            if (depth == 0)
            {
                return 0f;
            }

            float side = depth % 2 == 1 ? 1f : -1f;
            return side * step * ((depth + 1) / 2);
        }

        private void StopPunch()
        {
            if (_punch == null)
            {
                return;
            }

            Tween punch = _punch;
            _punch = null;
            punch.Kill(true);
        }
    }
}
