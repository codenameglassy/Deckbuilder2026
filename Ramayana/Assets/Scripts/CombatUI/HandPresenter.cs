// Keeps the hand of card views in step with the deck: a card entering the hand takes a pooled view, a card
// leaving returns it. It orders views to match the hand, squeezes spacing so up to ten cards fit the hand width,
// dims cards that cannot be played right now, and wires explicit left/right navigation with End Turn at the end.
// Clicking a card plays it at the first living enemy (auto-target); manual targeting will replace that call.
using System.Collections.Generic;
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
        private readonly HorizontalLayoutGroup _layout;
        private readonly CanvasGroup _group;
        private readonly Button _endTurn;
        private CombatHudStyleSO _style;
        private CombatFlow _flow;

        public HandPresenter(CardView template, RectTransform handArea, HorizontalLayoutGroup layout, CanvasGroup group, Button endTurn)
        {
            _pool = new ComponentPool<CardView>(template, handArea, 12, 32);
            _layout = layout;
            _group = group;
            _endTurn = endTurn;
        }

        public void SetStyle(CombatHudStyleSO style)
        {
            _style = style;
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
            CombatDeck deck = flow.Model.Deck;

            _pool.Prewarm(deck.MaxHandSize);
            deck.CardMoved += HandleCardMoved;

            for (int i = 0; i < deck.Hand.Count; i++)
            {
                AddView(deck.Hand[i]);
            }

            Refresh();
        }

        public void Detach()
        {
            if (_flow == null)
            {
                return;
            }

            _flow.Model.Deck.CardMoved -= HandleCardMoved;
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                ReleaseAt(i);
            }

            _flow = null;
        }

        public void Dispose()
        {
            Detach();
            _pool.Dispose();
        }

        public void Refresh()
        {
            if (_flow == null || _style == null)
            {
                return;
            }

            RefreshPlayable();
            RefreshOrderAndSpacing();
            RefreshNavigation();
        }

        public void RefreshPlayable()
        {
            if (_flow == null || _style == null)
            {
                return;
            }

            bool playerTurn = _flow.IsPlayerTurn;
            _group.interactable = playerTurn;
            _group.blocksRaycasts = playerTurn;
            _group.alpha = playerTurn ? 1f : _style.HandInactiveAlpha;

            CombatModel model = _flow.Model;
            int target = model.FirstLivingEnemyIndex();
            for (int i = 0; i < _views.Count; i++)
            {
                CardView view = _views[i];
                bool playable = playerTurn && model.CheckPlay(view.CardId, target) == CardPlayCheck.Playable;
                view.SetPlayable(playable, _style.UnplayableAlpha);
            }
        }

        public void SelectFirst()
        {
            Select(0);
        }

        private void HandleCardMoved(CardInstance card, CardPile from, CardPile to)
        {
            CardPile hand = _flow.Model.Deck.Hand;
            if (to == hand)
            {
                AddView(card);
                Refresh();
            }
            else if (from == hand)
            {
                RemoveView(card);
            }
        }

        private void AddView(CardInstance card)
        {
            CardView view = _pool.Get();
            if (_style != null)
            {
                view.ApplyStyle(_style);
            }

            view.Bind(card);
            view.Clicked += HandleCardClicked;

            int index = _flow.Model.Deck.Hand.IndexOf(card.Id);
            if (index < 0 || index > _views.Count)
            {
                index = _views.Count;
            }

            _views.Insert(index, view);
        }

        private void RemoveView(CardInstance card)
        {
            int index = IndexOfView(card.Id);
            if (index < 0)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            bool wasSelected = eventSystem != null && eventSystem.currentSelectedGameObject == _views[index].gameObject;

            ReleaseAt(index);
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
            _pool.Release(view);
        }

        private void HandleCardClicked(CardView view)
        {
            if (_flow == null || !_flow.IsPlayerTurn)
            {
                return;
            }

            CombatModel model = _flow.Model;
            if (!model.Deck.TryGetFromHand(view.CardId, out CardInstance card))
            {
                return;
            }

            int target = card.RequiresChosenTarget ? model.FirstLivingEnemyIndex() : -1;
            _flow.PlayCard(card.Id, target);
        }

        private void RefreshOrderAndSpacing()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].transform.SetSiblingIndex(i);
            }

            int count = _views.Count;
            float spacing = _style.CardSpacing;
            if (count > 1)
            {
                float fitted = (_style.HandMaxWidth - count * _style.CardSize.x) / (count - 1);
                spacing = Mathf.Min(spacing, fitted);
            }

            _layout.spacing = spacing;
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
                    selectOnDown = _endTurn,
                };
                _views[i].Button.navigation = navigation;
            }

            Selectable lastCard = count > 0 ? _views[count - 1].Button : null;
            _endTurn.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = lastCard,
                selectOnUp = lastCard,
            };
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
