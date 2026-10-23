// EditMode tests for RunState: a fresh run's health and deck, health carried after a victory, advancing through
// encounters to a win, losing on defeat, ignoring results once finished, seeded fight seeds, and adding cards.
using System;
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Tests;
using NUnit.Framework;
using UnityEditor;

namespace JourneyToLanka.Run.Tests
{
    public sealed class RunStateTests
    {
        private CombatTestAssets _assets;
        private CardDefinitionSO _strike;
        private CardDefinitionSO _guard;
        private RunDefinitionSO _definition;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _strike = _assets.Card(1, _assets.Damage(6));
            _guard = _assets.Card(1, _assets.Block(5));
            PlayerDefinitionSO player = _assets.Player(78, _assets.Deck(new[] { _strike, _guard }, new[] { 5, 5 }));
            CombatEncounterSO encounter = _assets.Encounter(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7))));

            _definition = _assets.Create<RunDefinitionSO>();
            using var serializedObject = new SerializedObject(_definition);
            serializedObject.FindProperty("_player").objectReferenceValue = player;
            SerializedProperty encounters = serializedObject.FindProperty("_encounters");
            encounters.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                encounters.GetArrayElementAtIndex(i).objectReferenceValue = encounter;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void NewRun_StartsAtFullHealthWithTheStartingDeck()
        {
            var run = new RunState(_definition, 7);

            Assert.AreEqual(78, run.Health);
            Assert.AreEqual(78, run.MaxHealth);
            Assert.AreEqual(10, run.Deck.Count);
            Assert.AreEqual(0, run.EncounterIndex);
            Assert.AreEqual(RunOutcome.InProgress, run.Outcome);
            Assert.IsNotNull(run.CurrentEncounter);
        }

        [Test]
        public void Victory_CarriesHealthAndAdvances()
        {
            var run = new RunState(_definition, 7);

            run.ApplyCombatResult(new CombatResult(true, 4, 61));

            Assert.AreEqual(61, run.Health);
            Assert.AreEqual(1, run.EncounterIndex);
            Assert.IsFalse(run.IsFinished);

            PlayerSetup setup = run.CreatePlayerSetup();
            Assert.AreEqual(61, setup.Health);
            Assert.AreEqual(78, setup.MaxHealth);
            Assert.AreEqual(10, setup.Deck.Count);
        }

        [Test]
        public void ClearingEveryEncounter_WinsTheRun()
        {
            var run = new RunState(_definition, 7);

            for (int i = 0; i < 3; i++)
            {
                run.ApplyCombatResult(new CombatResult(true, 3, 50));
            }

            Assert.AreEqual(RunOutcome.Won, run.Outcome);
            Assert.IsTrue(run.IsFinished);
            Assert.IsNull(run.CurrentEncounter);
        }

        [Test]
        public void Defeat_LosesTheRun_AndLaterResultsAreIgnored()
        {
            var run = new RunState(_definition, 7);

            run.ApplyCombatResult(new CombatResult(false, 5, 0));
            run.ApplyCombatResult(new CombatResult(true, 2, 40));

            Assert.AreEqual(RunOutcome.Lost, run.Outcome);
            Assert.AreEqual(0, run.Health);
            Assert.AreEqual(0, run.EncounterIndex);
        }

        [Test]
        public void SameSeed_GivesTheSameFightSeeds()
        {
            var first = new RunState(_definition, 99);
            var second = new RunState(_definition, 99);

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(first.NextCombatSeed(), second.NextCombatSeed());
            }
        }

        [Test]
        public void AddCard_GrowsTheDeck()
        {
            var run = new RunState(_definition, 7);

            run.AddCard(_strike);

            Assert.AreEqual(11, run.Deck.Count);
            Assert.AreSame(_strike, run.Deck[10]);
            Assert.Throws<ArgumentNullException>(() => run.AddCard(null));
        }
    }
}
