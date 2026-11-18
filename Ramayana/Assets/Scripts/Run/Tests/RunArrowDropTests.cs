// EditMode tests for special arrows across a run: a run starts with its player's starting arrows in a quiver that
// every fight shares, so an arrow fired in one fight is gone in the next; a won fight that continues the run rolls
// for an arrow with the run's drop table, putting it in the quiver or leaving it behind when the quiver is full; the
// running chance follows the table; and runs without a drop table never drop one.
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Tests;
using NUnit.Framework;
using UnityEditor;

namespace JourneyToLanka.Run.Tests
{
    public sealed class RunArrowDropTests
    {
        private CombatTestAssets _assets;
        private PlayerDefinitionSO _player;
        private CombatEncounterSO _encounter;
        private ArrowDefinitionSO _vajra;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            CardDefinitionSO guard = _assets.Card(1, _assets.Block(5));
            _player = _assets.Player(78, _assets.Deck(new[] { guard }, new[] { 10 }));
            _encounter = _assets.Encounter(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7))));
            _vajra = _assets.Arrow(ArrowTier.Common, _assets.Block(12));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void TheRunsQuiver_IsSharedByEveryFight()
        {
            SerializedTestUtility.SetObjectArray(_player, "_startingArrows", _vajra, _vajra);
            var run = new RunState(CreateRun(null), 11);

            PlayerSetup first = run.CreatePlayerSetup();
            first.Quiver.RemoveAt(0);

            Assert.AreSame(run.Quiver, first.Quiver);
            Assert.AreEqual(1, run.CreatePlayerSetup().Quiver.Count);
        }

        [Test]
        public void CertainDrop_GoesIntoTheQuiver_AndTheChanceSteps()
        {
            var run = new RunState(CreateRun(_assets.DropTable(100, 10, 0, 100, 1, 0, 0, _vajra)), 11);

            run.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.AreSame(_vajra, run.LastArrowDrop);
            Assert.IsTrue(run.LastArrowKept);
            Assert.AreEqual(1, run.Quiver.Count);
            Assert.AreEqual(90, run.ArrowChance);
        }

        [Test]
        public void DropIntoAFullQuiver_IsLeftBehind()
        {
            SerializedTestUtility.SetObjectArray(_player, "_startingArrows", _vajra, _vajra, _vajra);
            var run = new RunState(CreateRun(_assets.DropTable(100, 0, 0, 100, 1, 0, 0, _vajra)), 11);

            run.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.AreSame(_vajra, run.LastArrowDrop);
            Assert.IsFalse(run.LastArrowKept);
            Assert.AreEqual(3, run.Quiver.Count);
        }

        [Test]
        public void ImpossibleDrop_RaisesTheChance_AndClearsTheLastDrop()
        {
            var run = new RunState(CreateRun(_assets.DropTable(0, 10, 0, 100, 1, 0, 0, _vajra)), 11);

            run.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.IsNull(run.LastArrowDrop);
            Assert.AreEqual(10, run.ArrowChance);
            Assert.AreEqual(0, run.Quiver.Count);
        }

        [Test]
        public void RunWithoutADropTable_NeverDrops()
        {
            var run = new RunState(CreateRun(null), 11);

            run.ApplyCombatResult(new CombatResult(true, 3, 60));
            run.ApplyCombatResult(new CombatResult(true, 3, 60));

            Assert.IsNull(run.LastArrowDrop);
            Assert.AreEqual(0, run.Quiver.Count);
        }

        private RunDefinitionSO CreateRun(ArrowDropTableSO drops)
        {
            RunDefinitionSO definition = _assets.Create<RunDefinitionSO>();
            using var serializedObject = new SerializedObject(definition);
            serializedObject.FindProperty("_player").objectReferenceValue = _player;
            serializedObject.FindProperty("_arrowDrops").objectReferenceValue = drops;
            SerializedProperty encounters = serializedObject.FindProperty("_encounters");
            encounters.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                encounters.GetArrayElementAtIndex(i).objectReferenceValue = _encounter;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }
    }
}
