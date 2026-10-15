// EditMode tests for CombatDeck: instance creation, drawing from the top, reshuffling the discard pile in,
// the hand limit, the play flow through the in-play pile, and seeded shuffles.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatDeckTests
    {
        private CombatTestAssets _assets;
        private CardDefinitionSO _strike;
        private CardDefinitionSO _guard;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _strike = _assets.Card(1, _assets.Damage(6));
            _guard = _assets.Card(1, _assets.Block(5));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void AddStartingDeck_CreatesUniqueInstancesInTheDrawPile()
        {
            var deck = new CombatDeck(new CombatRandom(1), 10);
            deck.AddStartingDeck(_assets.Deck(new[] { _strike, _guard }, new[] { 5, 5 }));

            Assert.AreEqual(10, deck.DrawPile.Count);
            var ids = new HashSet<int>();
            for (int i = 0; i < deck.DrawPile.Count; i++)
            {
                Assert.IsTrue(ids.Add(deck.DrawPile[i].Id), "Duplicate instance id.");
            }
        }

        [Test]
        public void Draw_TakesFromTheTopIntoTheHand()
        {
            var deck = new CombatDeck(new CombatRandom(1), 10);
            deck.AddToDrawPile(_strike);
            CardInstance second = deck.AddToDrawPile(_guard);
            CardInstance top = deck.AddToDrawPile(_strike);
            var moves = new List<string>();
            deck.CardMoved += (card, from, to) => moves.Add(card.Id + ":" + from.DebugName + ">" + to.DebugName);

            int drawn = deck.Draw(2);

            Assert.AreEqual(2, drawn);
            Assert.AreSame(top, deck.Hand[0]);
            Assert.AreSame(second, deck.Hand[1]);
            Assert.AreEqual(1, deck.DrawPile.Count);
            CollectionAssert.AreEqual(new[] { top.Id + ":Draw>Hand", second.Id + ":Draw>Hand" }, moves);
        }

        [Test]
        public void Draw_WhenDrawPileIsEmpty_ReshufflesTheDiscardPileOnce()
        {
            var deck = new CombatDeck(new CombatRandom(1), 10);
            deck.AddToDrawPile(_strike);
            deck.AddToDrawPile(_guard);
            deck.AddToDrawPile(_strike);
            deck.Draw(3);
            deck.DiscardHand();
            int reshuffles = 0;
            deck.Reshuffled += () => reshuffles++;

            int drawn = deck.Draw(2);

            Assert.AreEqual(2, drawn);
            Assert.AreEqual(1, reshuffles);
            Assert.AreEqual(0, deck.DiscardPile.Count);
            Assert.AreEqual(1, deck.DrawPile.Count);
            Assert.AreEqual(3, deck.TotalCount);
        }

        [Test]
        public void Draw_StopsAtTheHandLimit()
        {
            var deck = new CombatDeck(new CombatRandom(1), 3);
            for (int i = 0; i < 5; i++)
            {
                deck.AddToDrawPile(_strike);
            }

            Assert.AreEqual(3, deck.Draw(5));
            Assert.AreEqual(3, deck.Hand.Count);
            Assert.AreEqual(2, deck.DrawPile.Count);
            Assert.AreEqual(0, deck.Draw(1));
        }

        [Test]
        public void Draw_StopsWhenNoCardsAreLeft()
        {
            var deck = new CombatDeck(new CombatRandom(1), 10);
            deck.AddToDrawPile(_strike);
            deck.AddToDrawPile(_guard);

            Assert.AreEqual(2, deck.Draw(5));
            Assert.AreEqual(0, deck.Draw(1));
        }

        [Test]
        public void PlayFlow_MovesThroughInPlayToDiscardOrExhaust()
        {
            var deck = new CombatDeck(new CombatRandom(1), 10);
            CardInstance played = deck.AddToDrawPile(_strike);
            CardInstance exhausted = deck.AddToDrawPile(_guard);
            deck.Draw(2);

            Assert.IsTrue(deck.TryGetFromHand(played.Id, out CardInstance found));
            Assert.AreSame(played, found);
            Assert.IsFalse(deck.TryGetFromHand(999, out _));

            deck.BeginPlay(played);
            Assert.IsTrue(deck.InPlay.Contains(played));
            Assert.IsFalse(deck.Hand.Contains(played));

            deck.FinishPlay(played);
            Assert.IsTrue(deck.DiscardPile.Contains(played));

            deck.BeginPlay(exhausted);
            deck.ExhaustFromPlay(exhausted);
            Assert.IsTrue(deck.ExhaustPile.Contains(exhausted));
            Assert.AreEqual(0, deck.InPlay.Count);
            Assert.AreEqual(2, deck.TotalCount);
        }

        [Test]
        public void BeginPlay_WithCardNotInHand_Throws()
        {
            var deck = new CombatDeck(new CombatRandom(1), 10);
            CardInstance card = deck.AddToDrawPile(_strike);

            Assert.Throws<InvalidOperationException>(() => deck.BeginPlay(card));
            Assert.IsTrue(deck.DrawPile.Contains(card));
        }

        [Test]
        public void ShuffleDrawPile_SameSeed_GivesSameOrder()
        {
            StartingDeckSO starting = _assets.Deck(new[] { _strike, _guard }, new[] { 5, 5 });
            var first = new CombatDeck(new CombatRandom(42), 10);
            var second = new CombatDeck(new CombatRandom(42), 10);
            first.AddStartingDeck(starting);
            second.AddStartingDeck(starting);

            first.ShuffleDrawPile();
            second.ShuffleDrawPile();

            for (int i = 0; i < first.DrawPile.Count; i++)
            {
                Assert.AreEqual(first.DrawPile[i].Id, second.DrawPile[i].Id, $"Position {i} differs.");
            }
        }
    }
}
