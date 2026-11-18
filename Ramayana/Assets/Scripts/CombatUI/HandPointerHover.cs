// Decides which hand card the mouse is over from the resting layout (HandLayoutSO.FindCardAt), not from the drawn
// cards, so spreading around a hovered card can never hand the hover to a neighbour and jitter. While enabled it reads
// the pointer each frame but only recomputes when the pointer moves or the hand changes, and reports the result
// through HoveredChanged (-1 for none). The hand enables it only during the player's turn, while the hand is usable
// and nothing is being targeted. No mouse, no hover: keyboard and gamepad keep using selection.
using System;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JourneyToLanka.CombatUI
{
    internal sealed class HandPointerHover : ITickable
    {
        private readonly RectTransform _handArea;
        private readonly HandLayoutAnimator _animator;
        private ITickService _ticks;
        private Vector2 _lastPointer;
        private bool _enabled;
        private bool _dirty = true;
        private bool _registered;

        public HandPointerHover(RectTransform handArea, HandLayoutAnimator animator)
        {
            _handArea = handArea;
            _animator = animator;
        }

        public event Action<int> HoveredChanged;

        public int Hovered { get; private set; } = -1;

        public void SetEnabled(bool enabled)
        {
            if (_enabled == enabled)
            {
                return;
            }

            _enabled = enabled;
            _dirty = true;
            SetRegistered(enabled);
            if (!enabled)
            {
                SetHovered(-1);
            }
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public void Dispose()
        {
            HoveredChanged = null;
            SetRegistered(false);
        }

        public void Tick(float deltaTime)
        {
            Mouse mouse = Mouse.current;
            HandLayoutSO layout = _animator.Layout;
            if (!_enabled || mouse == null || layout == null)
            {
                SetHovered(-1);
                return;
            }

            Vector2 pointer = mouse.position.ReadValue();
            if (!_dirty && pointer == _lastPointer)
            {
                return;
            }

            _dirty = false;
            _lastPointer = pointer;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_handArea, pointer, null, out Vector2 local))
            {
                SetHovered(-1);
                return;
            }

            SetHovered(layout.FindCardAt(_animator.CreateInput(-1), local, Hovered));
        }

        private void SetHovered(int index)
        {
            if (index == Hovered)
            {
                return;
            }

            Hovered = index;
            HoveredChanged?.Invoke(index);
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
