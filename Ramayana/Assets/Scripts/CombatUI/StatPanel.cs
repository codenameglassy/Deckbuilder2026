// A small HUD panel with a localized caption over a number, used for Prana and the draw and discard piles.
// Numbers update through TMP's numeric SetText, which does not allocate. Card flights aim at Rect, and Punch gives
// the panel a short DOTween core scale punch when a card lands on it; a new punch completes the previous one first.
// A panel can be made clickable (the draw and discard piles open the pile viewer); its button is mouse-only, with no
// navigation, because keyboard and gamepad open the viewer through their own bindings.
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class StatPanel
    {
        private const float CaptionShare = 0.38f;
        private const int PunchVibrato = 6;
        private const float PunchElasticity = 0.5f;

        private readonly RectTransform _rect;
        private readonly Image _background;
        private readonly TextMeshProUGUI _caption;
        private readonly TextMeshProUGUI _value;
        private readonly LocalizedText _captionText;
        private readonly TweenCallback _clearPunch;
        private Button _button;
        private UnityAction _clicked;
        private Tween _punch;

        private StatPanel(RectTransform rect, Image background, TextMeshProUGUI caption, TextMeshProUGUI value)
        {
            _rect = rect;
            _background = background;
            _caption = caption;
            _value = value;
            _captionText = new LocalizedText(caption);
            _clearPunch = () => _punch = null;
        }

        public RectTransform Rect => _rect;

        public static StatPanel Create(string name, Transform parent, string captionTable, string captionKey)
        {
            Image background = UIBuildUtility.CreateImage(name, parent, false);
            TextMeshProUGUI caption = UIBuildUtility.CreateText("Caption", background.transform, TextAlignmentOptions.Center);
            TextMeshProUGUI value = UIBuildUtility.CreateText("Value", background.transform, TextAlignmentOptions.Center);

            UIBuildUtility.SetOffsets(caption.rectTransform, new Vector2(0f, 1f - CaptionShare), Vector2.one, Vector2.zero, Vector2.zero);
            UIBuildUtility.SetOffsets(value.rectTransform, Vector2.zero, new Vector2(1f, 1f - CaptionShare), Vector2.zero, Vector2.zero);

            var panel = new StatPanel(background.rectTransform, background, caption, value);
            panel._captionText.Set(captionTable, captionKey);
            return panel;
        }

        public void ApplyStyle(CombatHudStyleSO style, Vector2 corner, Vector2 offset)
        {
            UIBuildUtility.Place(_rect, corner, corner, offset, style.StatPanelSize);
            UIBuildUtility.StyleImage(_background, style.PanelSprite, style.PanelColor);
            UIBuildUtility.StyleText(_caption, style.Font, style.CaptionFontSize, style.HudTextColor);
            UIBuildUtility.StyleText(_value, style.Font, style.ValueFontSize, style.HudTextColor);
            if (_button != null)
            {
                _button.colors = CardView.CreateTint(style);
            }
        }

        public void EnableButton(UnityAction clicked)
        {
            if (_button != null || clicked == null)
            {
                return;
            }

            _background.raycastTarget = true;
            _button = _background.gameObject.AddComponent<Button>();
            _button.targetGraphic = _background;
            _button.transition = Selectable.Transition.ColorTint;
            _button.navigation = new Navigation { mode = Navigation.Mode.None };
            _clicked = clicked;
            _button.onClick.AddListener(_clicked);
        }

        public void SetActive(bool active)
        {
            _rect.gameObject.SetActive(active);
        }

        public void SetValue(int value)
        {
            _value.SetText("{0}", (float)value);
        }

        public void SetValue(int value, int max)
        {
            _value.SetText("{0}/{1}", (float)value, (float)max);
        }

        public void Punch(float scale, float duration)
        {
            StopPunch();
            if (scale <= 0f || duration <= 0f || !_rect.gameObject.activeInHierarchy)
            {
                return;
            }

            _rect.localScale = Vector3.one;
            _punch = _rect.DOPunchScale(Vector3.one * scale, duration, PunchVibrato, PunchElasticity)
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
