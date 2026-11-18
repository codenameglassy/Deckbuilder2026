// EditMode tests for the status cards' building blocks, against the reference build: Exhausts sends a played card to
// the exhaust pile, retained Kavach survives one turn start, healing stops at maximum health, Brahmastra spends all
// Dharma before a hit that skips every modifier but still meets Kavach (in play and in the preview, which spends
// nothing real), Piercing Vow's bonus needs 3 Dharma, statuses applied before a hit raise it, and every status an
// effect applies or spends is announced on the fighter while statuses wearing off are not.
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class StatusCardTests
    {
        private const int Seed = 2026;

        private CombatTestAssets _assets;
        private CombatRulesSO _rules;
        private StatusDefinitionSO _focus;
        private StatusDefinitionSO _bhaya;
        private StatusDefinitionSO _dharma;
        private EnemyDefinitionSO _dummy;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _rules = _assets.Rules(3, 5, 10);
            _focus = _assets.Status("Focus", 0, false, _assets.Create<DurationDecaySO>(), _assets.FlatOutgoing(0));
            _bhaya = _assets.Status("Bhaya", 3, true, _assets.Create<TurnTimerDecaySO>(), _assets.IncomingScale(20, 125));
            _dharma = _assets.Status("Dharma", 0, false, null, null);
            _dummy = _assets.Enemy(60, 60, 0, _assets.Move(_assets.Block(1)));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void ExhaustingCard_GoesToTheExhaustPile()
        {
            CombatModel model = CreateModel(_assets.ExhaustingCard(1, _assets.Heal(8)), 70);
            model.StartPlayerTurn();
            int cardId = model.Deck.Hand[0].Id;

            Assert.IsTrue(model.TryPlayCard(cardId, -1));

            Assert.AreEqual(1, model.Deck.ExhaustPile.Count);
            Assert.AreEqual(0, model.Deck.DiscardPile.Count);
            Assert.AreEqual(78, model.Player.Health);
        }

        [Test]
        public void Heal_StopsAtMaximumHealth_AndReportsWhatItRestored()
        {
            var fighter = new Combatant("fighter", 78, 74);
            var restored = new List<int>();
            fighter.Healed += (_, amount) => restored.Add(amount);

            Assert.AreEqual(4, fighter.Heal(8));
            Assert.AreEqual(0, fighter.Heal(8));

            Assert.AreEqual(78, fighter.Health);
            CollectionAssert.AreEqual(new[] { 4 }, restored);
        }

        [Test]
        public void RetainedKavach_SurvivesOneTurnStart_ThenClears()
        {
            CombatModel model = CreateModel(_assets.Card(1, _assets.RetainedBlock(11)), 78);
            model.StartPlayerTurn();
            model.TryPlayCard(model.Deck.Hand[0].Id, -1);
            model.EndPlayerTurn();

            model.StartPlayerTurn();
            Assert.AreEqual(11, model.Player.Block, "Retained Kavach was cleared.");

            model.EndPlayerTurn();
            model.StartPlayerTurn();
            Assert.AreEqual(0, model.Player.Block, "Retained Kavach lasted more than one turn.");
        }

        [Test]
        public void Brahmastra_SpendsAllDharma_ThenHitsWithoutModifiers_ThroughKavach()
        {
            CardDefinitionSO brahmastra = _assets.ExhaustingCard(3, _assets.SpendStatusDamage(_dharma, 10, 7));
            CombatModel model = CreateModel(brahmastra, 78);
            Combatant enemy = model.Enemies[0].Fighter;
            model.Resolver.ApplyStatus(model.Player, _dharma, 3, 0);
            model.Resolver.ApplyStatus(model.Player, _focus, 5, 1);
            model.Resolver.ApplyStatus(enemy, _bhaya, 2, 0);
            enemy.GainBlock(5);
            model.StartPlayerTurn();
            CardInstance card = model.Deck.Hand[0];
            var outcome = new CombatOutcomePreview();

            model.PreviewPlay(card, 0, outcome);
            outcome.TryGet(enemy, out CombatantOutcome previewed);
            Assert.AreEqual(26, previewed.HealthLost, "31 damage, 5 of it into Kavach.");
            Assert.AreEqual(3, model.Player.Statuses.GetAmount(_dharma), "The preview spent real Dharma.");

            model.TryPlayCard(card.Id, 0);
            Assert.AreEqual(34, enemy.Health);
            Assert.AreEqual(0, enemy.Block);
            Assert.AreEqual(0, model.Player.Statuses.GetAmount(_dharma));
            Assert.AreEqual(1, model.Deck.ExhaustPile.Count);
        }

        [Test]
        public void Brahmastra_WithNoDharma_DealsItsBase()
        {
            CombatModel model = CreateModel(_assets.Card(3, _assets.SpendStatusDamage(_dharma, 10, 7)), 78);
            model.StartPlayerTurn();

            model.TryPlayCard(model.Deck.Hand[0].Id, 0);

            Assert.AreEqual(50, model.Enemies[0].Fighter.Health);
        }

        [Test]
        public void PiercingVow_AddsItsBonus_OnlyFromThreeDharma()
        {
            CardDefinitionSO pierce = _assets.Card(1, _assets.DamageWithStatusBonus(5, _dharma, 3, 6));
            CombatModel model = CreateModel(pierce, 78);
            Combatant enemy = model.Enemies[0].Fighter;
            model.StartPlayerTurn();
            model.Resolver.ApplyStatus(model.Player, _dharma, 2, 0);

            model.TryPlayCard(model.Deck.Hand[0].Id, 0);
            Assert.AreEqual(55, enemy.Health, "The bonus applied below 3 Dharma.");

            model.Resolver.ApplyStatus(model.Player, _dharma, 1, 0);
            model.TryPlayCard(model.Deck.Hand[0].Id, 0);
            Assert.AreEqual(44, enemy.Health, "The bonus did not apply at 3 Dharma.");
            Assert.AreEqual(3, model.Player.Statuses.GetAmount(_dharma), "Piercing Vow spent Dharma.");
        }

        [Test]
        public void PiercingVow_CardValues_AreDamageHitsThresholdAndBonus()
        {
            CardDefinitionSO pierce = _assets.Card(1, _assets.DamageWithStatusBonus(5, _dharma, 3, 6));
            var values = new List<CardValue>();

            pierce.CollectCardValues(default, values);

            Assert.AreEqual(4, values.Count);
            Assert.AreEqual(5, values[0].Current);
            Assert.AreEqual(1, values[1].Current);
            Assert.AreEqual(3, values[2].Current);
            Assert.AreEqual(6, values[3].Current);
        }

        [Test]
        public void TerrorOfTheBow_BhayaBeforeTheHit_RaisesIt()
        {
            CardDefinitionSO terror = _assets.Card(1, _assets.ApplyStatus(_bhaya, 2, 0, true), _assets.Damage(4));
            CombatModel model = CreateModel(terror, 78);
            model.StartPlayerTurn();

            model.TryPlayCard(model.Deck.Hand[0].Id, 0);

            Assert.AreEqual(55, model.Enemies[0].Fighter.Health);
            Assert.AreEqual(2, model.Enemies[0].Fighter.Statuses.GetAmount(_bhaya));
        }

        [Test]
        public void StatusApplied_AnnouncesAppliedAndSpentStatuses_ButNotDecay()
        {
            CombatModel model = CreateModel(_assets.Card(3, _assets.SpendStatusDamage(_dharma, 10, 7)), 78);
            var announced = new List<int>();
            model.Player.StatusApplied += (_, status, amount) => announced.Add(amount);
            model.Resolver.ApplyStatus(model.Player, _dharma, 3, 0);
            model.Resolver.ApplyStatus(model.Player, _focus, 2, 1);
            model.StartPlayerTurn();

            model.TryPlayCard(model.Deck.Hand[0].Id, 0);
            model.EndPlayerTurn();

            CollectionAssert.AreEqual(new[] { 3, 2, -3 }, announced);
            Assert.AreEqual(0, model.Player.Statuses.Count);
        }

        private CombatModel CreateModel(CardDefinitionSO card, int playerHealth)
        {
            StartingDeckSO deck = _assets.Deck(new[] { card }, new[] { 10 });
            PlayerDefinitionSO player = _assets.Player(78, deck);
            return new CombatModel(_rules, player, playerHealth, _assets.Encounter(_dummy), new CombatRandom(Seed));
        }
    }
}
