// EditMode tests for building a fight from a PlayerSetup: carried health, a run's own deck list, empty slots in
// that list, and the starting deck expansion used by FromDefinition.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class PlayerSetupTests
    {
        private CombatTestAssets _assets;
        private CardDefinitionSO _strike;
        private CardDefinitionSO _guard;
        private CombatEncounterSO _encounter;
        private CombatRulesSO _rules;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _strike = _assets.Card(1, _assets.Damage(6));
            _guard = _assets.Card(1, _assets.Block(5));
            _encounter = _assets.Encounter(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7))));
            _rules = _assets.Rules(3, 5, 10);
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Setup_CarriesHealthAndUsesTheGivenDeck()
        {
            var deck = new List<CardDefinitionSO> { _strike, _strike, null, _guard };
            var setup = new PlayerSetup("rama", 78, 40, deck);

            var model = new CombatModel(_rules, setup, _encounter, new CombatRandom(1));

            Assert.AreEqual(40, model.Player.Health);
            Assert.AreEqual(78, model.Player.MaxHealth);
            Assert.AreEqual(3, model.Deck.DrawPile.Count);
        }

        [Test]
        public void Setup_WithoutDeck_Throws()
        {
            var setup = new PlayerSetup("rama", 78, 78, null);

            Assert.Throws<ArgumentException>(() => new CombatModel(_rules, setup, _encounter, new CombatRandom(1)));
        }

        [Test]
        public void FromDefinition_ExpandsTheStartingDeck()
        {
            StartingDeckSO starting = _assets.Deck(new[] { _strike, _guard }, new[] { 5, 4 });
            PlayerDefinitionSO player = _assets.Player(78, starting);

            PlayerSetup setup = PlayerSetup.FromDefinition(player, 60);

            Assert.AreEqual(9, setup.Deck.Count);
            Assert.AreSame(_strike, setup.Deck[0]);
            Assert.AreSame(_guard, setup.Deck[8]);
            Assert.AreEqual(60, setup.Health);
            Assert.AreEqual(78, setup.MaxHealth);
        }
    }
}
