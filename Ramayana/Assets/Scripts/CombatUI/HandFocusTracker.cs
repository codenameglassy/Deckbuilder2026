// Decides which hand card has focus. Mouse hover always counts as focus; UI selection counts only after keyboard or
// gamepad navigation, so the card selected at the start of a turn does not rise for a mouse player. The hand turns
// focus off while targeting or dragging, outside the player's turn and while the pile viewer is open. FocusedCard is
// the focused card while focus is allowed (null otherwise); FocusedCardChanged fires when it changes, so the hand
// layout can raise, enlarge and straighten exactly that card in its own slot (the HandLayoutSO hover pose) and spread
// its neighbours. It never draws anything itself.
using System;
using UnityEngine.InputSystem;

namespace JourneyToLanka.CombatUI
{
    internal sealed class HandFocusTracker : IDisposable
    {
        private readonly InputAction _navigate;
        private readonly InputAction _pointerMoved;
        private CardView _focused;
        private bool _allowed;
        private bool _navigationMode;

        public HandFocusTracker()
        {
            _navigate = new InputAction("HandFocusNavigate", InputActionType.PassThrough);
            _navigate.AddBinding("<Keyboard>/leftArrow");
            _navigate.AddBinding("<Keyboard>/rightArrow");
            _navigate.AddBinding("<Keyboard>/upArrow");
            _navigate.AddBinding("<Keyboard>/downArrow");
            _navigate.AddBinding("<Keyboard>/w");
            _navigate.AddBinding("<Keyboard>/a");
            _navigate.AddBinding("<Keyboard>/s");
            _navigate.AddBinding("<Keyboard>/d");
            _navigate.AddBinding("<Keyboard>/tab");
            _navigate.AddBinding("<Gamepad>/dpad");
            _navigate.AddBinding("<Gamepad>/leftStick");
            _navigate.performed += HandleNavigate;

            _pointerMoved = new InputAction("HandFocusPointerMoved", InputActionType.PassThrough, "<Mouse>/delta");
            _pointerMoved.performed += HandlePointerMoved;

            _navigate.Enable();
            _pointerMoved.Enable();
        }

        public event Action<CardView> FocusedCardChanged;

        public CardView FocusedCard => _allowed ? _focused : null;

        public void SetAllowed(bool allowed)
        {
            if (_allowed == allowed)
            {
                return;
            }

            CardView before = FocusedCard;
            _allowed = allowed;
            RaiseIfFocusedCardChanged(before);
        }

        public void HandleFocusChanged(CardView view)
        {
            if (view == null)
            {
                return;
            }

            if (WantsFocus(view))
            {
                SetFocused(view);
            }
            else if (view == _focused)
            {
                SetFocused(null);
            }
        }

        public void Release(CardView view)
        {
            if (view != null && view == _focused)
            {
                SetFocused(null);
            }
        }

        public void Dispose()
        {
            FocusedCardChanged = null;
            _focused = null;
            _navigate.performed -= HandleNavigate;
            _pointerMoved.performed -= HandlePointerMoved;
            _navigate.Dispose();
            _pointerMoved.Dispose();
        }

        private void HandleNavigate(InputAction.CallbackContext context)
        {
            _navigationMode = true;
        }

        private void HandlePointerMoved(InputAction.CallbackContext context)
        {
            _navigationMode = false;
        }

        private bool WantsFocus(CardView view)
        {
            return view.IsPointerInside || (view.IsSelected && _navigationMode);
        }

        private void SetFocused(CardView view)
        {
            CardView before = FocusedCard;
            _focused = view;
            RaiseIfFocusedCardChanged(before);
        }

        private void RaiseIfFocusedCardChanged(CardView before)
        {
            CardView after = FocusedCard;
            if (after != before)
            {
                FocusedCardChanged?.Invoke(after);
            }
        }
    }
}
