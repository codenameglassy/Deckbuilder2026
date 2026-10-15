// Owns every card instance in a fight and the five piles they move between: draw, hand, in play, discard, exhaust.
// Drawing takes from the top of the draw pile, reshuffles the discard pile in when the draw pile runs out, and
// stops at the hand limit. A played card waits in the in-play pile while its effects resolve, so a draw effect
// can never shuffle it back into the deck. CardMoved fires for every single move; a reshuffle fires Reshuffled once.
using System;
using JourneyToLanka.Combat.Cards;

namespace JourneyToLanka.Combat.Model
{
    public sealed class CombatDeck
    {
        private const int PileCapacity = 64;

        private readonly CombatRandom _random;
        private int _nextInstanceId = 1;

        public CombatDeck(CombatRandom random, int maxHandSize)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (maxHandSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHandSize), maxHandSize, "The hand must hold at least one card.");
            }

            _random = random;
            MaxHandSize = maxHandSize;
            DrawPile = new CardPile("Draw", PileCapacity);
            Hand = new CardPile("Hand", maxHandSize);
            InPlay = new CardPile("InPlay", 4);
            DiscardPile = new CardPile("Discard", PileCapacity);
            ExhaustPile = new CardPile("Exhaust", PileCapacity);
        }

        // Card, pile it left (null for a newly created card), pile it entered.
        public event Action<CardInstance, CardPile, CardPile> CardMoved;

        public event Action Reshuffled;

        public int MaxHandSize { get; }

        public CardPile DrawPile { get; }

        public CardPile Hand { get; }

        public CardPile InPlay { get; }

        public CardPile DiscardPile { get; }

        public CardPile ExhaustPile { get; }

        public int TotalCount => DrawPile.Count + Hand.Count + InPlay.Count + DiscardPile.Count + ExhaustPile.Count;

        public CardInstance AddToDrawPile(CardDefinitionSO definition)
        {
            var card = new CardInstance(_nextInstanceId++, definition);
            DrawPile.Add(card);
            CardMoved?.Invoke(card, null, DrawPile);
            return card;
        }

        public void AddStartingDeck(StartingDeckSO deck)
        {
            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            var entries = deck.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeckEntry entry = entries[i];
                if (entry.Card == null)
                {
                    continue;
                }

                for (int copy = 0; copy < entry.Count; copy++)
                {
                    AddToDrawPile(entry.Card);
                }
            }
        }

        public void ShuffleDrawPile()
        {
            DrawPile.Shuffle(_random);
        }

        public int Draw(int count)
        {
            int drawn = 0;
            while (drawn < count && Hand.Count < MaxHandSize)
            {
                if (DrawPile.IsEmpty)
                {
                    if (DiscardPile.IsEmpty)
                    {
                        break;
                    }

                    ReshuffleDiscardIntoDraw();
                }

                CardInstance card = DrawPile.RemoveTop();
                Hand.Add(card);
                drawn++;
                CardMoved?.Invoke(card, DrawPile, Hand);
            }

            return drawn;
        }

        public bool TryGetFromHand(int instanceId, out CardInstance card)
        {
            int index = Hand.IndexOf(instanceId);
            card = index >= 0 ? Hand[index] : null;
            return card != null;
        }

        public void BeginPlay(CardInstance card)
        {
            Move(card, Hand, InPlay);
        }

        public void FinishPlay(CardInstance card)
        {
            Move(card, InPlay, DiscardPile);
        }

        public void ExhaustFromPlay(CardInstance card)
        {
            Move(card, InPlay, ExhaustPile);
        }

        public void DiscardFromHand(CardInstance card)
        {
            Move(card, Hand, DiscardPile);
        }

        public void DiscardHand()
        {
            while (!Hand.IsEmpty)
            {
                CardInstance card = Hand.RemoveTop();
                DiscardPile.Add(card);
                CardMoved?.Invoke(card, Hand, DiscardPile);
            }
        }

        private void ReshuffleDiscardIntoDraw()
        {
            while (!DiscardPile.IsEmpty)
            {
                DrawPile.Add(DiscardPile.RemoveTop());
            }

            DrawPile.Shuffle(_random);
            Reshuffled?.Invoke();
        }

        private void Move(CardInstance card, CardPile from, CardPile to)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!from.Remove(card))
            {
                throw new InvalidOperationException($"Card {card.Id} ({card.Definition.name}) is not in the {from.DebugName} pile.");
            }

            to.Add(card);
            CardMoved?.Invoke(card, from, to);
        }
    }
}
