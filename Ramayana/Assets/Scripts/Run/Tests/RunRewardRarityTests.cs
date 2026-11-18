// EditMode tests for card reward rarity in a run, against the reference build: every choice is a card of the rolled
// rarity while that rarity has cards left, a rarity with too few cards falls back to the rest of the pool, the won
// encounter's odds take priority over the run's default, and runs without odds still offer distinct pool cards.
using System.Collections.Generic;
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Tests;
using NUnit.Framework;
using UnityEditor;

namespace JourneyToLanka.Run.Tests
{
    public sealed class RunRewardRarityTests
    {
        private CombatTestAssets _assets;
        private PlayerDefinitionSO _player;
        private CardDefinitionSO[] _commons;
        private CardDefinitionSO[] _epics;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            CardDefinitionSO strike = _assets.Card(1, _assets.Damage(6));
            _player = _assets.Player(78, _assets.Deck(new[] { strike }, new[] { 10 }));
            _commons = new[]
            {
                Card(CardRarity.Common, 4), Card(CardRarity.Common, 5), Card(CardRarity.Common, 6), Card(CardRarity.Common, 7),
            };
            _epics = new[] { Card(CardRarity.Epic, 20), Card(CardRarity.Epic, 21), Card(CardRarity.Epic, 22) };
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void EveryChoice_HasTheRolledRarity_WhileThatRarityHasCards()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var run = new RunState(CreateRun(Encounter(null), Odds(0, 0, 1), _commons[0], _epics[0], _commons[1],
                    _epics[1], _commons[2], _epics[2]), seed);

                run.ApplyCombatResult(new CombatResult(true, 3, 60));

                CollectionAssert.AreEquivalent(_epics, run.PendingReward);
            }
        }

        [Test]
        public void RarityWithTooFewCards_FallsBackToTheRestOfThePool()
        {
            var run = new RunState(CreateRun(Encounter(null), Odds(0, 0, 1), _commons[0], _commons[1], _epics[0],
                _commons[2]), 9);

            run.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.AreEqual(3, run.PendingReward.Count);
            Assert.AreSame(_epics[0], run.PendingReward[0]);
            AssertDistinct(run.PendingReward);
        }

        [Test]
        public void EncounterOdds_TakePriorityOverTheRunsDefault()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var run = new RunState(CreateRun(Encounter(Odds(0, 0, 1)), Odds(1, 0, 0), _commons[0], _commons[1],
                    _commons[2], _commons[3], _epics[0], _epics[1], _epics[2]), seed);

                run.ApplyCombatResult(new CombatResult(true, 3, 60));

                CollectionAssert.AreEquivalent(_epics, run.PendingReward);
            }
        }

        [Test]
        public void RunsDefaultOdds_ApplyWhenTheEncounterHasNone()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var run = new RunState(CreateRun(Encounter(null), Odds(1, 0, 0), _commons[0], _commons[1], _commons[2],
                    _epics[0], _epics[1], _epics[2]), seed);

                run.ApplyCombatResult(new CombatResult(true, 3, 60));

                CollectionAssert.AreEquivalent(new[] { _commons[0], _commons[1], _commons[2] }, run.PendingReward);
            }
        }

        [Test]
        public void NoOdds_OffersDistinctPoolCards_AndIsSeeded()
        {
            RunDefinitionSO definition = CreateRun(Encounter(null), null, _commons[0], _commons[1], _epics[0], _epics[1]);
            var first = new RunState(definition, 31);
            var second = new RunState(definition, 31);

            first.ApplyCombatResult(new CombatResult(true, 3, 60));
            second.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.AreEqual(3, first.PendingReward.Count);
            AssertDistinct(first.PendingReward);
            CollectionAssert.AreEqual(first.PendingReward, second.PendingReward);
        }

        private static void AssertDistinct(IReadOnlyList<CardDefinitionSO> cards)
        {
            var seen = new HashSet<CardDefinitionSO>();
            for (int i = 0; i < cards.Count; i++)
            {
                Assert.IsTrue(seen.Add(cards[i]), "Duplicate reward card.");
            }
        }

        private CardDefinitionSO Card(CardRarity rarity, int damage)
        {
            CardDefinitionSO card = _assets.Card(1, _assets.Damage(damage));
            using var serializedObject = new SerializedObject(card);
            serializedObject.FindProperty("_rarity").enumValueIndex = (int)rarity;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        private CardRewardOddsSO Odds(int common, int rare, int epic)
        {
            CardRewardOddsSO odds = _assets.Create<CardRewardOddsSO>();
            using var serializedObject = new SerializedObject(odds);
            serializedObject.FindProperty("_commonWeight").intValue = common;
            serializedObject.FindProperty("_rareWeight").intValue = rare;
            serializedObject.FindProperty("_epicWeight").intValue = epic;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return odds;
        }

        private CombatEncounterSO Encounter(CardRewardOddsSO odds)
        {
            CombatEncounterSO encounter = _assets.Encounter(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7))));
            using var serializedObject = new SerializedObject(encounter);
            serializedObject.FindProperty("_rewardOdds").objectReferenceValue = odds;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return encounter;
        }

        private RunDefinitionSO CreateRun(CombatEncounterSO encounter, CardRewardOddsSO odds, params CardDefinitionSO[] pool)
        {
            CardRewardPoolSO rewardPool = _assets.Create<CardRewardPoolSO>();
            SerializedTestUtility.SetObjectArray(rewardPool, "_cards", pool);

            RunDefinitionSO definition = _assets.Create<RunDefinitionSO>();
            using var serializedObject = new SerializedObject(definition);
            serializedObject.FindProperty("_player").objectReferenceValue = _player;
            serializedObject.FindProperty("_rewardPool").objectReferenceValue = rewardPool;
            serializedObject.FindProperty("_rewardOdds").objectReferenceValue = odds;
            SerializedProperty encounters = serializedObject.FindProperty("_encounters");
            encounters.arraySize = 2;
            encounters.GetArrayElementAtIndex(0).objectReferenceValue = encounter;
            encounters.GetArrayElementAtIndex(1).objectReferenceValue = encounter;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }
    }
}
