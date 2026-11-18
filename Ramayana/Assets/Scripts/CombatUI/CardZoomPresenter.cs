// Card zoom for the hand: when a card has focus, an enlarged copy appears over its slot on the HUD's motion canvas
// while the hand card hides behind it (still receiving the pointer), so the card seems to grow in place without the
// hand layout moving or rebuilding. Mouse hover always counts as focus; UI selection counts only after keyboard or
// gamepad navigation, so the card selected at the start of a turn does not zoom for a mouse player. Showing and
// hiding wait a short delay, and moving focus between cards swaps the copy at once, so sweeping across the hand does
// not flicker. The hand turns zoom off while targeting, outside the player's turn and while the pile viewer is open.
// The copy is one CardView built from the hand template; tweens are DOTween core on unscaled time.
// FocusedCard is the card that currently has focus while zoom is allowed (null otherwise); FocusedCardChanged fires
// when it changes, so the hand layout can spread around exactly the card being zoomed. The copy mirrors the hand
// card's cost colour, live description numbers and playable glow, and SyncShown refreshes them while it is up.
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JourneyToLanka.CombatUI
{
    internal sealed class CardZoomPresenter : IDisposable
    {
        private readonly RectTransform _root;
        private readonly CardView _copy;
        private readonly RectTransform _copyRect;
        private readonly InputAction _navigate;
        private readonly InputAction _pointerMoved;
        private readonly TweenCallback _showFocused;
        private readonly TweenCallback _hideIfUnfocused;
        private readonly TweenCallback _clearGrow;
        private CombatHudStyleSO _cardStyle;
        private CombatInspectStyleSO _style;
        private Tween _delay;
        private Tween _grow;
        private CardView _focused;
        private CardView _shownFor;
        private bool _allowed;
        private bool _navigationMode;

        public event Action<CardView> FocusedCardChanged;

        public CardView FocusedCard => _allowed ? _focused : null;

        public CardZoomPresenter(CardView template, RectTransform root)
        {
            _root = root;
            _copy = Object.Instantiate(template, root);
            _copy.name = "ZoomedCard";
            _copy.Button.navigation = new Navigation { mode = Navigation.Mode.None };
            _copy.Group.interactable = false;
            _copy.Group.blocksRaycasts = false;
            _copyRect = (RectTransform)_copy.transform;
            _copy.gameObject.SetActive(false);

            _showFocused = () =>
            {
                _delay = null;
                ShowFocused();
            };
            _hideIfUnfocused = () =>
            {
                _delay = null;
                if (_focused == null)
                {
                    HideNow();
                }
            };
            _clearGrow = () => _grow = null;

            _navigate = new InputAction("ZoomNavigate", InputActionType.PassThrough);
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
            _navigate.performed += _ => _navigationMode = true;

            _pointerMoved = new InputAction("ZoomPointerMoved", InputActionType.PassThrough, "<Mouse>/delta");
            _pointerMoved.performed += _ => _navigationMode = false;

            _navigate.Enable();
            _pointerMoved.Enable();
        }

        public void SetStyles(CombatHudStyleSO cardStyle, CombatInspectStyleSO style)
        {
            _cardStyle = cardStyle;
            _style = style;
            if (_cardStyle != null)
            {
                _copy.ApplyStyle(_cardStyle);
            }

            if (_shownFor != null && _cardStyle != null && _style != null)
            {
                Place(_shownFor);
                _copyRect.localScale = Vector3.one * _style.ZoomScale;
            }
        }

        public void SetAllowed(bool allowed)
        {
            if (_allowed == allowed)
            {
                return;
            }

            CardView before = FocusedCard;
            _allowed = allowed;
            RaiseIfFocusedCardChanged(before);
            if (!_allowed)
            {
                StopDelay();
                HideNow();
            }
            else if (_focused != null)
            {
                Schedule(_showFocused);
            }
        }

        public void HandleFocusChanged(CardView view)
        {
            if (WantsFocus(view))
            {
                SetFocused(view);
                if (_shownFor != null && _allowed)
                {
                    StopDelay();
                    ShowFocused();
                }
                else
                {
                    Schedule(_showFocused);
                }

                return;
            }

            if (view == _focused)
            {
                SetFocused(null);
                Schedule(_hideIfUnfocused);
            }
        }

        public void SyncShown()
        {
            if (_shownFor != null)
            {
                _copy.SetCostAffordable(_shownFor.IsCostAffordable);
                _copy.SetTextContext(_shownFor.TextContext);
                _copy.SetGlow(_shownFor.IsGlowing);
            }
        }

        public void Release(CardView view)
        {
            if (view == _focused)
            {
                SetFocused(null);
            }

            if (view == _shownFor)
            {
                StopDelay();
                HideNow();
            }
        }

        public bool TryPlayRejected(CardView view, float angle, float duration)
        {
            if (view == null || view != _shownFor)
            {
                return false;
            }

            _copy.PlayRejected(angle, duration);
            return true;
        }

        public void HideNow()
        {
            StopGrow();
            if (_shownFor != null)
            {
                _shownFor.SetZoomHidden(false);
                _shownFor = null;
            }

            _copy.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            FocusedCardChanged = null;
            StopDelay();
            HideNow();
            _navigate.Dispose();
            _pointerMoved.Dispose();
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

        private bool WantsFocus(CardView view)
        {
            return view.IsPointerInside || (view.IsSelected && _navigationMode);
        }

        private void ShowFocused()
        {
            CardView view = _focused;
            if (!_allowed || view == null || view.Card == null || !view.IsRevealed || _style == null || _cardStyle == null)
            {
                HideNow();
                return;
            }

            if (view == _shownFor)
            {
                return;
            }

            bool swapping = _shownFor != null;
            if (swapping)
            {
                _shownFor.SetZoomHidden(false);
            }

            _shownFor = view;
            _copy.gameObject.SetActive(true);
            _copy.OnTakenFromPool();
            _copy.Group.blocksRaycasts = false;
            _copy.Bind(view.Card);
            _copy.SetCostAffordable(view.IsCostAffordable);
            _copy.SetTextContext(view.TextContext);
            _copy.SetGlow(view.IsGlowing);
            _copyRect.SetAsLastSibling();
            Place(view);
            view.SetZoomHidden(true);

            StopGrow();
            if (swapping)
            {
                _copyRect.localScale = Vector3.one * _style.ZoomScale;
                return;
            }

            _copyRect.localScale = view.transform.localScale;
            _grow = _copyRect.DOScale(_style.ZoomScale, _style.ZoomDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(_clearGrow);
        }

        private void Place(CardView view)
        {
            var handRect = (RectTransform)view.transform;
            Vector3 local = _root.InverseTransformPoint(CardFlight.WorldCenter(handRect));
            Vector2 size = _cardStyle.CardSize;
            local.y += (_style.ZoomScale - 1f) * size.y * 0.5f + _style.ZoomLift;

            Rect bounds = _root.rect;
            float halfWidth = size.x * _style.ZoomScale * 0.5f + _style.ZoomScreenMargin;
            if (bounds.width > halfWidth * 2f)
            {
                local.x = Mathf.Clamp(local.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
            }

            _copyRect.position = _root.TransformPoint(local);
        }

        private void Schedule(TweenCallback callback)
        {
            StopDelay();
            float delay = _style != null ? _style.ZoomDelay : 0f;
            _delay = DOVirtual.DelayedCall(delay, callback, true);
        }

        private void StopDelay()
        {
            if (_delay == null)
            {
                return;
            }

            Tween delay = _delay;
            _delay = null;
            delay.Kill();
        }

        private void StopGrow()
        {
            if (_grow == null)
            {
                return;
            }

            Tween grow = _grow;
            _grow = null;
            grow.Kill();
        }
    }
}
