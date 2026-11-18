// A gentle, looping glow and swell on the End Turn button while the player has nothing left to play, so the next step
// is obvious without being loud. It lerps the button's base colour toward the glow colour and its scale toward the
// pulse scale and back, forever, until switched off, then returns both to rest. The Button's colour tint multiplies on
// top, so hover and press still read. One DOTween core Sequence on unscaled time; values come from CombatHudStyleSO.
using System;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class EndTurnNudge : IDisposable
    {
        private readonly Image _image;
        private readonly RectTransform _rect;
        private readonly DOGetter<float> _getAmount;
        private readonly DOSetter<float> _setAmount;
        private CombatHudStyleSO _style;
        private Sequence _loop;
        private float _amount;
        private bool _active;

        public EndTurnNudge(Image image)
        {
            _image = image;
            _rect = image.rectTransform;
            _getAmount = () => _amount;
            _setAmount = SetAmount;
        }

        public void SetStyle(CombatHudStyleSO style)
        {
            _style = style;
            if (_active)
            {
                Restart();
            }
            else
            {
                SetAmount(0f);
            }
        }

        public void SetActive(bool active)
        {
            if (_active == active)
            {
                return;
            }

            _active = active;
            if (_active)
            {
                Restart();
            }
            else
            {
                StopLoop();
                SetAmount(0f);
            }
        }

        public void Dispose()
        {
            StopLoop();
        }

        private void Restart()
        {
            StopLoop();
            SetAmount(0f);
            if (_style == null || _style.EndTurnPulseDuration <= 0f)
            {
                return;
            }

            Sequence loop = DOTween.Sequence();
            loop.Append(DOTween.To(_getAmount, _setAmount, 1f, _style.EndTurnPulseDuration).SetEase(Ease.InOutSine));
            loop.Append(DOTween.To(_getAmount, _setAmount, 0f, _style.EndTurnPulseDuration).SetEase(Ease.InOutSine));
            loop.SetLoops(-1);
            loop.SetUpdate(true);
            _loop = loop;
        }

        private void StopLoop()
        {
            if (_loop == null)
            {
                return;
            }

            Sequence loop = _loop;
            _loop = null;
            loop.Kill();
        }

        private void SetAmount(float amount)
        {
            _amount = amount;
            if (_style == null || _image == null)
            {
                return;
            }

            _image.color = Color.Lerp(_style.EndTurnColor, _style.EndTurnGlowColor, amount);
            float scale = Mathf.Lerp(1f, _style.EndTurnPulseScale, amount);
            _rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
