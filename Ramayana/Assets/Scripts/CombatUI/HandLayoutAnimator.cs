// Moves the hand's cards to the poses chosen by the Combat HUD style's HandLayoutSO strategy. Targets are recomputed
// only when something changes (cards added or removed, hover, a card lifted for targeting, the style or the layout
// asset); each card's position then follows its target on a slightly underdamped spring (sub-stepped, so it is the
// same at any frame rate) for a soft settle, while its angle and scale ease with frame-rate independent smoothing,
// all on unscaled time, and the animator stops ticking once every card has settled. When a card leaves or joins the
// hand (HoldFrom), the others start moving one after another outward from that slot. A card lifted for targeting straightens, rises and scales on top
// of its layout pose. A dragged card (SetDragged) leaves the layout and follows the drag position with its own, quicker
// sharpness, tilting with its sideways speed, until ClearDragged sends it back to its slot. The strategy decides where
// cards go; this decides how they get there, the same for every layout. Cards are anchored at the bottom centre of the
// hand area, the origin of the layout's space.
using System.Collections.Generic;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    internal sealed class HandLayoutAnimator : ITickable
    {
        private const float PositionEpsilon = 0.25f;
        private const float AngleEpsilon = 0.05f;
        private const float ScaleEpsilon = 0.0005f;
        private const float VelocityEpsilon = 2f;
        private const float MaxSpringStep = 1f / 120f;
        private const int MaxSpringSteps = 8;
        private static readonly Vector2 BottomCentre = new Vector2(0.5f, 0f);
        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        private readonly IReadOnlyList<CardView> _cards;
        private readonly List<HandCardPose> _targets = new List<HandCardPose>(12);
        private readonly Dictionary<CardView, Vector2> _velocities = new Dictionary<CardView, Vector2>(16);
        private readonly Dictionary<CardView, float> _holdUntil = new Dictionary<CardView, float>(16);
        private CombatHudStyleSO _style;
        private HandLayoutSO _layout;
        private ITickService _ticks;
        private CardView _dragged;
        private Vector2 _dragPosition;
        private float _dragScale = 1f;
        private float _dragTilt;
        private int _hoveredIndex = -1;
        private bool _dirty = true;
        private bool _registered;

        public HandLayoutAnimator(IReadOnlyList<CardView> cards)
        {
            _cards = cards;
        }

        public HandLayoutSO Layout => _layout;

        public CardView Dragged => _dragged;

        public void SetStyle(CombatHudStyleSO style)
        {
            _style = style;
            HandLayoutSO layout = style != null ? style.HandLayout : null;
            if (layout != _layout)
            {
                if (_layout != null)
                {
                    _layout.Changed -= MarkDirty;
                }

                _layout = layout;
                if (_layout != null)
                {
                    _layout.Changed += MarkDirty;
                }
            }

            MarkDirty();
        }

        public void SetHovered(int index)
        {
            if (index == _hoveredIndex)
            {
                return;
            }

            _hoveredIndex = index;
            MarkDirty();
        }

        public HandLayoutInput CreateInput(int hoveredIndex)
        {
            return new HandLayoutInput(_cards.Count, _style != null ? _style.HandMaxWidth : 0f,
                _style != null ? _style.CardSize : Vector2.zero, hoveredIndex);
        }

        public void SetDragged(CardView view, Vector2 position, float scale)
        {
            if (view != _dragged)
            {
                _dragged = view;
                _dragTilt = 0f;
            }

            _dragPosition = position;
            _dragScale = scale;
            SetRegistered(true);
        }

        public void ClearDragged()
        {
            if (_dragged == null)
            {
                return;
            }

            _velocities[_dragged] = Vector2.zero;
            _dragged = null;
            _dragTilt = 0f;
            MarkDirty();
        }

        public void HoldFrom(int index, bool removed)
        {
            float stagger = _style != null ? _style.ReflowStagger : 0f;
            if (stagger <= 0f)
            {
                return;
            }

            float now = Time.unscaledTime;
            for (int i = 0; i < _cards.Count; i++)
            {
                int distance;
                if (removed)
                {
                    distance = i < index ? index - 1 - i : i - index;
                }
                else
                {
                    if (i == index)
                    {
                        continue;
                    }

                    distance = Mathf.Abs(i - index) - 1;
                }

                if (distance > 0)
                {
                    _holdUntil[_cards[i]] = now + distance * stagger;
                }
            }

            MarkDirty();
        }

        public void PrepareCard(CardView view)
        {
            _velocities[view] = Vector2.zero;
            _holdUntil.Remove(view);
            var rect = (RectTransform)view.transform;
            rect.anchorMin = BottomCentre;
            rect.anchorMax = BottomCentre;
            rect.pivot = Centre;
        }

        public void Snap(CardView view)
        {
            RecomputeTargets();
            int index = IndexOf(view);
            if (index >= 0)
            {
                Apply((RectTransform)view.transform, Target(index), 1f);
                _velocities[view] = Vector2.zero;
                _holdUntil.Remove(view);
            }

            MarkDirty();
        }

        public void MarkDirty()
        {
            _dirty = true;
            SetRegistered(true);
        }

        public void Dispose()
        {
            if (_layout != null)
            {
                _layout.Changed -= MarkDirty;
                _layout = null;
            }

            _dragged = null;
            _velocities.Clear();
            _holdUntil.Clear();
            SetRegistered(false);
        }

        public void Tick(float deltaTime)
        {
            if (_layout == null || _style == null)
            {
                SetRegistered(false);
                return;
            }

            if (_dirty)
            {
                RecomputeTargets();
            }

            float unscaledDeltaTime = Time.unscaledDeltaTime;
            float blend = 1f - Mathf.Exp(-_style.HandSharpness * unscaledDeltaTime);
            float now = Time.unscaledTime;
            bool settled = true;
            for (int i = 0; i < _cards.Count; i++)
            {
                CardView card = _cards[i];
                if (card == _dragged)
                {
                    settled &= ApplyDragged((RectTransform)card.transform, unscaledDeltaTime);
                    continue;
                }

                if (_holdUntil.TryGetValue(card, out float holdUntil))
                {
                    if (now < holdUntil)
                    {
                        settled = false;
                        continue;
                    }

                    _holdUntil.Remove(card);
                }

                settled &= ApplySpring(card, Target(i), blend, unscaledDeltaTime);
            }

            if (settled)
            {
                SetRegistered(false);
            }
        }

        private void RecomputeTargets()
        {
            _dirty = false;
            _targets.Clear();
            if (_layout == null || _style == null)
            {
                return;
            }

            HandLayoutInput input = CreateInput(_hoveredIndex);
            for (int i = 0; i < _cards.Count; i++)
            {
                _targets.Add(_layout.GetPose(input, i));
            }
        }

        private HandCardPose Target(int index)
        {
            HandCardPose pose = index < _targets.Count ? _targets[index] : new HandCardPose(Vector2.zero, 0f, 1f);
            CardView view = _cards[index];
            if (!view.IsLifted || _style == null)
            {
                return pose;
            }

            return new HandCardPose(pose.Position + new Vector2(0f, _style.LiftedCardRise), 0f, pose.Scale * view.LiftScale);
        }

        private bool ApplySpring(CardView card, in HandCardPose target, float blend, float deltaTime)
        {
            var rect = (RectTransform)card.transform;
            Vector2 position = rect.anchoredPosition;
            _velocities.TryGetValue(card, out Vector2 velocity);

            float omega = 2f * Mathf.PI * _style.HandSpringFrequency;
            float damping = 2f * _style.HandSpringDamping * omega;
            int steps = Mathf.Clamp(Mathf.CeilToInt(deltaTime / MaxSpringStep), 1, MaxSpringSteps);
            float step = deltaTime / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector2 acceleration = -omega * omega * (position - target.Position) - damping * velocity;
                velocity += acceleration * step;
                position += velocity * step;
            }

            bool positionSettled = (position - target.Position).sqrMagnitude < PositionEpsilon * PositionEpsilon
                && velocity.sqrMagnitude < VelocityEpsilon * VelocityEpsilon;
            if (positionSettled)
            {
                position = target.Position;
                velocity = Vector2.zero;
            }

            _velocities[card] = velocity;
            rect.anchoredPosition = position;

            float angle = rect.localEulerAngles.z;
            float scale = rect.localScale.x;
            bool turnSettled = Mathf.Abs(Mathf.DeltaAngle(angle, target.Rotation)) < AngleEpsilon
                && Mathf.Abs(scale - target.Scale) < ScaleEpsilon;
            if (turnSettled)
            {
                angle = target.Rotation;
                scale = target.Scale;
            }
            else
            {
                angle = Mathf.LerpAngle(angle, target.Rotation, blend);
                scale = Mathf.Lerp(scale, target.Scale, blend);
            }

            rect.localEulerAngles = new Vector3(0f, 0f, angle);
            rect.localScale = new Vector3(scale, scale, 1f);
            return positionSettled && turnSettled;
        }

        private bool ApplyDragged(RectTransform rect, float deltaTime)
        {
            float blend = 1f - Mathf.Exp(-_style.DragSharpness * deltaTime);
            float before = rect.anchoredPosition.x;
            bool settled = Apply(rect, new HandCardPose(_dragPosition, _dragTilt, _dragScale), blend);

            if (deltaTime > 0f)
            {
                float speed = (rect.anchoredPosition.x - before) / deltaTime;
                float maxTilt = _style.DragMaxTilt;
                _dragTilt = Mathf.Clamp(-speed * _style.DragTiltPerSpeed, -maxTilt, maxTilt);
            }

            return settled && Mathf.Abs(_dragTilt) < AngleEpsilon;
        }

        private static bool Apply(RectTransform rect, in HandCardPose target, float blend)
        {
            Vector2 position = rect.anchoredPosition;
            float angle = rect.localEulerAngles.z;
            float scale = rect.localScale.x;

            bool settled = (position - target.Position).sqrMagnitude < PositionEpsilon * PositionEpsilon
                && Mathf.Abs(Mathf.DeltaAngle(angle, target.Rotation)) < AngleEpsilon
                && Mathf.Abs(scale - target.Scale) < ScaleEpsilon;

            if (settled || blend >= 1f)
            {
                position = target.Position;
                angle = target.Rotation;
                scale = target.Scale;
            }
            else
            {
                position = Vector2.Lerp(position, target.Position, blend);
                angle = Mathf.LerpAngle(angle, target.Rotation, blend);
                scale = Mathf.Lerp(scale, target.Scale, blend);
            }

            rect.anchoredPosition = position;
            rect.localEulerAngles = new Vector3(0f, 0f, angle);
            rect.localScale = new Vector3(scale, scale, 1f);
            return settled;
        }

        private int IndexOf(CardView view)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == view)
                {
                    return i;
                }
            }

            return -1;
        }

        private void SetRegistered(bool registered)
        {
            if (registered && _ticks == null)
            {
                ServiceLocator.TryGet(out _ticks);
            }

            if (_ticks == null || _registered == registered)
            {
                return;
            }

            _registered = registered;
            if (registered)
            {
                _ticks.AddTickable(this);
            }
            else
            {
                _ticks.RemoveTickable(this);
            }
        }
    }
}
