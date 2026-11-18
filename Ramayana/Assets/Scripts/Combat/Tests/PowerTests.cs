// EditMode tests for powers and the remaining chapter one cards, against the reference build: Blessing of Agni adds 2
// to Rama's hits before Moha, Vow of the Exile gains 1 Dharma at each turn start after Kavach clears, Devotion turns
// every Dharma gained into 2 Kavach (in play, at turn start through the Exile, and in a card's preview), powers do not
// stack past their cap, and Vayavyastra strips Kavach before its hit, in play and in the preview.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class PowerTests
    {
        private const int Seed = 2026;

        private CombatTestAssets _assets;
        private CombatRulesSO _rules;
        private StatusDefinitionSO _moha;
        private StatusDefinitionSO _dharma;
        private StatusDefinitionSO _agni;
        private StatusDefinitionSO _exile;
        private StatusDefinitionSO _devotion;
        private EnemyDefinitionSO _dummy;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _rules = _assets.Rules(3, 5, 10);
            _moha = _assets.Status("Moha", 3, true, _assets.Create<TurnTimerDecaySO>(), _assets.OutgoingScale(10, 75));
            _dharma = _assets.Status("Dharma", 0, false, null, null);
            _agni = _assets.Power("Agni", 2, _assets.FlatOutgoing(0), null);
            _exile = _assets.Power("Exile", 1, null, _assets.TurnStartGain(_dharma, 1));
            _devotion = _assets.Power("Devotion", 1, null, _assets.GainedBlock(_dharma, 2));
            _dummy = _assets.Enemy(60, 60, 0, _assets.Move(_assets.Block(1)));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void BlessingOfAgni_AddsTwoPerHit_BeforeMoha_AndDoesNotStack()
        {
            CombatModel model = CreateModel(_assets.Card(1, _assets.Damage(6)));
            Combatant enemy = model.Enemies[0].Fighter;
            model.Resolver.ApplyStatus(model.Player, _agni, 2, 0);
            model.Resolver.ApplyStatus(model.Player, _agni, 2, 0);

            Assert.AreEqual(2, model.Player.Statuses.GetAmount(_agni));
            Assert.AreEqual(8, model.Resolver.CalculateDamage(model.Player, enemy, 6));

            model.Resolver.ApplyStatus(model.Player, _moha, 1, 0);
            Assert.AreEqual(6, model.Resolver.CalculateDamage(model.Player, enemy, 6), "(6 + 2) x 0.75 rounds down to 6.");
            Assert.AreEqual(5, model.Resolver.CalculateDamage(enemy, model.Player, 5), "Agni boosted the enemy.");
        }

        [Test]
        public void VowOfTheExile_GainsOneDharmaAtEachTurnStart()
        {
            CombatModel model = CreateModel(_assets.ExhaustingCard(2, _assets.ApplyStatus(_exile, 1, 0, false)));
            model.StartPlayerTurn();
            model.TryPlayCard(model.Deck.Hand[0].Id, -1);
            Assert.AreEqual(0, model.Player.Statuses.GetAmount(_dharma), "Dharma came before the next turn.");
            Assert.AreEqual(1, model.Deck.ExhaustPile.Count);

            model.EndPlayerTurn();
            model.StartPlayerTurn();
            model.EndPlayerTurn();
            model.StartPlayerTurn();

            Assert.AreEqual(2, model.Player.Statuses.GetAmount(_dharma));
            Assert.AreEqual(1, model.Player.Statuses.GetAmount(_exile), "The power wore off.");
        }

        [Test]
        public void Devotion_TurnsEveryDharmaGainedIntoTwoKavach_ButNotSpending()
        {
            CombatModel model = CreateModel(_assets.Card(0, _assets.ApplyStatus(_dharma, 3, 0, false)));
            model.Resolver.ApplyStatus(model.Player, _devotion, 1, 0);
            model.StartPlayerTurn();

            model.TryPlayCard(model.Deck.Hand[0].Id, -1);
            Assert.AreEqual(6, model.Player.Block);

            model.Resolver.ApplyStatus(model.Player, _dharma, -3, 0);
            Assert.AreEqual(6, model.Player.Block, "Spending Dharma gained Kavach.");
        }

        [Test]
        public void ExileWithDevotion_GivesKavachAfterTheTurnStartClear()
        {
            CombatModel model = CreateModel(_assets.Card(1, _assets.Block(5)));
            model.Resolver.ApplyStatus(model.Player, _exile, 1, 0);
            model.Resolver.ApplyStatus(model.Player, _devotion, 1, 0);
            model.StartPlayerTurn();
            Assert.AreEqual(1, model.Player.Statuses.GetAmount(_dharma));
            Assert.AreEqual(2, model.Player.Block);

            model.TryPlayCard(model.Deck.Hand[0].Id, -1);
            model.EndPlayerTurn();
            model.StartPlayerTurn();

            Assert.AreEqual(2, model.Player.Statuses.GetAmount(_dharma));
            Assert.AreEqual(2, model.Player.Block, "Last turn's Kavach survived, or the Exile's Kavach was cleared.");
        }

        [Test]
        public void Devotion_ShowsInTheCardPreview_WithoutChangingRealState()
        {
            CombatModel model = CreateModel(_assets.Card(0, _assets.ApplyStatus(_dharma, 2, 0, false)));
            model.Resolver.ApplyStatus(model.Player, _devotion, 1, 0);
            model.StartPlayerTurn();
            var outcome = new CombatOutcomePreview();

            model.PreviewPlay(model.Deck.Hand[0], -1, outcome);

            Assert.IsTrue(outcome.TryGet(model.Player, out CombatantOutcome previewed));
            Assert.AreEqual(4, previewed.BlockGained);
            Assert.AreEqual(0, model.Player.Block);
            Assert.AreEqual(0, model.Player.Statuses.GetAmount(_dharma));
        }

        [Test]
        public void Vayavyastra_StripsKavachBeforeItsHit_InPreviewAndInPlay()
        {
            CardDefinitionSO vayu = _assets.Card(1, _assets.RemoveBlock(), _assets.Damage(8));
            CombatModel model = CreateModel(vayu);
            Combatant enemy = model.Enemies[0].Fighter;
            enemy.GainBlock(10);
            model.StartPlayerTurn();
            CardInstance card = model.Deck.Hand[0];
            var outcome = new CombatOutcomePreview();

            model.PreviewPlay(card, 0, outcome);
            outcome.TryGet(enemy, out CombatantOutcome previewed);
            Assert.AreEqual(0, previewed.EndBlock);
            Assert.AreEqual(8, previewed.HealthLost);
            Assert.AreEqual(10, enemy.Block, "The preview stripped real Kavach.");

            model.TryPlayCard(card.Id, 0);
            Assert.AreEqual(0, enemy.Block);
            Assert.AreEqual(52, enemy.Health);
        }

        private CombatModel CreateModel(CardDefinitionSO card)
        {
            StartingDeckSO deck = _assets.Deck(new[] { card }, new[] { 10 });
            PlayerDefinitionSO player = _assets.Player(78, deck);
            return new CombatModel(_rules, player, 78, _assets.Encounter(_dummy), new CombatRandom(Seed));
        }
    }
}
