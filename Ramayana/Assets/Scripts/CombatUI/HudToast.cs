// A short localized message above the hand, such as "Not enough Prana" when a card cannot be played. It fades in,
// holds, then fades out, and never blocks input; showing it again restarts it. It sits on the HUD's motion canvas,
// which already animates every frame, so its fade never rebuilds the static HUD panels. One DOTween core Sequence on
// unscaled time; Hide and Dispose kill it. Every size, colour and timing comes from CombatHudStyleSO.
using System;
using DG.Tweening;
using DG.Tweening.Core;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class HudToast : IDisposable
    {
        private readonly RectTransform _rect;
        private readonly Image _background;
        private readonly TextMeshProUGUI _label;
        private readonly LocalizedText _text;
        private readonly CanvasGroup _group;
        private readonly DOGetter<float> _getAlpha;
        private readonly DOSetter<float> _setAlpha;
        private readonly TweenCallback _finish;
        private CombatHudStyleSO _style;
        private Sequence _sequence;

        private HudToast(RectTransform rect, Image background, TextMeshProUGUI label, CanvasGroup group)
        {
            _rect = rect;
            _background = background;
            _label = label;
            _group = group;
            _text = new LocalizedText(label);
            _getAlpha = () => _group.alpha;
            _setAlpha = value => _group.alpha = value;
            _finish = HandleFinished;
        }

        public static HudToast Create(Transform parent)
        {
            Image background = UIBuildUtility.CreateImage("Toast", parent, false);
            CanvasGroup group = background.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;

            TextMeshProUGUI label = UIBuildUtility.CreateText("Label", background.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(label.rectTransform);

            background.gameObject.SetActive(false);
            return new HudToast(background.rectTransform, background, label, group);
        }

        public void ApplyStyle(CombatHudStyleSO style)
        {
            _style = style;
            if (_style == null)
            {
                return;
            }

            UIBuildUtility.Place(_rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), style.ToastOffset, style.ToastSize);
            UIBuildUtility.StyleImage(_background, style.PanelSprite, style.PanelColor);
            UIBuildUtility.StyleText(_label, style.Font, style.ToastFontSize, style.ToastTextColor);
        }

        public void Show(string key)
        {
            if (_style == null)
            {
                return;
            }

            StopSequence();
            _text.Set(LocalizationTableNames.UI, key);
            _group.alpha = 0f;
            _rect.gameObject.SetActive(true);
            _rect.SetAsLastSibling();

            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(_getAlpha, _setAlpha, 1f, _style.ToastFadeIn));
            sequence.AppendInterval(_style.ToastHold);
            sequence.Append(DOTween.To(_getAlpha, _setAlpha, 0f, _style.ToastFadeOut));
            sequence.SetUpdate(true);
            sequence.OnComplete(_finish);
            _sequence = sequence;
        }

        public void Hide()
        {
            StopSequence();
            _group.alpha = 0f;
            _rect.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            StopSequence();
            _text.Clear();
        }

        private void HandleFinished()
        {
            _sequence = null;
            _rect.gameObject.SetActive(false);
        }

        private void StopSequence()
        {
            if (_sequence == null)
            {
                return;
            }

            Sequence sequence = _sequence;
            _sequence = null;
            sequence.Kill();
        }
    }
}
