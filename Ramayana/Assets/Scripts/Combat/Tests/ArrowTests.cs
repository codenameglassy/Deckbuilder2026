// EditMode tests for the special-arrow quiver, against the reference build: the quiver keeps three arrows in order
// and leaves a fourth behind, a fired arrow costs no Prana and is gone from the quiver, the Arrow of Agni hits then
// burns, Surya's Prana goes above the per-turn amount, Sanjivani heals a fifth of maximum health rounded down, Garuda
// clears Moha and draws, Indra hits every enemy through Rama's modifiers, a targeted arrow needs a living target, the
// fire command only works on Rama's turn, a fired arrow is loosed at once and resolves only after its flight, player
// commands are refused while it flies, a stopped fight drops an arrow in flight, with no flight time an arrow is
// loosed and lands in the same call, the preview shows an arrow's outcome without spending it, and a player
// definition's starting arrows fill a fresh quiver.
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.Commands;
using NUnit.Framework;
using UnityEditor;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class ArrowTests
    {
        private const int Seed = 2026;

        private CombatTestAssets _assets;
        private CombatRulesSO _rules;
        private StatusDefinitionSO _moha;
        private StatusDefinitionSO _dahan;
        private StatusDefinitionSO _agniPower;
        private EnemyDefinitionSO _dummy;
        private CardDefinitionSO _guard;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _rules = _assets.Rules(3, 5, 10);
            _moha = _assets.Status("Moha", 3, true, _assets.Create<TurnTimerDecaySO>(), _assets.OutgoingScale(10, 75));
            _dahan = _assets.Status("Dahan", 0, true, _assets.BurnDecay(50), null);
            _agniPower = _assets.Power("Agni", 2, _assets.FlatOutgoing(0), null);
            _dummy = _assets.Enemy(60, 60, 0, _assets.Move(_assets.Block(1)));
            _guard = _assets.Card(1, _assets.Block(5));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Quiver_KeepsArrowsInOrder_LeavesOneBehindWhenFull_AndClosesGaps()
        {
            ArrowDefinitionSO a = _assets.Arrow(_assets.Block(1));
            ArrowDefinitionSO b = _assets.Arrow(_assets.Block(2));
            ArrowDefinitionSO c = _assets.Arrow(_assets.Block(3));
            var quiver = new Quiver(3);
            int changes = 0;
            quiver.Changed += _ => changes++;

            Assert.IsTrue(quiver.TryAdd(a));
            Assert.IsTrue(quiver.TryAdd(b));
            Assert.IsTrue(quiver.TryAdd(c));
            Assert.IsFalse(quiver.TryAdd(_assets.Arrow(_assets.Block(4))));
            Assert.IsTrue(quiver.IsFull);

            Assert.AreSame(b, quiver.RemoveAt(1));
            Assert.AreSame(c, quiver[1]);
            Assert.AreEqual(2, quiver.Count);
            Assert.IsNull(quiver.RemoveAt(5));
            Assert.AreEqual(4, changes);
        }

        [Test]
        public void ArrowOfAgni_CostsNoPrana_HitsThenBurns_AndLeavesTheQuiver()
        {
            ArrowDefinitionSO agni = _assets.Arrow(_assets.Damage(12), _assets.ApplyStatus(_dahan, 3, 0, true));
            CombatModel model = CreateModel(agni);
            Combatant enemy = model.Enemies[0].Fighter;
            model.StartPlayerTurn();
            var fired = new List<ArrowDefinitionSO>();
            model.ArrowFired += (arrow, target) => fired.Add(arrow);

            Assert.IsTrue(model.TryFireArrow(0, 0));

            Assert.AreEqual(48, enemy.Health);
            Assert.AreEqual(3, enemy.Statuses.GetAmount(_dahan));
            Assert.AreEqual(3, model.Prana.Current);
            Assert.AreEqual(0, model.Quiver.Count);
            Assert.AreEqual(5, model.Deck.Hand.Count);
            CollectionAssert.AreEqual(new[] { agni }, fired);
            Assert.AreEqual(ArrowUseCheck.NoArrow, model.CheckArrow(0, 0));
        }

        [Test]
        public void ArrowOfSurya_GainsPranaAboveThePerTurnAmount()
        {
            CombatModel model = CreateModel(_assets.Arrow(_assets.GainPrana(2)));
            model.StartPlayerTurn();

            model.TryFireArrow(0, -1);

            Assert.AreEqual(5, model.Prana.Current);
        }

        [Test]
        public void SanjivaniArrow_HealsAFifthOfMaxHealth_RoundedDown()
        {
            CombatModel model = CreateModel(50, _assets.Arrow(_assets.HealPercent(20)));
            model.StartPlayerTurn();

            model.TryFireArrow(0, -1);

            Assert.AreEqual(65, model.Player.Health, "78 x 20% = 15.6, so 15.");
        }

        [Test]
        public void ArrowOfGaruda_ClearsMoha_AndDrawsTwo()
        {
            CombatModel model = CreateModel(_assets.Arrow(_assets.RemoveStatus(_moha), _assets.Draw(2)));
            model.StartPlayerTurn();
            model.Resolver.ApplyStatus(model.Player, _moha, 2, 0);

            model.TryFireArrow(0, -1);

            Assert.AreEqual(0, model.Player.Statuses.GetAmount(_moha));
            Assert.AreEqual(7, model.Deck.Hand.Count);
        }

        [Test]
        public void ArrowOfIndra_HitsEveryEnemy_ThroughRamasModifiers()
        {
            CombatModel model = CreateModel(78, new[] { _assets.Arrow(_assets.DamageAll(8)) }, _dummy, _dummy);
            model.StartPlayerTurn();
            model.Resolver.ApplyStatus(model.Player, _agniPower, 2, 0);

            Assert.AreEqual(ArrowUseCheck.Usable, model.CheckArrow(0, -1));
            model.TryFireArrow(0, -1);

            Assert.AreEqual(50, model.Enemies[0].Fighter.Health);
            Assert.AreEqual(50, model.Enemies[1].Fighter.Health);
        }

        [Test]
        public void TargetedArrow_NeedsALivingTarget()
        {
            CombatModel model = CreateModel(_assets.Arrow(_assets.Damage(25)));
            model.StartPlayerTurn();

            Assert.AreEqual(ArrowUseCheck.InvalidTarget, model.CheckArrow(0, -1));
            Assert.AreEqual(ArrowUseCheck.InvalidTarget, model.CheckArrow(0, 3));
            Assert.IsFalse(model.TryFireArrow(0, -1));
            Assert.AreEqual(1, model.Quiver.Count);
        }

        [Test]
        public void FireArrowCommand_WorksOnlyOnRamasTurn()
        {
            CombatModel model = CreateModel(_assets.Arrow(_assets.Block(12)), _assets.Arrow(_assets.Block(12)));
            var flow = new CombatFlow(model, Pacing(1f));

            Assert.AreEqual(CommandSubmitResult.Rejected, flow.FireArrow(0, -1));
            flow.Begin();
            Assert.AreEqual(CommandSubmitResult.Executed, flow.FireArrow(0, -1));
            Assert.AreEqual(12, model.Player.Block);

            flow.EndTurn();
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.FireArrow(0, -1));
            Assert.AreEqual(1, model.Quiver.Count);
        }

        [Test]
        public void FiredArrow_IsLoosedAtOnce_AndResolvesOnlyAfterItsFlight()
        {
            CombatModel model = CreateModel(_assets.Arrow(_assets.Block(12)));
            var flow = new CombatFlow(model, Pacing(1f, 0.5f));
            var loosed = new List<int>();
            var landed = new List<int>();
            flow.ArrowLoosed += (slot, target) => loosed.Add(slot);
            flow.ArrowLanded += (slot, target) => landed.Add(slot);
            flow.Begin();

            Assert.AreEqual(CommandSubmitResult.Executed, flow.FireArrow(0, -1));
            CollectionAssert.AreEqual(new[] { 0 }, loosed);
            Assert.IsTrue(flow.IsArrowInFlight);
            Assert.IsFalse(flow.CanPlayerAct);
            Assert.AreEqual(0, model.Player.Block);
            Assert.AreEqual(1, model.Quiver.Count);

            flow.Tick(0.3f);
            Assert.AreEqual(0, model.Player.Block);
            Assert.IsEmpty(landed);

            flow.Tick(0.3f);
            Assert.AreEqual(12, model.Player.Block);
            Assert.AreEqual(0, model.Quiver.Count);
            CollectionAssert.AreEqual(new[] { 0 }, landed);
            Assert.IsFalse(flow.IsArrowInFlight);
            Assert.IsTrue(flow.CanPlayerAct);
        }

        [Test]
        public void WhileAnArrowFlies_CardsArrowsAndEndTurn_AreRefused()
        {
            CombatModel model = CreateModel(_assets.Arrow(_assets.Block(12)), _assets.Arrow(_assets.Block(12)));
            var flow = new CombatFlow(model, Pacing(1f, 0.5f));
            flow.Begin();
            int cardId = model.Deck.Hand[0].Id;

            flow.FireArrow(0, -1);

            Assert.AreEqual(CommandSubmitResult.Rejected, flow.PlayCard(cardId, -1));
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.FireArrow(1, -1));
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.EndTurn());
            Assert.AreEqual(2, model.Quiver.Count);

            flow.Tick(0.5f);

            Assert.AreEqual(CommandSubmitResult.Executed, flow.PlayCard(cardId, -1));
            Assert.AreEqual(CommandSubmitResult.Executed, flow.EndTurn());
        }

        [Test]
        public void StoppingTheFight_DropsAnArrowInFlight()
        {
            CombatModel model = CreateModel(_assets.Arrow(_assets.Block(12)));
            var flow = new CombatFlow(model, Pacing(1f, 0.5f));
            flow.Begin();
            flow.FireArrow(0, -1);

            flow.Stop();
            flow.Tick(1f);

            Assert.IsFalse(flow.IsArrowInFlight);
            Assert.AreEqual(0, model.Player.Block);
            Assert.AreEqual(1, model.Quiver.Count);
        }

        [Test]
        public void WithNoFlightTime_AnArrowIsLoosedAndLandsInTheSameCall()
        {
            CombatModel model = CreateModel(_assets.Arrow(_assets.Block(12)));
            var flow = new CombatFlow(model, Pacing(1f));
            var events = new List<string>();
            flow.ArrowLoosed += (slot, target) => events.Add("loosed");
            model.ArrowFired += (arrow, target) => events.Add("fired");
            flow.ArrowLanded += (slot, target) => events.Add("landed");
            flow.Begin();

            flow.FireArrow(0, -1);

            CollectionAssert.AreEqual(new[] { "loosed", "fired", "landed" }, events);
            Assert.IsFalse(flow.IsArrowInFlight);
            Assert.AreEqual(12, model.Player.Block);
        }

        [Test]
        public void PreviewArrow_ShowsTheOutcome_WithoutSpendingTheArrow()
        {
            CombatModel model = CreateModel(_assets.Arrow(_assets.Damage(12), _assets.ApplyStatus(_dahan, 3, 0, true)));
            Combatant enemy = model.Enemies[0].Fighter;
            model.StartPlayerTurn();
            var outcome = new CombatOutcomePreview();

            model.PreviewArrow(0, 0, outcome);

            Assert.IsTrue(outcome.TryGet(enemy, out CombatantOutcome previewed));
            Assert.AreEqual(12, previewed.HealthLost);
            Assert.IsTrue(outcome.TryGetStatuses(enemy, out StatusSet statuses));
            Assert.AreEqual(3, statuses.GetAmount(_dahan));
            Assert.AreEqual(60, enemy.Health);
            Assert.AreEqual(1, model.Quiver.Count);
        }

        [Test]
        public void PlayerDefinition_StartingArrowsFillAFreshQuiver_UpToItsSize()
        {
            ArrowDefinitionSO vajra = _assets.Arrow(_assets.Block(12));
            StartingDeckSO deck = _assets.Deck(new[] { _guard }, new[] { 10 });
            PlayerDefinitionSO player = _assets.Player(78, deck);
            SerializedTestUtility.SetObjectArray(player, "_startingArrows", vajra, vajra, vajra, vajra);

            PlayerSetup setup = PlayerSetup.FromDefinition(player, 78);

            Assert.AreEqual(3, setup.Quiver.Capacity);
            Assert.AreEqual(3, setup.Quiver.Count);
        }

        private CombatModel CreateModel(params ArrowDefinitionSO[] arrows)
        {
            return CreateModel(78, arrows, _dummy);
        }

        private CombatModel CreateModel(int playerHealth, params ArrowDefinitionSO[] arrows)
        {
            return CreateModel(playerHealth, arrows, _dummy);
        }

        private CombatModel CreateModel(int playerHealth, ArrowDefinitionSO[] arrows, params EnemyDefinitionSO[] enemies)
        {
            var deck = new List<CardDefinitionSO>(10);
            for (int i = 0; i < 10; i++)
            {
                deck.Add(_guard);
            }

            var setup = new PlayerSetup("rama", 78, playerHealth, deck, new Quiver(3, arrows));
            return new CombatModel(_rules, setup, _assets.Encounter(enemies), new CombatRandom(Seed));
        }

        private CombatPacingSO Pacing(float delay, float arrowFlight = 0f)
        {
            CombatPacingSO pacing = _assets.Create<CombatPacingSO>();
            using var serializedObject = new SerializedObject(pacing);
            serializedObject.FindProperty("_enemyTurnStartDelay").floatValue = delay;
            serializedObject.FindProperty("_enemyWindup").floatValue = 0f;
            serializedObject.FindProperty("_enemyActionInterval").floatValue = delay;
            serializedObject.FindProperty("_enemyTurnEndDelay").floatValue = delay;
            serializedObject.FindProperty("_arrowFlight").floatValue = arrowFlight;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return pacing;
        }
    }
}
