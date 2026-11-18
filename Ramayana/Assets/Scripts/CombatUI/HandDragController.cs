// Drag to play for the hand. The hand starts a drag on a playable card and forwards the pointer here. A card that
// needs a chosen target, with more than one living enemy, stays lifted in the hand while the targeting line follows
// the pointer, locking onto an enemy while the pointer is over one; releasing on an enemy plays the card at it, and
// releasing anywhere else cancels (TargetingPresenter's drag mode). Every other card (untargeted cards, and attacks
// when only one enemy is alive) leaves the layout and follows the pointer, growing once it passes the style's play
// line; released above the line it plays (an attack at the only living enemy), released below it returns to the hand.
// Escape, a right click or the east face button cancels a drag. A drag also ends when the player's turn ends. A drag
// cannot start while a special arrow is in flight, and a card released during one returns to the hand unplayed.
// It plays through CombatFlow, so drags use the same command pipeline as clicks. It only presents and forwards.
using System;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace JourneyToLanka.CombatUI
{
    internal sealed class HandDragController : IDisposable
    {
        private enum DragMode
        {
            None,
            Free,
            Target,
        }

        private readonly HandLayoutAnimator _animator;
        private readonly RectTransform _handArea;
        private readonly TargetingPresenter _targeting;
        private readonly TargetingLinePresenter _line;
        private readonly InputAction _cancel;
        private readonly Action<bool> _targetingActiveChanged;
        private readonly Action<CombatPhase> _phaseChanged;
        private CombatHudStyleSO _style;
        private CombatFlow _flow;
        private CardView _view;
        private DragMode _mode;
        private int _cardId = -1;

        public HandDragController(HandLayoutAnimator animator, RectTransform handArea, TargetingPresenter targeting,
            TargetingLinePresenter line)
        {
            _animator = animator;
            _handArea = handArea;
            _targeting = targeting;
            _line = line;
            _targetingActiveChanged = HandleTargetingActiveChanged;
            _phaseChanged = HandlePhaseChanged;
            _targeting.ActiveChanged += _targetingActiveChanged;

            _cancel = new InputAction("DragCancel", InputActionType.Button, "<Keyboard>/escape");
            _cancel.AddBinding("<Mouse>/rightButton");
            _cancel.AddBinding("<Gamepad>/buttonEast");
            _cancel.performed += _ => Cancel();
        }

        public event Action DraggingChanged;

        public event Action<CardView, CardPlayCheck> PlayRejected;

        public bool IsDragging => _mode != DragMode.None;

        public CardView DraggedCard => _view;

        public void SetStyle(CombatHudStyleSO style)
        {
            _style = style;
        }

        public void Attach(CombatFlow flow)
        {
            Detach();
            _flow = flow;
            _flow.PhaseChanged += _phaseChanged;
        }

        public void Detach()
        {
            Cancel();
            if (_flow == null)
            {
                return;
            }

            _flow.PhaseChanged -= _phaseChanged;
            _flow = null;
        }

        public void Dispose()
        {
            Detach();
            _targeting.ActiveChanged -= _targetingActiveChanged;
            DraggingChanged = null;
            PlayRejected = null;
            _cancel.Dispose();
        }

        public bool TryBegin(CardView view, CardInstance card, PointerEventData eventData)
        {
            Cancel();
            if (_flow == null || _style == null || !_flow.CanPlayerAct || view == null || card == null)
            {
                return false;
            }

            if (card.RequiresChosenTarget && _targeting.TryBeginDrag(view, card))
            {
                if (!_targeting.IsDragMode)
                {
                    return false;
                }

                Start(view, card.Id, DragMode.Target);
                _line.SetFreeEnd(eventData.position);
                return true;
            }

            _targeting.Abort();
            Start(view, card.Id, DragMode.Free);
            _cancel.Enable();
            FollowPointer(eventData);
            return true;
        }

        public void Drag(PointerEventData eventData)
        {
            if (_mode == DragMode.Target)
            {
                _line.SetFreeEnd(eventData.position);
            }
            else if (_mode == DragMode.Free)
            {
                FollowPointer(eventData);
            }
        }

        public void End(PointerEventData eventData)
        {
            if (_mode == DragMode.Target)
            {
                _targeting.ReleaseDrag();
                Finish();
                return;
            }

            if (_mode != DragMode.Free)
            {
                return;
            }

            bool armed = IsAboveLine(eventData);
            CardView view = _view;
            int cardId = _cardId;
            Finish();
            if (armed)
            {
                Play(view, cardId);
            }
        }

        public void Cancel()
        {
            if (_mode == DragMode.Target)
            {
                _targeting.Abort();
            }

            Finish();
        }

        private void Start(CardView view, int cardId, DragMode mode)
        {
            _view = view;
            _cardId = cardId;
            _mode = mode;
            DraggingChanged?.Invoke();
        }

        private void Finish()
        {
            if (_mode == DragMode.None)
            {
                return;
            }

            if (_mode == DragMode.Free)
            {
                _animator.ClearDragged();
            }

            _cancel.Disable();
            _mode = DragMode.None;
            _view = null;
            _cardId = -1;
            DraggingChanged?.Invoke();
        }

        private void FollowPointer(PointerEventData eventData)
        {
            if (!TryGetLocalPoint(eventData, out Vector2 local))
            {
                return;
            }

            bool armed = local.y >= _style.PlayLineHeight;
            _animator.SetDragged(_view, local, armed ? _style.DragArmedScale : _style.DragScale);
        }

        private bool IsAboveLine(PointerEventData eventData)
        {
            return _style != null && TryGetLocalPoint(eventData, out Vector2 local) && local.y >= _style.PlayLineHeight;
        }

        private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_handArea, eventData.position,
                eventData.pressEventCamera, out local);
        }

        private void Play(CardView view, int cardId)
        {
            if (_flow == null || !_flow.CanPlayerAct)
            {
                return;
            }

            CombatModel model = _flow.Model;
            if (!model.Deck.TryGetFromHand(cardId, out CardInstance card))
            {
                return;
            }

            int firstTarget = model.FirstLivingEnemyIndex();
            CardPlayCheck check = model.CheckPlay(card.Id, firstTarget);
            if (check != CardPlayCheck.Playable)
            {
                PlayRejected?.Invoke(view, check);
                return;
            }

            _flow.PlayCard(card.Id, card.RequiresChosenTarget ? firstTarget : -1);
        }

        private void HandleTargetingActiveChanged(bool active)
        {
            if (!active && _mode == DragMode.Target)
            {
                Finish();
            }
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase != CombatPhase.PlayerTurn)
            {
                Cancel();
            }
        }
    }
}
