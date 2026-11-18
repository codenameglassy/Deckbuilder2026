// Manual targeting for cards and special arrows that need an enemy. Cards use two modes. Select then confirm (TryBegin): the card lifts and the
// first living enemy is highlighted; the mouse hovers an enemy to highlight it and clicks to play, keyboard and
// gamepad move the highlight left and right and confirm. Drag (TryBeginDrag): the card lifts with nothing highlighted;
// an enemy is highlighted only while the pointer is over it, and ReleaseDrag plays at that enemy or quietly puts the
// card back when there is none. In both, Escape, the east face button or a right click cancels. With one living
// enemy there is nothing to choose, so both return false and the hand plays the card directly instead. Targeting
// ends when the card is played, cancelled, or the player's turn ends. It plays through CombatFlow, so it uses the
// same command pipeline as before. ActiveChanged reports targeting starting and ending, so the hand can pause its card
// zoom and the targeting line can show from SourceRect. TargetChanged reports the highlighted enemy (-1 for none,
// including while a drag points at empty space), so the enemy info panel can follow the pick and the line can lock on.
// A special arrow that needs an enemy uses select then confirm from its quiver slot (TryBeginArrow): the line grows
// from the slot, confirming fires the arrow through CombatFlow.FireArrow, and cancelling raises ArrowCancelled with
// the slot (Cancelled is for cards only) so the HUD can put the selection back on it. LiftedCard is null meanwhile.
// While a special arrow is in flight (CombatFlow.CanPlayerAct is false) targeting does not start (the request is
// swallowed rather than handed back to be played directly) and confirming waits until the arrow has landed.
using System;
using UnityEngine;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.CombatScene;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace JourneyToLanka.CombatUI
{
    internal sealed class TargetingPresenter : IDisposable
    {
        private readonly InputAction _previous;
        private readonly InputAction _next;
        private readonly InputAction _confirm;
        private readonly InputAction _cancel;
        private CombatFlow _flow;
        private CombatController _controller;
        private CombatHudStyleSO _style;
        private CardView _cardView;
        private RectTransform _sourceRect;
        private int _cardId = -1;
        private int _arrowSlot = -1;
        private int _targetIndex = -1;
        private int _reportedTarget = -1;
        private bool _dragMode;

        public TargetingPresenter()
        {
            _previous = new InputAction("TargetPrevious", InputActionType.Button, "<Keyboard>/leftArrow");
            _previous.AddBinding("<Keyboard>/a");
            _previous.AddBinding("<Gamepad>/dpad/left");
            _previous.AddBinding("<Gamepad>/leftStick/left");
            _previous.performed += _ => Cycle(-1);

            _next = new InputAction("TargetNext", InputActionType.Button, "<Keyboard>/rightArrow");
            _next.AddBinding("<Keyboard>/d");
            _next.AddBinding("<Gamepad>/dpad/right");
            _next.AddBinding("<Gamepad>/leftStick/right");
            _next.performed += _ => Cycle(1);

            _confirm = new InputAction("TargetConfirm", InputActionType.Button, "<Keyboard>/enter");
            _confirm.AddBinding("<Keyboard>/space");
            _confirm.AddBinding("<Gamepad>/buttonSouth");
            _confirm.performed += _ => Confirm();

            _cancel = new InputAction("TargetCancel", InputActionType.Button, "<Keyboard>/escape");
            _cancel.AddBinding("<Gamepad>/buttonEast");
            _cancel.AddBinding("<Mouse>/rightButton");
            _cancel.performed += _ => Cancel();
        }

        public event Action<CardView> Cancelled;

        public event Action<int> ArrowCancelled;

        public event Action<int> TargetChanged;

        public event Action<bool> ActiveChanged;

        public bool IsActive => _cardView != null || _arrowSlot >= 0;

        public bool IsArrowMode => _arrowSlot >= 0;

        public bool IsDragMode => IsActive && _dragMode;

        public int CardId => _cardId;

        public int ArrowSlot => _arrowSlot;

        public CardView LiftedCard => _cardView;

        public RectTransform SourceRect => _sourceRect;

        public void SetStyle(CombatHudStyleSO style)
        {
            _style = style;
            if (_cardView != null && _style != null)
            {
                _cardView.SetLifted(true, _style.TargetingCardScale);
            }
        }

        public void Attach(CombatFlow flow, CombatController controller)
        {
            Detach();
            _flow = flow;
            _controller = controller;
            _flow.PhaseChanged += HandlePhaseChanged;

            for (int i = 0; i < flow.Model.Enemies.Count; i++)
            {
                EnemyTargetView target = controller != null ? controller.GetEnemyTarget(i) : null;
                if (target != null)
                {
                    target.Clicked += HandleTargetClicked;
                    target.Hovered += HandleTargetHovered;
                    target.Unhovered += HandleTargetUnhovered;
                }
            }
        }

        public void Detach()
        {
            End(false);
            if (_flow == null)
            {
                return;
            }

            _flow.PhaseChanged -= HandlePhaseChanged;
            for (int i = 0; i < _flow.Model.Enemies.Count; i++)
            {
                EnemyTargetView target = _controller != null ? _controller.GetEnemyTarget(i) : null;
                if (target != null)
                {
                    target.Clicked -= HandleTargetClicked;
                    target.Hovered -= HandleTargetHovered;
                    target.Unhovered -= HandleTargetUnhovered;
                }
            }

            _flow = null;
            _controller = null;
        }

        public void Dispose()
        {
            Detach();
            Cancelled = null;
            ArrowCancelled = null;
            TargetChanged = null;
            ActiveChanged = null;
            _previous.Dispose();
            _next.Dispose();
            _confirm.Dispose();
            _cancel.Dispose();
        }

        public bool TryBegin(CardView view, CardInstance card)
        {
            return TryBegin(view, card, false);
        }

        public bool TryBeginDrag(CardView view, CardInstance card)
        {
            return TryBegin(view, card, true);
        }

        public bool TryBeginArrow(RectTransform slotRect, int slot)
        {
            if (_flow == null || slotRect == null || !_flow.IsPlayerTurn || CountLivingEnemies() <= 1)
            {
                return false;
            }

            if (!_flow.CanPlayerAct)
            {
                return true;
            }

            int firstTarget = _flow.Model.FirstLivingEnemyIndex();
            if (_flow.Model.CheckArrow(slot, firstTarget) != ArrowUseCheck.Usable)
            {
                return true;
            }

            End(false);
            _arrowSlot = slot;
            _sourceRect = slotRect;
            _dragMode = false;
            _targetIndex = firstTarget;
            BeginInput();
            return true;
        }

        public void ReleaseDrag()
        {
            if (!IsDragMode)
            {
                return;
            }

            if (IsLivingEnemy(_targetIndex))
            {
                Confirm();
            }
            else
            {
                End(false);
            }
        }

        public void Cancel()
        {
            End(true);
        }

        public void Abort()
        {
            End(false);
        }

        private bool TryBegin(CardView view, CardInstance card, bool dragMode)
        {
            if (_flow == null || !_flow.IsPlayerTurn || CountLivingEnemies() <= 1)
            {
                return false;
            }

            if (!_flow.CanPlayerAct)
            {
                return true;
            }

            int firstTarget = _flow.Model.FirstLivingEnemyIndex();
            if (_flow.Model.CheckPlay(card.Id, firstTarget) != CardPlayCheck.Playable)
            {
                return true;
            }

            End(false);
            _cardView = view;
            _sourceRect = (RectTransform)view.transform;
            _cardId = card.Id;
            _dragMode = dragMode;
            _targetIndex = dragMode ? -1 : firstTarget;
            if (_style != null)
            {
                _cardView.SetLifted(true, _style.TargetingCardScale);
            }

            BeginInput();
            return true;
        }

        private void BeginInput()
        {
            RefreshHighlights();
            if (!_dragMode)
            {
                _previous.Enable();
                _next.Enable();
                _confirm.Enable();
            }

            _cancel.Enable();

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }

            ActiveChanged?.Invoke(true);
        }

        private void Confirm()
        {
            if (!IsActive || !IsLivingEnemy(_targetIndex) || !_flow.CanPlayerAct)
            {
                return;
            }

            int cardId = _cardId;
            int arrowSlot = _arrowSlot;
            int target = _targetIndex;
            End(false);
            if (arrowSlot >= 0)
            {
                _flow.FireArrow(arrowSlot, target);
            }
            else
            {
                _flow.PlayCard(cardId, target);
            }
        }

        private void Cycle(int direction)
        {
            if (!IsActive)
            {
                return;
            }

            int count = _flow.Model.Enemies.Count;
            int index = _targetIndex;
            for (int step = 0; step < count; step++)
            {
                index = (index + direction + count) % count;
                if (IsLivingEnemy(index))
                {
                    _targetIndex = index;
                    break;
                }
            }

            RefreshHighlights();
        }

        private void HandleTargetHovered(EnemyTargetView target)
        {
            if (IsActive && IsLivingEnemy(target.EnemyIndex))
            {
                _targetIndex = target.EnemyIndex;
                RefreshHighlights();
            }
        }

        private void HandleTargetUnhovered(EnemyTargetView target)
        {
            if (IsDragMode && target.EnemyIndex == _targetIndex)
            {
                _targetIndex = -1;
                RefreshHighlights();
            }
        }

        private void HandleTargetClicked(EnemyTargetView target)
        {
            if (IsActive && !_dragMode && IsLivingEnemy(target.EnemyIndex))
            {
                _targetIndex = target.EnemyIndex;
                Confirm();
            }
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase != CombatPhase.PlayerTurn)
            {
                End(false);
            }
        }

        private void End(bool notifyCancelled)
        {
            if (!IsActive)
            {
                return;
            }

            CardView view = _cardView;
            int arrowSlot = _arrowSlot;
            if (view != null && view.CardId == _cardId)
            {
                view.SetLifted(false, 1f);
            }

            _cardView = null;
            _sourceRect = null;
            _cardId = -1;
            _arrowSlot = -1;
            _targetIndex = -1;
            _dragMode = false;
            RefreshHighlights();
            _previous.Disable();
            _next.Disable();
            _confirm.Disable();
            _cancel.Disable();
            ActiveChanged?.Invoke(false);

            if (!notifyCancelled)
            {
                return;
            }

            if (arrowSlot >= 0)
            {
                ArrowCancelled?.Invoke(arrowSlot);
            }
            else
            {
                Cancelled?.Invoke(view);
            }
        }

        private void RefreshHighlights()
        {
            int reported = IsActive ? _targetIndex : -1;
            if (reported != _reportedTarget)
            {
                _reportedTarget = reported;
                TargetChanged?.Invoke(reported);
            }

            if (_flow == null || _controller == null)
            {
                return;
            }

            for (int i = 0; i < _flow.Model.Enemies.Count; i++)
            {
                EnemyTargetView target = _controller.GetEnemyTarget(i);
                if (target != null)
                {
                    target.SetHighlighted(IsActive && i == _targetIndex);
                }
            }
        }

        private int CountLivingEnemies()
        {
            int living = 0;
            var enemies = _flow.Model.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive)
                {
                    living++;
                }
            }

            return living;
        }

        private bool IsLivingEnemy(int index)
        {
            var enemies = _flow.Model.Enemies;
            return index >= 0 && index < enemies.Count && enemies[index].IsAlive;
        }
    }
}
