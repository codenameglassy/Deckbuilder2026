// One card in motion on the HUD's motion canvas: a card face (a CardView copy) or a plain card back, flown by a
// single DOTween core tween from where it is now to a fixed point or to a live RectTransform (a hand slot that is
// still settling), while it scales, fades and turns: to the live slot's angle (a fanned hand), or upright for a
// point. Before a leg, SetPath bends it into an upward arc and adds a spin, SetFlip turns a face card over partway
// (it starts face down under its cover and flips edge-on to face up), and SetSwell gives it a slight swell just
// before it lands; each applies to the next leg only. CardMotionPresenter owns the pools and chains legs by passing
// the next step as the arrival callback. The tween reference is managed by hand, never cleared from an OnKill
// callback, because legs are chained from inside DOTween's own update. Plain C#; it never touches combat state.
using System;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class CardFlight
    {
        private readonly DOGetter<float> _getProgress;
        private readonly DOSetter<float> _setProgress;
        private readonly TweenCallback _complete;
        private Tween _tween;
        private Action<CardFlight> _arrived;
        private RectTransform _toRect;
        private Vector3 _toPoint;
        private Vector3 _from;
        private float _fromScale;
        private float _toScale;
        private float _fromAlpha;
        private float _toAlpha;
        private float _fromAngle;
        private float _progress;
        private float _nextArc;
        private float _nextSpin;
        private float _nextFlipPoint = -1f;
        private float _nextFlipWidth;
        private float _nextSwell;
        private float _legArc;
        private float _legSpin;
        private float _legFlipPoint = -1f;
        private float _legFlipWidth;
        private float _legSwell;

        public CardFlight(RectTransform rect, CanvasGroup group, CardView face, Image back, Image cover)
        {
            Rect = rect;
            Group = group;
            Face = face;
            Back = back;
            Cover = cover;
            IsFace = !ReferenceEquals(face, null);
            _getProgress = () => _progress;
            _setProgress = SetProgress;
            _complete = HandleComplete;
        }

        public RectTransform Rect { get; }

        public CanvasGroup Group { get; }

        public CardView Face { get; }

        public Image Back { get; }

        public Image Cover { get; }

        public bool IsFace { get; }

        public int CardId { get; set; } = -1;

        public CardView HandView { get; set; }

        public bool ExhaustOnExit { get; set; }

        public static Vector3 WorldCenter(RectTransform rect)
        {
            return rect.TransformPoint(rect.rect.center);
        }

        public void Place(Vector3 position, float scale, float alpha, float angle = 0f)
        {
            Stop();
            Rect.position = position;
            Rect.localScale = Vector3.one * scale;
            Rect.localEulerAngles = new Vector3(0f, 0f, angle);
            Group.alpha = alpha;
        }

        public void SetPath(float arcHeight, float spin)
        {
            _nextArc = arcHeight;
            _nextSpin = spin;
        }

        public void SetFlip(float point, float width)
        {
            _nextFlipPoint = Cover != null ? Mathf.Clamp01(point) : -1f;
            _nextFlipWidth = Mathf.Max(0.01f, width);
        }

        public void SetSwell(float swell)
        {
            _nextSwell = swell;
        }

        public void FlyTo(Vector3 point, float scale, float alpha, float duration, float delay, Ease ease, Action<CardFlight> arrived)
        {
            _toRect = null;
            _toPoint = point;
            Begin(scale, alpha, duration, delay, ease, arrived);
        }

        public void FlyTo(RectTransform target, float scale, float alpha, float duration, float delay, Ease ease, Action<CardFlight> arrived)
        {
            _toRect = target;
            Begin(scale, alpha, duration, delay, ease, arrived);
        }

        public void Stop()
        {
            _arrived = null;
            if (_tween == null)
            {
                return;
            }

            Tween tween = _tween;
            _tween = null;
            tween.Kill();
        }

        public void Reset()
        {
            Stop();
            _toRect = null;
            Rect.localRotation = Quaternion.identity;
            ClearNextLeg();
            _legArc = 0f;
            _legSpin = 0f;
            _legFlipPoint = -1f;
            _legSwell = 0f;
            SetCovered(false);
            CardId = -1;
            HandView = null;
            ExhaustOnExit = false;
        }

        private void Begin(float scale, float alpha, float duration, float delay, Ease ease, Action<CardFlight> arrived)
        {
            Stop();
            _from = Rect.position;
            _fromScale = Rect.localScale.x;
            _fromAlpha = Group.alpha;
            _fromAngle = Rect.localEulerAngles.z;
            _toScale = scale;
            _toAlpha = alpha;
            _progress = 0f;
            _arrived = arrived;

            Transform parent = Rect.parent;
            _legArc = _nextArc * (parent != null ? parent.lossyScale.y : 1f);
            _legSpin = _nextSpin;
            _legFlipPoint = _nextFlipPoint;
            _legFlipWidth = _nextFlipWidth;
            _legSwell = _nextSwell;
            ClearNextLeg();
            SetCovered(_legFlipPoint >= 0f);

            _tween = DOTween.To(_getProgress, _setProgress, 1f, Mathf.Max(0.01f, duration))
                .SetDelay(delay)
                .SetEase(ease)
                .SetUpdate(true)
                .OnComplete(_complete);
        }

        private void SetProgress(float progress)
        {
            _progress = progress;
            Vector3 to = _toRect != null ? WorldCenter(_toRect) : _toPoint;
            Vector3 position = Vector3.LerpUnclamped(_from, to, progress);
            if (_legArc != 0f)
            {
                position.y += _legArc * 4f * progress * (1f - progress);
            }

            Rect.position = position;

            float scale = Mathf.LerpUnclamped(_fromScale, _toScale, progress);
            if (_legSwell != 0f)
            {
                scale *= 1f + _legSwell * Mathf.Sin(Mathf.PI * Mathf.Clamp01(progress)) * progress;
            }

            float width = 1f;
            if (_legFlipPoint >= 0f)
            {
                width = Mathf.Clamp01(Mathf.Abs(progress - _legFlipPoint) / _legFlipWidth);
                SetCovered(progress < _legFlipPoint);
            }

            Rect.localScale = new Vector3(scale * width, scale, 1f);
            Group.alpha = Mathf.Lerp(_fromAlpha, _toAlpha, progress);
            float toAngle = _toRect != null ? _toRect.eulerAngles.z : 0f;
            float angle = Mathf.LerpAngle(_fromAngle, toAngle, progress) + _legSpin * progress;
            Rect.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private void ClearNextLeg()
        {
            _nextArc = 0f;
            _nextSpin = 0f;
            _nextFlipPoint = -1f;
            _nextFlipWidth = 0f;
            _nextSwell = 0f;
        }

        private void SetCovered(bool covered)
        {
            if (Cover != null && Cover.enabled != covered)
            {
                Cover.enabled = covered;
            }
        }

        private void HandleComplete()
        {
            _tween = null;
            SetCovered(false);
            Action<CardFlight> arrived = _arrived;
            _arrived = null;
            arrived?.Invoke(this);
        }
    }
}
