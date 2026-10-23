// EditMode tests for card rewards in a run: distinct choices from the pool after a won fight that continues the run,
// no reward after the final fight or a defeat, taking or skipping a reward, seeded rolls, small pools with duplicate
// and empty entries, and runs without a pool.
using System.Collections.Generic;
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Tests;
using NUnit.Framework;
using UnityEditor;

namespace JourneyToLanka.Run.Tests
{
    public sealed class RunRewardTests
    {
        private CombatTestAssets _assets;
        private PlayerDefinitionSO _player;
        private CombatEncounterSO _encounter;
        private CardDefinitionSO[] _poolCards;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            CardDefinitionSO strike = _assets.Card(1, _assets.Damage(6));
            CardDefinitionSO guard = _assets.Card(1, _assets.Block(5));
            _player = _assets.Player(78, _assets.Deck(new[] { strike, guard }, new[] { 5, 5 }));
            _encounter = _assets.Encounter(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7))));
            _poolCards = new[]
            {
                _assets.Card(1, _assets.Damage(4, 2)),
                _assets.Card(2, _assets.Damage(3, 4)),
                _assets.Card(1, _assets.Block(8)),
                _assets.Card(0, _assets.Block(3)),
            };
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void WinThatContinuesTheRun_OffersDistinctCardsFromThePool()
        {
            var run = new RunState(CreateRun(3, CreatePool(_poolCards)), 11);

            run.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.IsTrue(run.HasPendingReward);
            Assert.AreEqual(3, run.PendingReward.Count);
            var seen = new HashSet<CardDefinitionSO>();
            for (int i = 0; i < run.PendingReward.Count; i++)
            {
                Assert.IsTrue(seen.Add(run.PendingReward[i]), "Duplicate reward card.");
                CollectionAssert.Contains(_poolCards, run.PendingReward[i]);
            }
        }

        [Test]
        public void FinalWinAndDefeat_OfferNothing()
        {
            var won = new RunState(CreateRun(1, CreatePool(_poolCards)), 11);
            won.ApplyCombatResult(new CombatResult(true, 3, 60));

            var lost = new RunState(CreateRun(3, CreatePool(_poolCards)), 11);
            lost.ApplyCombatResult(new CombatResult(false, 3, 0));

            Assert.IsFalse(won.HasPendingReward);
            Assert.IsFalse(lost.HasPendingReward);
        }

        [Test]
        public void ResolveReward_AddsTheChosenCardAndClearsTheOffer()
        {
            var run = new RunState(CreateRun(3, CreatePool(_poolCards)), 11);
            run.ApplyCombatResult(new CombatResult(true, 3, 60));
            CardDefinitionSO offered = run.PendingReward[1];

            CardDefinitionSO chosen = run.ResolveReward(1);

            Assert.AreSame(offered, chosen);
            Assert.AreEqual(11, run.Deck.Count);
            Assert.AreSame(offered, run.Deck[10]);
            Assert.IsFalse(run.HasPendingReward);
        }

        [Test]
        public void ResolveReward_WithMinusOne_TakesNothing()
        {
            var run = new RunState(CreateRun(3, CreatePool(_poolCards)), 11);
            run.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.IsNull(run.ResolveReward(-1));
            Assert.AreEqual(10, run.Deck.Count);
            Assert.IsFalse(run.HasPendingReward);
        }

        [Test]
        public void SameSeed_RollsTheSameReward()
        {
            RunDefinitionSO definition = CreateRun(3, CreatePool(_poolCards));
            var first = new RunState(definition, 42);
            var second = new RunState(definition, 42);

            first.ApplyCombatResult(new CombatResult(true, 3, 60));
            second.ApplyCombatResult(new CombatResult(true, 3, 60));

            CollectionAssert.AreEqual(first.PendingReward, second.PendingReward);
        }

        [Test]
        public void SmallPool_OffersEachDistinctCardOnce()
        {
            CardRewardPoolSO pool = CreatePool(_poolCards[0], null, _poolCards[0], _poolCards[1]);
            var run = new RunState(CreateRun(3, pool), 11);

            run.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.AreEqual(2, run.PendingReward.Count);
            CollectionAssert.AreEquivalent(new[] { _poolCards[0], _poolCards[1] }, run.PendingReward);
        }

        [Test]
        public void RunWithoutPool_OffersNothing()
        {
            var run = new RunState(CreateRun(3, null), 11);

            run.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.IsFalse(run.HasPendingReward);
        }

        private CardRewardPoolSO CreatePool(params CardDefinitionSO[] cards)
        {
            CardRewardPoolSO pool = _assets.Create<CardRewardPoolSO>();
            SerializedTestUtility.SetObjectArray(pool, "_cards", cards);
            return pool;
        }

        private RunDefinitionSO CreateRun(int encounterCount, CardRewardPoolSO pool)
        {
            RunDefinitionSO definition = _assets.Create<RunDefinitionSO>();
            using var serializedObject = new SerializedObject(definition);
            serializedObject.FindProperty("_player").objectReferenceValue = _player;
            serializedObject.FindProperty("_rewardPool").objectReferenceValue = pool;
            SerializedProperty encounters = serializedObject.FindProperty("_encounters");
            encounters.arraySize = encounterCount;
            for (int i = 0; i < encounterCount; i++)
            {
                encounters.GetArrayElementAtIndex(i).objectReferenceValue = _encounter;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }
    }
}
