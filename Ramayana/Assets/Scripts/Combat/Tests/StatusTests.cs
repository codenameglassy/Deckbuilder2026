// EditMode tests for statuses, against the reference build's rules: stacking and caps, rule order, how each decay
// counts down (round timers for Bhaya and Moha, owner turns for Focus, halving burn for Dahan, none for Dharma), the
// damage order (base plus Focus, then Moha, then Bhaya, each rounded down), statuses applied earlier in a card
// raising later hits in play and in the preview, the turn and round timing in the combat model and flow, and the
// threat panel following a status the previewed card would apply.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using NUnit.Framework;
using UnityEditor;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class StatusTests
    {
        private const int Seed = 2026;

        private CombatTestAssets _assets;
        private CombatRulesSO _rules;
        private StatusDefinitionSO _focus;
        private StatusDefinitionSO _moha;
        private StatusDefinitionSO _bhaya;
        private StatusDefinitionSO _dahan;
        private StatusDefinitionSO _dharma;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _rules = _assets.Rules(3, 5, 10);
            var turnTimer = _assets.Create<TurnTimerDecaySO>();
            _focus = _assets.Status("Focus", 0, false, _assets.Create<DurationDecaySO>(), _assets.FlatOutgoing(0));
            _moha = _assets.Status("Moha", 3, true, turnTimer, _assets.OutgoingScale(10, 75));
            _bhaya = _assets.Status("Bhaya", 3, true, turnTimer, _assets.IncomingScale(20, 125));
            _dahan = _assets.Status("Dahan", 0, true, _assets.BurnDecay(50), null);
            _dharma = _assets.Status("Dharma", 0, false, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Apply_CapsAtTheStatusMaximum()
        {
            var set = new StatusSet();

            set.Apply(_bhaya, 2, 0);
            set.Apply(_bhaya, 2, 0);

            Assert.AreEqual(3, set.GetAmount(_bhaya));
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void Apply_StacksWithoutACap_AndReachingZeroRemovesIt()
        {
            var set = new StatusSet();
            int changes = 0;
            set.Changed += _ => changes++;

            set.Apply(_dahan, 5, 0);
            set.Apply(_dahan, 4, 0);
            Assert.AreEqual(9, set.GetAmount(_dahan));

            set.Apply(_dahan, -9, 0);
            Assert.AreEqual(0, set.Count);
            Assert.AreEqual(3, changes);
        }

        [Test]
        public void Statuses_AreKeptInDamageRuleOrder()
        {
            var set = new StatusSet();

            set.Apply(_dharma, 1, 0);
            set.Apply(_bhaya, 1, 0);
            set.Apply(_moha, 1, 0);
            set.Apply(_focus, 2, 1);

            Assert.AreSame(_focus, set[0].Definition);
            Assert.AreSame(_moha, set[1].Definition);
            Assert.AreSame(_bhaya, set[2].Definition);
            Assert.AreSame(_dharma, set[3].Definition);
        }

        [Test]
        public void TurnTimer_CountsDownAtRoundEnd_NotAtTurnEnd()
        {
            var set = new StatusSet();
            set.Apply(_moha, 2, 0);

            set.TickOwnerTurnEnd();
            Assert.AreEqual(2, set.GetAmount(_moha));

            set.TickRoundEnd(default);
            Assert.AreEqual(1, set.GetAmount(_moha));

            set.TickRoundEnd(default);
            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void Duration_OneTurnFocusEndsWithTheTurn_TwoTurnFocusSurvivesOne()
        {
            var playerSet = new StatusSet();
            var enemySet = new StatusSet();
            playerSet.Apply(_focus, 3, 1);
            enemySet.Apply(_focus, 3, 2);

            playerSet.TickOwnerTurnEnd();
            enemySet.TickOwnerTurnEnd();
            enemySet.TickRoundEnd(default);

            Assert.AreEqual(0, playerSet.Count);
            Assert.AreEqual(3, enemySet.GetAmount(_focus));

            enemySet.TickOwnerTurnEnd();
            Assert.AreEqual(0, enemySet.Count);
        }

        [Test]
        public void Burn_IgnoresKavach_ThenHalvesUntilGone()
        {
            var resolver = new CombatResolver();
            var enemy = new Combatant("enemy", 30, 30);
            enemy.GainBlock(10);
            enemy.Statuses.Apply(_dahan, 8, 0);
            var context = new StatusTickContext(resolver, enemy);
            int[] expectedHealth = { 22, 18, 16, 15 };
            int[] expectedBurn = { 4, 2, 1, 0 };

            for (int round = 0; round < 4; round++)
            {
                enemy.Statuses.TickRoundEnd(context);
                Assert.AreEqual(expectedHealth[round], enemy.Health, $"Health after round {round + 1}.");
                Assert.AreEqual(expectedBurn[round], enemy.Statuses.GetAmount(_dahan), $"Burn after round {round + 1}.");
            }

            Assert.AreEqual(10, enemy.Block);
            Assert.AreEqual(0, enemy.Statuses.Count);
        }

        [Test]
        public void Burn_CanKill_AndAFallenFighterLosesItsStatuses()
        {
            var resolver = new CombatResolver();
            var enemy = new Combatant("enemy", 30, 5);
            enemy.Statuses.Apply(_dahan, 8, 0);
            enemy.Statuses.Apply(_bhaya, 2, 0);
            int deaths = 0;
            enemy.Died += _ => deaths++;

            enemy.Statuses.TickRoundEnd(new StatusTickContext(resolver, enemy));

            Assert.IsFalse(enemy.IsAlive);
            Assert.AreEqual(1, deaths);
            Assert.AreEqual(0, enemy.Statuses.Count);
        }

        [Test]
        public void Dharma_NeverDecays()
        {
            var set = new StatusSet();
            set.Apply(_dharma, 4, 0);

            for (int i = 0; i < 5; i++)
            {
                set.TickOwnerTurnEnd();
                set.TickRoundEnd(default);
            }

            Assert.AreEqual(4, set.GetAmount(_dharma));
        }

        [Test]
        public void Damage_FollowsTheReferenceOrder_FocusThenMohaThenBhaya()
        {
            var resolver = new CombatResolver();
            resolver.AddModifier(new StatusDamageModifier());
            var attacker = new Combatant("attacker", 30, 30);
            var target = new Combatant("target", 30, 30);

            attacker.Statuses.Apply(_focus, 3, 1);
            Assert.AreEqual(9, resolver.CalculateDamage(attacker, target, 6), "Focus adds per hit.");

            attacker.Statuses.Apply(_moha, 1, 0);
            Assert.AreEqual(6, resolver.CalculateDamage(attacker, target, 6), "(6 + 3) x 0.75 rounds down to 6.");

            target.Statuses.Apply(_bhaya, 3, 0);
            Assert.AreEqual(7, resolver.CalculateDamage(attacker, target, 6), "6 x 1.25 rounds down to 7.");

            Assert.AreEqual(5, resolver.CalculateDamage(target, attacker, 5), "Statuses act only on their own side.");
        }

        [Test]
        public void Model_StatusAppliedBeforeAHit_RaisesIt_InPreviewAndInPlay()
        {
            CardDefinitionSO terror = _assets.Card(1, _assets.ApplyStatus(_bhaya, 1, 0, true), _assets.Damage(8));
            CombatModel model = CreateModel(terror, _assets.Enemy(40, 40, 0, _assets.Move(_assets.Block(1))));
            model.StartPlayerTurn();
            CardInstance card = model.Deck.Hand[0];
            Combatant enemy = model.Enemies[0].Fighter;
            var outcome = new CombatOutcomePreview();

            model.PreviewPlay(card, 0, outcome);

            Assert.IsTrue(outcome.TryGet(enemy, out CombatantOutcome previewed));
            Assert.AreEqual(10, previewed.HealthLost);
            Assert.IsTrue(outcome.TryGetStatuses(enemy, out StatusSet previewStatuses));
            Assert.AreEqual(1, previewStatuses.GetAmount(_bhaya));
            Assert.AreEqual(0, enemy.Statuses.Count, "The preview changed the real enemy.");

            Assert.IsTrue(model.TryPlayCard(card.Id, 0));
            Assert.AreEqual(30, enemy.Health);
            Assert.AreEqual(1, enemy.Statuses.GetAmount(_bhaya));
        }

        [Test]
        public void Model_AStatusChange_RaisesModifiersChanged()
        {
            CardDefinitionSO focus = _assets.Card(1, _assets.ApplyStatus(_focus, 2, 1, false));
            CombatModel model = CreateModel(focus, _assets.Enemy(30, 30, 0, _assets.Move(_assets.Block(1))));
            model.StartPlayerTurn();
            int changes = 0;
            model.Resolver.ModifiersChanged += () => changes++;

            model.TryPlayCard(model.Deck.Hand[0].Id, -1);

            Assert.AreEqual(1, changes);
            Assert.AreEqual(2, model.Player.Statuses.GetAmount(_focus));
        }

        [Test]
        public void Model_PlayerFocusEndsWithHisTurn_EnemyFocusLastsThroughItsNextAction()
        {
            CardDefinitionSO focus = _assets.Card(1, _assets.ApplyStatus(_focus, 2, 1, false));
            EnemyDefinitionSO raider = _assets.Enemy(30, 30, 0,
                _assets.Move(_assets.ApplyStatus(_focus, 3, 2, false)),
                _assets.Move(_assets.Damage(5)));
            CombatModel model = CreateModel(focus, raider);
            Combatant enemy = model.Enemies[0].Fighter;
            model.StartPlayerTurn();
            model.TryPlayCard(model.Deck.Hand[0].Id, -1);

            model.EndPlayerTurn();
            Assert.AreEqual(0, model.Player.Statuses.Count, "Rama's Focus outlived his turn.");

            model.EnemyTakeTurn(0);
            model.EndRound();
            Assert.AreEqual(3, enemy.Statuses.GetAmount(_focus), "The enemy's Focus ended before it could use it.");
            Assert.AreEqual(8, model.Enemies[0].GetIntent(model.Resolver, model.Player).TotalDamage);

            model.StartPlayerTurn();
            model.EndPlayerTurn();
            model.EnemyTakeTurn(0);
            Assert.AreEqual(70, model.Player.Health);
            Assert.AreEqual(0, enemy.Statuses.Count, "The enemy's Focus outlived its next action.");
        }

        [Test]
        public void Model_EndRound_BurnsThenCountsDownEveryone()
        {
            CombatModel model = CreateModel(_assets.Card(1, _assets.Block(5)),
                _assets.Enemy(30, 30, 0, _assets.Move(_assets.Block(1))));
            Combatant enemy = model.Enemies[0].Fighter;
            model.Resolver.ApplyStatus(enemy, _dahan, 6, 0);
            model.Resolver.ApplyStatus(enemy, _bhaya, 2, 0);
            model.Resolver.ApplyStatus(model.Player, _moha, 1, 0);
            int rounds = 0;
            model.RoundEnded += () => rounds++;

            model.EndRound();

            Assert.AreEqual(24, enemy.Health);
            Assert.AreEqual(3, enemy.Statuses.GetAmount(_dahan));
            Assert.AreEqual(1, enemy.Statuses.GetAmount(_bhaya));
            Assert.AreEqual(0, model.Player.Statuses.Count);
            Assert.AreEqual(1, rounds);
        }

        [Test]
        public void Threat_FollowsAMohaThePreviewedCardWouldApply()
        {
            CardDefinitionSO illusion = _assets.Card(1, _assets.ApplyStatus(_moha, 1, 0, true));
            CombatModel model = CreateModel(illusion, _assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(8))));
            model.StartPlayerTurn();
            var outcome = new CombatOutcomePreview();

            model.PreviewPlay(model.Deck.Hand[0], 0, outcome);

            Assert.AreEqual(8, IncomingThreat.Calculate(model).Incoming);
            Assert.AreEqual(6, IncomingThreat.Calculate(model, outcome).Incoming);
        }

        [Test]
        public void Flow_ABurnThatFinishesTheLastEnemy_WinsAtRoundEnd()
        {
            CardDefinitionSO agneyastra = _assets.Card(1, _assets.ApplyStatus(_dahan, 9, 0, true));
            CombatModel model = CreateModel(agneyastra, _assets.Enemy(6, 6, 0, _assets.Move(_assets.Block(1))));
            var flow = new CombatFlow(model, InstantPacing());
            flow.Begin();

            flow.PlayCard(model.Deck.Hand[0].Id, 0);
            flow.EndTurn();
            for (int i = 0; i < 10 && !flow.IsOver; i++)
            {
                flow.Tick(0.1f);
            }

            Assert.AreEqual(CombatPhase.Victory, flow.Phase);
            Assert.AreEqual(1, model.TurnNumber, "A new player turn started after the fight was won.");
        }

        private CombatModel CreateModel(CardDefinitionSO card, params EnemyDefinitionSO[] enemies)
        {
            StartingDeckSO deck = _assets.Deck(new[] { card }, new[] { 10 });
            PlayerDefinitionSO player = _assets.Player(78, deck);
            return new CombatModel(_rules, player, 78, _assets.Encounter(enemies), new CombatRandom(Seed));
        }

        private CombatPacingSO InstantPacing()
        {
            CombatPacingSO pacing = _assets.Create<CombatPacingSO>();
            using var serializedObject = new SerializedObject(pacing);
            serializedObject.FindProperty("_enemyTurnStartDelay").floatValue = 0f;
            serializedObject.FindProperty("_enemyWindup").floatValue = 0f;
            serializedObject.FindProperty("_enemyActionInterval").floatValue = 0f;
            serializedObject.FindProperty("_enemyTurnEndDelay").floatValue = 0f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return pacing;
        }
    }
}
