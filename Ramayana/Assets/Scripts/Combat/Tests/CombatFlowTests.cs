// EditMode tests for CombatFlow: the turn cycle, command validation per phase, enemy pacing, field-order enemy
// actions that skip the fallen, victory and defeat, the shared command pipeline, and a full seeded fight
// played by a simple bot that must end the same way every time.
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Commands;
using NUnit.Framework;
using UnityEditor;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatFlowTests
    {
        private const int Seed = 2026;

        private CombatTestAssets _assets;
        private CombatRulesSO _rules;
        private CardDefinitionSO _strike;
        private CardDefinitionSO _guard;
        private EnemyDefinitionSO _scout;
        private CombatPacingSO _instant;

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
            _instant = Pacing(0f, 0f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Begin_StartsThePlayerTurn()
        {
            CombatFlow flow = CreateFlow(SliceDeck(), 78, _instant, _scout);
            var phases = new List<CombatPhase>();
            flow.PhaseChanged += phases.Add;

            flow.Begin();

            Assert.IsTrue(flow.IsPlayerTurn);
            Assert.AreEqual(1, flow.Model.TurnNumber);
            Assert.AreEqual(5, flow.Model.Deck.Hand.Count);
            CollectionAssert.AreEqual(new[] { CombatPhase.PlayerTurn }, phases);
            Assert.Throws<System.InvalidOperationException>(flow.Begin);
        }

        [Test]
        public void Commands_AreValidatedAgainstThePhase()
        {
            CombatFlow flow = CreateFlow(OnlyDeck(_strike), 78, Pacing(1f, 1f, 1f), _scout);

            Assert.AreEqual(CommandSubmitResult.Rejected, flow.EndTurn());

            flow.Begin();
            int cardId = flow.Model.Deck.Hand[0].Id;
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.PlayCard(999, 0));
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.PlayCard(cardId, 7));
            Assert.AreEqual(CommandSubmitResult.Executed, flow.PlayCard(cardId, 0));

            Assert.AreEqual(CommandSubmitResult.Executed, flow.EndTurn());
            Assert.IsTrue(flow.IsEnemyTurn);
            Assert.AreEqual(0, flow.Model.Deck.Hand.Count);
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.EndTurn());
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.PlayCard(cardId, 0));
        }

        [Test]
        public void EnemyTurn_IsPacedByTicks()
        {
            EnemyDefinitionSO attacker = _assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7)));
            CombatFlow flow = CreateFlow(SliceDeck(), 78, Pacing(0.5f, 1f, 0.5f), attacker);
            flow.Begin();
            flow.EndTurn();

            flow.Tick(0.4f);
            Assert.AreEqual(78, flow.Model.Player.Health, "Acted before the start delay.");

            flow.Tick(0.2f);
            Assert.AreEqual(71, flow.Model.Player.Health, "Did not act after the start delay.");

            flow.Tick(0.4f);
            Assert.IsTrue(flow.IsEnemyTurn, "Returned to the player before the end delay.");

            flow.Tick(0.2f);
            Assert.IsTrue(flow.IsPlayerTurn);
            Assert.AreEqual(2, flow.Model.TurnNumber);
        }

        [Test]
        public void Enemies_ActInFieldOrder_SkippingTheFallen()
        {
            EnemyDefinitionSO first = _assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(3)));
            EnemyDefinitionSO fragile = _assets.Enemy(6, 6, 0, _assets.Move(_assets.Damage(5)));
            EnemyDefinitionSO third = _assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7)));
            CombatFlow flow = CreateFlow(OnlyDeck(_strike), 78, _instant, first, fragile, third);
            var actors = new List<int>();
            flow.Commands.Executed += command =>
            {
                if (command is EnemyActCommand act)
                {
                    actors.Add(act.EnemyIndex);
                }
            };

            flow.Begin();
            flow.PlayCard(flow.Model.Deck.Hand[0].Id, 1);
            flow.EndTurn();

            flow.Tick(0.1f);
            Assert.AreEqual(75, flow.Model.Player.Health);
            flow.Tick(0.1f);
            Assert.AreEqual(68, flow.Model.Player.Health);
            flow.Tick(0.1f);

            Assert.IsTrue(flow.IsPlayerTurn);
            CollectionAssert.AreEqual(new[] { 0, 2 }, actors);
        }

        [Test]
        public void KillingTheLastEnemy_EndsInVictory_AndRejectsFurtherCommands()
        {
            EnemyDefinitionSO fragile = _assets.Enemy(6, 6, 0, _assets.Move(_assets.Damage(7)));
            CombatFlow flow = CreateFlow(OnlyDeck(_strike), 78, _instant, fragile);
            CombatPhase lastPhase = CombatPhase.None;
            flow.PhaseChanged += phase => lastPhase = phase;
            flow.Begin();

            flow.PlayCard(flow.Model.Deck.Hand[0].Id, 0);

            Assert.AreEqual(CombatPhase.Victory, flow.Phase);
            Assert.AreEqual(CombatPhase.Victory, lastPhase);
            Assert.IsTrue(flow.IsOver);
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.EndTurn());
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.PlayCard(flow.Model.Deck.Hand[0].Id, 0));
        }

        [Test]
        public void PlayerFalling_EndsInDefeat_AndLaterEnemiesDoNotAct()
        {
            EnemyDefinitionSO attacker = _assets.Enemy(30, 30, 0,
                _assets.Move(_assets.Damage(7)),
                _assets.Move(_assets.Damage(7)));
            CombatFlow flow = CreateFlow(SliceDeck(), 5, _instant, attacker, attacker);
            flow.Begin();
            flow.EndTurn();

            flow.Tick(0.1f);
            flow.Tick(0.1f);
            flow.Tick(0.1f);

            Assert.AreEqual(CombatPhase.Defeat, flow.Phase);
            Assert.AreEqual(0, flow.Model.Player.Health);
            Assert.AreEqual(1, flow.Model.Enemies[0].PatternIndex);
            Assert.AreEqual(0, flow.Model.Enemies[1].PatternIndex);
        }

        [Test]
        public void EveryAction_GoesThroughTheCommandPipeline()
        {
            CombatFlow flow = CreateFlow(OnlyDeck(_strike), 78, _instant, _scout);
            var executed = new List<string>();
            flow.Commands.Executed += command =>
            {
                switch (command)
                {
                    case PlayCardCommand play:
                        executed.Add("play:" + play.CardInstanceId + ">" + play.TargetIndex);
                        break;
                    case EndTurnCommand _:
                        executed.Add("end");
                        break;
                    case EnemyActCommand act:
                        executed.Add("enemy:" + act.EnemyIndex);
                        break;
                }
            };

            flow.Begin();
            int cardId = flow.Model.Deck.Hand[0].Id;
            flow.PlayCard(cardId, 0);
            flow.EndTurn();
            TickUntilPlayerTurn(flow);

            CollectionAssert.AreEqual(new[] { "play:" + cardId + ">0", "end", "enemy:0" }, executed);
        }

        [Test]
        public void Stop_EndsTheFlow_AndRejectsCommands()
        {
            CombatFlow flow = CreateFlow(SliceDeck(), 78, _instant, _scout);
            flow.Begin();

            flow.Stop();

            Assert.AreEqual(CombatPhase.None, flow.Phase);
            Assert.AreEqual(CommandSubmitResult.Rejected, flow.EndTurn());
            Assert.DoesNotThrow(() => flow.Tick(1f));
        }

        [Test]
        public void FullFight_AgainstTheScout_IsWonAndDeterministic()
        {
            (CombatPhase Phase, int Turns, int PlayerHealth, int CommandsExecuted) first = RunBotFight();
            (CombatPhase Phase, int Turns, int PlayerHealth, int CommandsExecuted) second = RunBotFight();

            Assert.AreEqual(CombatPhase.Victory, first.Phase);
            Assert.Greater(first.Turns, 1);
            Assert.AreEqual(first.Turns, second.Turns);
            Assert.AreEqual(first.PlayerHealth, second.PlayerHealth);
            Assert.AreEqual(first.CommandsExecuted, second.CommandsExecuted);
        }

        private (CombatPhase Phase, int Turns, int PlayerHealth, int CommandsExecuted) RunBotFight()
        {
            CombatFlow flow = CreateFlow(SliceDeck(), 78, Pacing(0.5f, 0.8f, 0.5f), _scout);
            int executed = 0;
            flow.Commands.Executed += _ => executed++;
            flow.Begin();

            for (int turn = 0; turn < 50 && !flow.IsOver; turn++)
            {
                PlayEveryPlayableCard(flow);
                if (flow.IsOver)
                {
                    break;
                }

                flow.EndTurn();
                TickUntilPlayerTurn(flow);
            }

            return (flow.Phase, flow.Model.TurnNumber, flow.Model.Player.Health, executed);
        }

        private static void PlayEveryPlayableCard(CombatFlow flow)
        {
            bool played = true;
            while (played && flow.IsPlayerTurn)
            {
                played = false;
                CardPile hand = flow.Model.Deck.Hand;
                for (int i = 0; i < hand.Count; i++)
                {
                    CardInstance card = hand[i];
                    int target = card.RequiresChosenTarget ? flow.Model.FirstLivingEnemyIndex() : -1;
                    if (flow.PlayCard(card.Id, target) == CommandSubmitResult.Executed)
                    {
                        played = true;
                        break;
                    }
                }
            }
        }

        private static void TickUntilPlayerTurn(CombatFlow flow)
        {
            for (int frame = 0; frame < 1000 && flow.IsEnemyTurn; frame++)
            {
                flow.Tick(1f / 60f);
            }
        }

        private CombatFlow CreateFlow(StartingDeckSO deck, int playerHealth, CombatPacingSO pacing, params EnemyDefinitionSO[] enemies)
        {
            PlayerDefinitionSO player = _assets.Player(78, deck);
            var model = new CombatModel(_rules, player, playerHealth, _assets.Encounter(enemies), new CombatRandom(Seed));
            return new CombatFlow(model, pacing);
        }

        private CombatPacingSO Pacing(float startDelay, float actionInterval, float endDelay)
        {
            CombatPacingSO pacing = _assets.Create<CombatPacingSO>();
            using var serializedObject = new SerializedObject(pacing);
            serializedObject.FindProperty("_enemyTurnStartDelay").floatValue = startDelay;
            serializedObject.FindProperty("_enemyActionInterval").floatValue = actionInterval;
            serializedObject.FindProperty("_enemyTurnEndDelay").floatValue = endDelay;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return pacing;
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
