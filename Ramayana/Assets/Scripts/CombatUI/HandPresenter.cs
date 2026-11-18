// Keeps the hand of card views in step with the deck: a card entering the hand takes a pooled view, a card
// leaving returns it. It orders views to match the hand, lets the HandLayoutAnimator place them with the style's
// HandLayoutSO strategy (spreading around the focused card), dims cards that cannot be played right now and glows
// the ones that can, turns a card's cost badge red when the player lacks the Prana for it, starts the other cards
// moving outward from the slot when a card joins or leaves, and wires explicit left/right navigation with End Turn
// at the end and up to the neighbour the HUD gives (SetUpNeighbour, the quiver); NavigationChanged fires after the
// cards are rewired, and FirstCardButton is where that neighbour leads back down to. HasPlayableCard (and
// PlayableChanged) tell the HUD when nothing in the hand can be played.
// Each card's description shows live numbers: the hand gives every card a preview context (the combat resolver and
// Rama, plus the highlighted enemy for the card being targeted) and refreshes it when damage modifiers change or the
// highlighted enemy changes. ConsideredCardChanged reports the card the player is considering (the dragged card,
// else the card being targeted, else the focused card; null for none), so the Prana orbs can preview its cost and
// the outcome preview can show what it would do.
// The mouse reaches the hand only through the HandInputSurface: hover comes from HandPointerHover and presses from the
// same resting-layout hit test, so overlapping fanned cards never fight over the pointer and the zoomed copy is
// clickable. A click plays the card under the press; dragging a card hands it to the HandDragController (drag to
// play). Keyboard and gamepad use the cards' own buttons. Clicking a card that needs a target hands it to the
// TargetingPresenter when there is more than one living enemy; otherwise the card plays at once (at the only living
// enemy, or untargeted). Clicking the lifted card again cancels. Clicking or dragging a card that cannot be played
// wiggles it and raises PlayRejected with the reason, so the HUD can say why. Cards entering the hand during a fight
// fly in through the CardMotionPresenter, and cards leaving fly out from their slot (or from wherever they were
// dragged); a card that is already in the hand when the HUD attaches simply appears. Card focus goes to the
// CardZoomPresenter, which zooms only during the player's turn, outside targeting and dragging, and while the hand is
// not blocked (the HUD blocks it while the pile viewer is open). While a special arrow is in flight
// (CombatFlow.CanPlayerAct is false) clicks and drags on cards are ignored; the hand keeps its look, since the flight
// is too short for dimming it to read as anything but a flicker.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Pooling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class HandPresenter
    {
        private readonly ComponentPool<CardView> _pool;
        private readonly List<CardView> _views = new List<CardView>(12);
        private readonly HandLayoutAnimator _animator;
        private readonly HandPointerHover _hover;
        private readonly CanvasGroup _group;
        private readonly Button _endTurn;
        private readonly TargetingPresenter _targeting;
        private readonly CardMotionPresenter _motion;
        private readonly CardZoomPresenter _zoom;
        private readonly RectTransform _handArea;
        private readonly HandInputSurface _surface;
        private readonly HandDragController _drag;
        private CombatHudStyleSO _style;
        private CombatFlow _flow;
        private CardView _pointerHovered;
        private bool _blocked;
        private bool _hasPlayableCard;
        private CardTextContext _textContext;
        private int _targetIndex = -1;
        private CardInstance _consideredCard;
        private Selectable _upNeighbour;

        public HandPresenter(CardView template, RectTransform handArea, HandInputSurface surface, CanvasGroup group,
            Button endTurn, TargetingPresenter targeting, TargetingLinePresenter targetingLine,
            CardMotionPresenter motion, CardZoomPresenter zoom)
        {
            _pool = new ComponentPool<CardView>(template, handArea, 12, 32);
            _animator = new HandLayoutAnimator(_views);
            _hover = new HandPointerHover(handArea, _animator);
            _drag = new HandDragController(_animator, handArea, targeting, targetingLine);
            _handArea = handArea;
            _surface = surface;
            _group = group;
            _endTurn = endTurn;
            _targeting = targeting;
            _motion = motion;
            _zoom = zoom;
            _targeting.Cancelled += HandleTargetingCancelled;
            _targeting.ActiveChanged += HandleTargetingActiveChanged;
            _targeting.TargetChanged += HandleTargetChanged;
            _hover.HoveredChanged += HandlePointerHoverChanged;
            _zoom.FocusedCardChanged += HandleFocusedCardChanged;
            _drag.DraggingChanged += HandleDraggingChanged;
            _drag.PlayRejected += Reject;
            _surface.SetHitTest(HitTest);
            _surface.Clicked += HandleSurfaceClicked;
            _surface.DragStarted += HandleSurfaceDragStarted;
            _surface.Dragged += HandleSurfaceDragged;
            _surface.DragEnded += HandleSurfaceDragEnded;
        }

        public event Action<CardPlayCheck> PlayRejected;

        public event Action PlayableChanged;

        public event Action<CardInstance> ConsideredCardChanged;

        public event Action NavigationChanged;

        public bool HasPlayableCard => _hasPlayableCard;

        public Selectable FirstCardButton => _views.Count > 0 ? _views[0].Button : null;

        public void SetStyle(CombatHudStyleSO style)
        {
            _style = style;
            _animator.SetStyle(style);
            _drag.SetStyle(style);
            if (_style == null)
            {
                return;
            }

            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].ApplyStyle(_style);
            }

            Refresh();
        }

        public void Attach(CombatFlow flow)
        {
            Detach();
            _flow = flow;
            CombatModel model = flow.Model;
            CombatDeck deck = model.Deck;
            _textContext = new CardTextContext(model.Resolver, model.Player, null);
            _targetIndex = -1;

            _pool.Prewarm(deck.MaxHandSize);
            deck.CardMoved += HandleCardMoved;
            model.Resolver.ModifiersChanged += HandleModifiersChanged;
            _drag.Attach(flow);

            for (int i = 0; i < deck.Hand.Count; i++)
            {
                AddView(deck.Hand[i]);
            }

            Refresh();
            for (int i = 0; i < _views.Count; i++)
            {
                _animator.Snap(_views[i]);
            }
        }

        public void Detach()
        {
            if (_flow == null)
            {
                return;
            }

            _drag.Detach();
            _flow.Model.Deck.CardMoved -= HandleCardMoved;
            _flow.Model.Resolver.ModifiersChanged -= HandleModifiersChanged;
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                ReleaseAt(i);
            }

            _flow = null;
            _textContext = default;
            _targetIndex = -1;
            _zoom.SetAllowed(false);
            _hover.SetEnabled(false);
            SetSurfaceActive(false);
            SetHasPlayableCard(false);
            RefreshConsideredCard();
            _animator.MarkDirty();
        }

        public void Dispose()
        {
            _targeting.Cancelled -= HandleTargetingCancelled;
            _targeting.ActiveChanged -= HandleTargetingActiveChanged;
            _targeting.TargetChanged -= HandleTargetChanged;
            _hover.HoveredChanged -= HandlePointerHoverChanged;
            _zoom.FocusedCardChanged -= HandleFocusedCardChanged;
            _drag.DraggingChanged -= HandleDraggingChanged;
            _drag.PlayRejected -= Reject;
            if (_surface != null)
            {
                _surface.SetHitTest(null);
                _surface.Clicked -= HandleSurfaceClicked;
                _surface.DragStarted -= HandleSurfaceDragStarted;
                _surface.Dragged -= HandleSurfaceDragged;
                _surface.DragEnded -= HandleSurfaceDragEnded;
            }

            PlayRejected = null;
            PlayableChanged = null;
            ConsideredCardChanged = null;
            NavigationChanged = null;
            Detach();
            _drag.Dispose();
            _hover.Dispose();
            _animator.Dispose();
            _pool.Dispose();
        }

        public void Refresh()
        {
            if (_flow == null || _style == null)
            {
                return;
            }

            RefreshPlayable();
            RefreshOrder();
            RefreshNavigation();
        }

        public void RefreshPlayable()
        {
            if (_flow == null || _style == null)
            {
                return;
            }

            bool playerTurn = _flow.IsPlayerTurn;
            bool usable = playerTurn && !_blocked;
            _group.interactable = usable;
            _group.blocksRaycasts = false;
            _group.alpha = playerTurn ? 1f : _style.HandInactiveAlpha;
            SetSurfaceActive(usable);
            RefreshZoomAllowed();

            CombatModel model = _flow.Model;
            int target = model.FirstLivingEnemyIndex();
            int prana = model.Prana.Current;
            bool anyPlayable = false;
            for (int i = 0; i < _views.Count; i++)
            {
                CardView view = _views[i];
                bool playable = playerTurn && model.CheckPlay(view.CardId, target) == CardPlayCheck.Playable;
                view.SetPlayable(playable, _style.UnplayableAlpha);
                view.SetGlow(playable);
                view.SetCostAffordable(!playerTurn || view.Card == null || view.Card.Cost <= prana);
                anyPlayable |= playable;
            }

            _zoom.SyncShown();
            SetHasPlayableCard(anyPlayable);
            RefreshConsideredCard();
        }

        public void SelectFirst()
        {
            Select(0);
        }

        public void SetUpNeighbour(Selectable neighbour)
        {
            if (_upNeighbour == neighbour)
            {
                return;
            }

            _upNeighbour = neighbour;
            RefreshNavigation();
        }

        public void SetBlocked(bool blocked)
        {
            if (_blocked == blocked)
            {
                return;
            }

            _blocked = blocked;
            if (blocked)
            {
                _drag.Cancel();
            }

            RefreshPlayable();
            RefreshZoomAllowed();
        }

        private void HandleCardMoved(CardInstance card, CardPile from, CardPile to)
        {
            CardPile hand = _flow.Model.Deck.Hand;
            if (to == hand)
            {
                CardView view = AddView(card);
                Refresh();
                _animator.Snap(view);
                _motion.DrawIntoHand(view, card, from);
            }
            else if (from == hand)
            {
                RemoveView(card, to);
            }
        }

        private CardView AddView(CardInstance card)
        {
            CardView view = _pool.Get();
            if (_style != null)
            {
                view.ApplyStyle(_style);
            }

            view.Bind(card);
            view.SetTextContext(_textContext);
            view.UseExternalHover(true);
            view.SetCostAffordable(true);
            view.Clicked += HandleCardClicked;
            view.FocusChanged += HandleCardFocusChanged;
            view.LiftChanged += HandleCardLiftChanged;
            _animator.PrepareCard(view);

            int index = _flow.Model.Deck.Hand.IndexOf(card.Id);
            if (index < 0 || index > _views.Count)
            {
                index = _views.Count;
            }

            _views.Insert(index, view);
            _animator.HoldFrom(index, false);
            return view;
        }

        private void RemoveView(CardInstance card, CardPile to)
        {
            int index = IndexOfView(card.Id);
            if (index < 0)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            bool wasSelected = eventSystem != null && eventSystem.currentSelectedGameObject == _views[index].gameObject;

            if (_drag.DraggedCard == _views[index])
            {
                _drag.Cancel();
            }

            if (_targeting.IsActive && _targeting.CardId == card.Id)
            {
                _targeting.Abort();
            }

            _motion.LaunchFromHand(_views[index], card, to);
            ReleaseAt(index);
            _animator.HoldFrom(index, true);
            Refresh();

            if (wasSelected)
            {
                Select(Mathf.Min(index, _views.Count - 1));
            }
        }

        private void ReleaseAt(int index)
        {
            CardView view = _views[index];
            _views.RemoveAt(index);
            view.Clicked -= HandleCardClicked;
            view.FocusChanged -= HandleCardFocusChanged;
            view.LiftChanged -= HandleCardLiftChanged;
            if (view == _pointerHovered)
            {
                _pointerHovered = null;
            }

            _zoom.Release(view);
            _pool.Release(view);
            RefreshConsideredCard();
        }

        private bool HitTest(Vector2 screenPoint, Camera eventCamera)
        {
            return FindCardIndex(screenPoint, eventCamera) >= 0;
        }

        private int FindCardIndex(Vector2 screenPoint, Camera eventCamera)
        {
            HandLayoutSO layout = _animator.Layout;
            if (layout == null || _views.Count == 0)
            {
                return -1;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_handArea, screenPoint, eventCamera, out Vector2 local))
            {
                return -1;
            }

            int index = layout.FindCardAt(_animator.CreateInput(-1), local, _hover.Hovered);
            return index >= 0 && index < _views.Count && _views[index].IsRevealed ? index : -1;
        }

        private void SetSurfaceActive(bool active)
        {
            if (_surface != null)
            {
                _surface.raycastTarget = active;
            }
        }

        private bool CanTakePointer()
        {
            return _flow != null && _flow.CanPlayerAct && !_blocked && _style != null;
        }

        private void HandleSurfaceClicked(PointerEventData eventData)
        {
            if (!CanTakePointer() || _drag.IsDragging)
            {
                return;
            }

            int index = FindCardIndex(eventData.pressPosition, eventData.pressEventCamera);
            if (index >= 0)
            {
                HandleCardClicked(_views[index]);
            }
        }

        private void HandleSurfaceDragStarted(PointerEventData eventData)
        {
            if (!CanTakePointer())
            {
                return;
            }

            int index = FindCardIndex(eventData.pressPosition, eventData.pressEventCamera);
            if (index < 0)
            {
                return;
            }

            CardView view = _views[index];
            CombatModel model = _flow.Model;
            if (!model.Deck.TryGetFromHand(view.CardId, out CardInstance card))
            {
                return;
            }

            CardPlayCheck check = model.CheckPlay(card.Id, model.FirstLivingEnemyIndex());
            if (check != CardPlayCheck.Playable)
            {
                Reject(view, check);
                return;
            }

            _drag.TryBegin(view, card, eventData);
        }

        private void HandleSurfaceDragged(PointerEventData eventData)
        {
            _drag.Drag(eventData);
        }

        private void HandleSurfaceDragEnded(PointerEventData eventData)
        {
            _drag.End(eventData);
        }

        private void HandleCardClicked(CardView view)
        {
            if (_flow == null || !_flow.CanPlayerAct || _drag.IsDragging)
            {
                return;
            }

            CombatModel model = _flow.Model;
            if (!model.Deck.TryGetFromHand(view.CardId, out CardInstance card))
            {
                return;
            }

            if (_targeting.IsActive && _targeting.CardId == card.Id)
            {
                _targeting.Cancel();
                return;
            }

            CardPlayCheck check = model.CheckPlay(card.Id, model.FirstLivingEnemyIndex());
            if (check != CardPlayCheck.Playable)
            {
                Reject(view, check);
                return;
            }

            if (card.RequiresChosenTarget && _targeting.TryBegin(view, card))
            {
                return;
            }

            _targeting.Abort();
            int target = card.RequiresChosenTarget ? model.FirstLivingEnemyIndex() : -1;
            _flow.PlayCard(card.Id, target);
        }

        private void Reject(CardView view, CardPlayCheck check)
        {
            if (_style != null && !_zoom.TryPlayRejected(view, _style.RejectAngle, _style.RejectDuration))
            {
                view.PlayRejected(_style.RejectAngle, _style.RejectDuration);
            }

            PlayRejected?.Invoke(check);
        }

        private void HandleCardFocusChanged(CardView view)
        {
            _zoom.HandleFocusChanged(view);
        }

        private void HandleTargetingActiveChanged(bool active)
        {
            RefreshZoomAllowed();
            RefreshConsideredCard();
        }

        private void HandleTargetChanged(int targetIndex)
        {
            _targetIndex = targetIndex;
            RefreshTextContexts();
        }

        private void HandleModifiersChanged()
        {
            RefreshTextContexts();
        }

        private void RefreshTextContexts()
        {
            if (_flow == null)
            {
                return;
            }

            IReadOnlyList<ICombatant> enemies = _flow.Model.EnemyFighters;
            CardView lifted = _targeting.LiftedCard;
            ICombatant target = _targetIndex >= 0 && _targetIndex < enemies.Count ? enemies[_targetIndex] : null;
            for (int i = 0; i < _views.Count; i++)
            {
                CardView view = _views[i];
                view.SetTextContext(view == lifted && target != null ? _textContext.WithTarget(target) : _textContext);
            }

            _zoom.SyncShown();
        }

        private void HandleDraggingChanged()
        {
            RefreshZoomAllowed();
            ApplySiblingOrder();
            RefreshConsideredCard();
        }

        private void RefreshConsideredCard()
        {
            CardInstance considered = null;
            if (_flow != null && _flow.IsPlayerTurn && !_blocked)
            {
                CardView card = _drag.DraggedCard;
                if (card == null)
                {
                    card = _targeting.LiftedCard;
                }

                if (card == null)
                {
                    card = _zoom.FocusedCard;
                }

                if (card != null)
                {
                    considered = card.Card;
                }
            }

            if (considered == _consideredCard)
            {
                return;
            }

            _consideredCard = considered;
            ConsideredCardChanged?.Invoke(considered);
        }

        private void SetHasPlayableCard(bool hasPlayableCard)
        {
            if (_hasPlayableCard == hasPlayableCard)
            {
                return;
            }

            _hasPlayableCard = hasPlayableCard;
            PlayableChanged?.Invoke();
        }

        private void HandlePointerHoverChanged(int index)
        {
            CardView next = index >= 0 && index < _views.Count ? _views[index] : null;
            if (next == _pointerHovered)
            {
                return;
            }

            CardView previous = _pointerHovered;
            _pointerHovered = next;
            if (previous != null)
            {
                previous.SetExternalHover(false);
            }

            if (next != null)
            {
                next.SetExternalHover(true);
            }
        }

        private void HandleFocusedCardChanged(CardView focused)
        {
            ApplySiblingOrder();
            _animator.SetHovered(IndexOfFocused());
            RefreshConsideredCard();
        }

        private void HandleCardLiftChanged(CardView view)
        {
            _animator.MarkDirty();
        }

        private void RefreshZoomAllowed()
        {
            bool interactive = _flow != null && _flow.IsPlayerTurn && !_blocked && !_targeting.IsActive
                && !_drag.IsDragging;
            _zoom.SetAllowed(interactive);
            _hover.SetEnabled(interactive);
        }

        private int IndexOfFocused()
        {
            CardView focused = _zoom.FocusedCard;
            return focused != null ? _views.IndexOf(focused) : -1;
        }

        private void HandleTargetingCancelled(CardView view)
        {
            int index = _views.IndexOf(view);
            if (index >= 0)
            {
                Select(index);
            }
        }

        private void RefreshOrder()
        {
            ApplySiblingOrder();
            _animator.SetHovered(IndexOfFocused());
            _animator.MarkDirty();
            _hover.MarkDirty();
        }

        private void ApplySiblingOrder()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].transform.SetSiblingIndex(i);
            }

            CardView top = _drag.DraggedCard != null ? _drag.DraggedCard : _zoom.FocusedCard;
            if (top != null && _views.Contains(top))
            {
                top.transform.SetAsLastSibling();
            }
        }

        private void RefreshNavigation()
        {
            int count = _views.Count;
            for (int i = 0; i < count; i++)
            {
                var navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? _views[i - 1].Button : null,
                    selectOnRight = i < count - 1 ? _views[i + 1].Button : _endTurn,
                    selectOnUp = _upNeighbour,
                    selectOnDown = _endTurn,
                };
                _views[i].Button.navigation = navigation;
            }

            Selectable lastCard = count > 0 ? _views[count - 1].Button : null;
            _endTurn.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = lastCard,
                selectOnUp = lastCard != null ? lastCard : _upNeighbour,
            };

            NavigationChanged?.Invoke();
        }

        private void Select(int index)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            GameObject target = index >= 0 && index < _views.Count && _flow != null && _flow.IsPlayerTurn
                ? _views[index].gameObject
                : _endTurn.gameObject;

            if (target.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(target);
            }
        }

        private int IndexOfView(int cardId)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i].CardId == cardId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
