// The "Your Turn" / "Enemy Turn" banner: a band with a localized label that fades and scales in, holds, then fades
// out, without blocking input. It lives on its own child canvas so its per-frame fade never rebuilds the HUD panels.
// The animation is one DOTween core Sequence on unscaled time; playing again restarts it, and Hide or Dispose kills it.
// Every size, colour and timing comes from CombatHudStyleSO.
using System;
using DG.Tweening;
using DG.Tweening.Core;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class TurnBanner : IDisposable
    {
        private readonly RectTransform _rect;
        private readonly Image _background;
        private readonly TextMeshProUGUI _label;
        private readonly LocalizedText _text;
        private readonly CanvasGroup _group;
        private readonly DOGetter<float> _getAlpha;
        private readonly DOSetter<float> _setAlpha;
        private readonly TweenCallback _deactivate;
        private readonly TweenCallback _clearSequence;
        private CombatHudStyleSO _style;
        private Sequence _sequence;

        private TurnBanner(RectTransform rect, Image background, TextMeshProUGUI label, CanvasGroup group)
        {
            _rect = rect;
            _background = background;
            _label = label;
            _group = group;
            _text = new LocalizedText(label);
            _getAlpha = () => _group.alpha;
            _setAlpha = value => _group.alpha = value;
            _deactivate = () => _rect.gameObject.SetActive(false);
            _clearSequence = () => _sequence = null;
        }

        public static TurnBanner Create(Transform parent)
        {
            Image background = UIBuildUtility.CreateImage("TurnBanner", parent, false);
            CanvasGroup group = background.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;

            TextMeshProUGUI label = UIBuildUtility.CreateText("Label", background.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(label.rectTransform);

            background.gameObject.SetActive(false);
            return new TurnBanner(background.rectTransform, background, label, group);
        }

        public void ApplyStyle(CombatHudStyleSO style)
        {
            _style = style;
            if (_style == null)
            {
                return;
            }

            UIBuildUtility.Place(_rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), style.TurnBannerOffset, style.TurnBannerSize);
            UIBuildUtility.StyleImage(_background, style.TurnBannerSprite, style.TurnBannerColor);
            UIBuildUtility.StyleText(_label, style.Font, style.TurnBannerFontSize, _label.color);
        }

        public void Play(string key, Color textColor)
        {
            if (_style == null)
            {
                return;
            }

            _sequence?.Kill();
            _text.Set(LocalizationTableNames.UI, key);
            _label.color = textColor;
            _group.alpha = 0f;
            _rect.localScale = Vector3.one * _style.TurnBannerStartScale;
            _rect.gameObject.SetActive(true);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(_getAlpha, _setAlpha, 1f, _style.TurnBannerFadeIn));
            sequence.Join(_rect.DOScale(1f, _style.TurnBannerFadeIn).SetEase(Ease.OutBack));
            sequence.AppendInterval(_style.TurnBannerHold);
            sequence.Append(DOTween.To(_getAlpha, _setAlpha, 0f, _style.TurnBannerFadeOut));
            sequence.SetUpdate(true);
            sequence.OnComplete(_deactivate);
            sequence.OnKill(_clearSequence);
            _sequence = sequence;
        }

        public void Hide()
        {
            _sequence?.Kill();
            _group.alpha = 0f;
            _rect.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            _sequence?.Kill();
            _text.Clear();
        }
    }
}
