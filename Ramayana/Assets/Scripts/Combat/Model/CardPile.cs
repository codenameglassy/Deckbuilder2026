// An ordered pile of card instances (draw, hand, in play, discard, exhaust). Read access is public so views can
// lay out cards and show counts; only CombatDeck changes a pile, so every move raises the deck's events.
// For the draw pile the last element is the top card, which keeps drawing O(1).
using System.Collections.Generic;

namespace JourneyToLanka.Combat.Model
{
    public sealed class CardPile
    {
        private readonly List<CardInstance> _cards;

        internal CardPile(string debugName, int capacity)
        {
            DebugName = debugName;
            _cards = new List<CardInstance>(capacity);
        }

        public string DebugName { get; }

        public int Count => _cards.Count;

        public bool IsEmpty => _cards.Count == 0;

        public CardInstance Top => _cards.Count > 0 ? _cards[_cards.Count - 1] : null;

        public CardInstance this[int index] => _cards[index];

        public int IndexOf(int instanceId)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Id == instanceId)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool Contains(CardInstance card)
        {
            return card != null && IndexOf(card.Id) >= 0;
        }

        internal void Add(CardInstance card)
        {
            _cards.Add(card);
        }

        internal CardInstance RemoveTop()
        {
            int last = _cards.Count - 1;
            CardInstance card = _cards[last];
            _cards.RemoveAt(last);
            return card;
        }

        internal bool Remove(CardInstance card)
        {
            int index = IndexOf(card.Id);
            if (index < 0)
            {
                return false;
            }

            _cards.RemoveAt(index);
            return true;
        }

        internal void Shuffle(CombatRandom random)
        {
            random.Shuffle(_cards);
        }

        public override string ToString()
        {
            return DebugName + " (" + _cards.Count + ")";
        }
    }
}
