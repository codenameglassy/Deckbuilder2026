// EditMode tests for CombatModel: building a fight from data, the turn rules (Prana refill, draw five, Kavach kept on
// turn one and cleared from turn two, hand discarded at end of turn), card play validation and resolution,
// enemy turns, multi-enemy targeting, win and loss flags, and seeded determinism.
using System;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatModelTests
    {
        private const int Seed = 2026;

        private CombatTestAssets _assets;
        private CombatRulesSO _rules;
        private CardDefinitionSO _strike;
        private CardDefinitionSO _guard;
        private EnemyDefinitionSO _scout;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _rules = _assets.Rules(3, 5, 10);
            _strike = _assets.Card(1, _assets.Damage(6));
            _guard = _assets.Card(1, _assets.Block(5));
            _scout = _assets.Enemy(30, 34, 0,
                _assets.Move(_assets.Damage(7)),
                _assets.Move(_assets.Damage(4, 2)),
                _assets.Move(_assets.Block(6)));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Constructor_BuildsTheFightFromData_SkippingEmptyEnemySlots()
        {
            CombatModel model = CreateModel(SliceDeck(), 78, _scout, null, _scout);

            Assert.AreEqual(78, model.Player.Health);
            Assert.AreEqual(2, model.Enemies.Count);
            Assert.AreEqual(2, model.EnemyFighters.Count);
            Assert.AreEqual(0, model.Enemies[0].SlotIndex);
            Assert.AreEqual(1, model.Enemies[1].SlotIndex);
            Assert.That(model.Enemies[0].Fighter.Health, Is.InRange(30, 34));
            Assert.AreEqual(10, model.Deck.DrawPile.Count);
            Assert.AreEqual(0, model.TurnNumber);
            Assert.IsFalse(model.IsOver);
        }

        [Test]
        public void Constructor_WithNoEnemies_Throws()
        {
            Assert.Throws<ArgumentException>(() => CreateModel(SliceDeck(), 78, new EnemyDefinitionSO[] { null }));
        }

        [Test]
        public void StartPlayerTurn_RefillsPranaAndDrawsFive()
        {
            CombatModel model = CreateModel(SliceDeck(), 78, _scout);
            int startedTurn = 0;
            model.PlayerTurnStarted += turn => startedTurn = turn;

            model.StartPlayerTurn();

            Assert.AreEqual(1, model.TurnNumber);
            Assert.AreEqual(1, startedTurn);
            Assert.AreEqual(3, model.Prana.Current);
            Assert.AreEqual(5, model.Deck.Hand.Count);
            Assert.AreEqual(5, model.Deck.DrawPile.Count);
        }

        [Test]
        public void Kavach_IsKeptOnTurnOne_AndClearedFromTurnTwo()
        {
            CombatModel model = CreateModel(SliceDeck(), 78, _scout);
            model.Player.GainBlock(4);

            model.StartPlayerTurn();
            Assert.AreEqual(4, model.Player.Block);

            model.EndPlayerTurn();
            model.StartPlayerTurn();
            Assert.AreEqual(0, model.Player.Block);
        }

        [Test]
        public void EndPlayerTurn_DiscardsTheHand()
        {
            CombatModel model = CreateModel(SliceDeck(), 78, _scout);
            model.StartPlayerTurn();

            model.EndPlayerTurn();

            Assert.AreEqual(0, model.Deck.Hand.Count);
            Assert.AreEqual(5, model.Deck.DiscardPile.Count);
        }

        [Test]
        public void PlayStrike_DamagesTarget_SpendsPrana_AndDiscards()
        {
            CombatModel model = CreateModel(OnlyDeck(_strike), 78, _scout);
            model.StartPlayerTurn();
            CardInstance card = model.Deck.Hand[0];
            int enemyHealth = model.Enemies[0].Fighter.Health;
            ICombatant playedTarget = null;
            model.CardPlayed += (_, target) => playedTarget = target;

            Assert.IsTrue(model.TryPlayCard(card.Id, 0));

            Assert.AreEqual(enemyHealth - 6, model.Enemies[0].Fighter.Health);
            Assert.AreEqual(2, model.Prana.Current);
            Assert.AreEqual(4, model.Deck.Hand.Count);
            Assert.IsTrue(model.Deck.DiscardPile.Contains(card));
            Assert.AreEqual(0, model.Deck.InPlay.Count);
            Assert.AreSame(model.EnemyFighters[0], playedTarget);
        }

        [Test]
        public void PlayGuard_GivesKavach_WithoutATarget()
        {
            CombatModel model = CreateModel(OnlyDeck(_guard), 78, _scout);
            model.StartPlayerTurn();
            CardInstance card = model.Deck.Hand[0];

            Assert.AreEqual(CardPlayCheck.Playable, model.CheckPlay(card.Id, -1));
            Assert.IsTrue(model.TryPlayCard(card.Id, -1));
            Assert.AreEqual(5, model.Player.Block);
        }

        [Test]
        public void CheckPlay_RejectsBadInput_WithoutChangingState()
        {
            CombatModel model = CreateModel(OnlyDeck(_strike), 78, _scout);
            model.StartPlayerTurn();
            int id = model.Deck.Hand[0].Id;

            Assert.AreEqual(CardPlayCheck.NotInHand, model.CheckPlay(999, 0));
            Assert.AreEqual(CardPlayCheck.InvalidTarget, model.CheckPlay(id, -1));
            Assert.AreEqual(CardPlayCheck.InvalidTarget, model.CheckPlay(id, 5));
            Assert.IsFalse(model.TryPlayCard(id, 5));
            Assert.AreEqual(5, model.Deck.Hand.Count);
            Assert.AreEqual(3, model.Prana.Current);

            model.TryPlayCard(model.Deck.Hand[0].Id, 0);
            model.TryPlayCard(model.Deck.Hand[0].Id, 0);
            model.TryPlayCard(model.Deck.Hand[0].Id, 0);
            Assert.AreEqual(CardPlayCheck.NotEnoughPrana, model.CheckPlay(model.Deck.Hand[0].Id, 0));
        }

        [Test]
        public void TargetIndex_HitsOnlyThatEnemy_AndFallenEnemiesAreInvalidTargets()
        {
            EnemyDefinitionSO weak = _assets.Enemy(6, 6, 0, _assets.Move(_assets.Damage(7)));
            CombatModel model = CreateModel(OnlyDeck(_strike), 78, weak, weak);
            model.StartPlayerTurn();

            model.TryPlayCard(model.Deck.Hand[0].Id, 1);

            Assert.AreEqual(6, model.Enemies[0].Fighter.Health);
            Assert.IsFalse(model.Enemies[1].IsAlive);
            Assert.AreEqual(0, model.FirstLivingEnemyIndex());
            Assert.AreEqual(CardPlayCheck.InvalidTarget, model.CheckPlay(model.Deck.Hand[0].Id, 1));
            Assert.IsFalse(model.AreEnemiesDefeated);
        }

        [Test]
        public void KillingTheLastEnemy_EndsCombat()
        {
            EnemyDefinitionSO weak = _assets.Enemy(6, 6, 0, _assets.Move(_assets.Damage(7)));
            CombatModel model = CreateModel(OnlyDeck(_strike), 78, weak);
            model.StartPlayerTurn();

            model.TryPlayCard(model.Deck.Hand[0].Id, 0);

            Assert.IsTrue(model.AreEnemiesDefeated);
            Assert.IsTrue(model.IsOver);
            Assert.AreEqual(-1, model.FirstLivingEnemyIndex());
            Assert.AreEqual(CardPlayCheck.CombatOver, model.CheckPlay(model.Deck.Hand[0].Id, 0));
        }

        [Test]
        public void EnemyTakeTurn_FollowsThePattern_AndCanDefeatThePlayer()
        {
            CombatModel model = CreateModel(SliceDeck(), 12, _scout);

            Assert.IsTrue(model.EnemyTakeTurn(0));
            Assert.AreEqual(5, model.Player.Health);

            Assert.IsTrue(model.EnemyTakeTurn(0));
            Assert.AreEqual(0, model.Player.Health);
            Assert.IsTrue(model.IsPlayerDefeated);
            Assert.IsFalse(model.EnemyTakeTurn(0));
            Assert.IsFalse(model.EnemyTakeTurn(3));
        }

        [Test]
        public void SameSeed_GivesSameOpeningHandAndEnemyHealth()
        {
            CombatModel first = CreateModel(SliceDeck(), 78, _scout, _scout);
            CombatModel second = CreateModel(SliceDeck(), 78, _scout, _scout);

            first.StartPlayerTurn();
            second.StartPlayerTurn();

            for (int i = 0; i < first.Deck.Hand.Count; i++)
            {
                Assert.AreEqual(first.Deck.Hand[i].Id, second.Deck.Hand[i].Id, $"Hand slot {i} differs.");
            }

            Assert.AreEqual(first.Enemies[0].Fighter.Health, second.Enemies[0].Fighter.Health);
            Assert.AreEqual(first.Enemies[1].Fighter.Health, second.Enemies[1].Fighter.Health);
        }

        private CombatModel CreateModel(StartingDeckSO deck, int playerHealth, params EnemyDefinitionSO[] enemies)
        {
            PlayerDefinitionSO player = _assets.Player(78, deck);
            return new CombatModel(_rules, player, playerHealth, _assets.Encounter(enemies), new CombatRandom(Seed));
        }

        private StartingDeckSO SliceDeck()
        {
            return _assets.Deck(new[] { _strike, _guard }, new[] { 5, 5 });
        }

        private StartingDeckSO OnlyDeck(CardDefinitionSO card)
        {
            return _assets.Deck(new[] { card }, new[] { 10 });
        }
    }
}
