// EditMode tests for the special-arrow drop table, against the reference build: the chance falls by its step after a
// drop and rises after none, held between the floor and the ceiling; tiers are picked by weight; a tier with no
// arrows falls back to any arrow; and the same seed rolls the same arrows.
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class ArrowDropTableTests
    {
        private CombatTestAssets _assets;
        private ArrowDefinitionSO _common;
        private ArrowDefinitionSO _uncommon;
        private ArrowDefinitionSO _rare;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _common = _assets.Arrow(ArrowTier.Common, _assets.Block(1));
            _uncommon = _assets.Arrow(ArrowTier.Uncommon, _assets.Block(2));
            _rare = _assets.Arrow(ArrowTier.Rare, _assets.Block(3));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void NextChance_StepsDownAfterADrop_UpAfterNone_WithinTheLimits()
        {
            ArrowDropTableSO table = _assets.DropTable(40, 10, 10, 90, 62, 30, 8, _common);

            Assert.AreEqual(30, table.NextChance(40, true));
            Assert.AreEqual(50, table.NextChance(40, false));
            Assert.AreEqual(10, table.NextChance(10, true));
            Assert.AreEqual(90, table.NextChance(90, false));
        }

        [Test]
        public void Roll_PicksTheTierByWeight()
        {
            ArrowDropTableSO onlyRare = _assets.DropTable(40, 10, 10, 90, 0, 0, 1, _common, _uncommon, _rare);
            ArrowDropTableSO onlyCommon = _assets.DropTable(40, 10, 10, 90, 1, 0, 0, _common, _uncommon, _rare);
            var random = new CombatRandom(7);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreSame(_rare, onlyRare.Roll(random));
                Assert.AreSame(_common, onlyCommon.Roll(random));
            }
        }

        [Test]
        public void Roll_FallsBackToAnyArrow_WhenTheTierHasNone()
        {
            ArrowDropTableSO table = _assets.DropTable(40, 10, 10, 90, 0, 0, 1, _common);

            Assert.AreSame(_common, table.Roll(new CombatRandom(3)));
            Assert.IsNull(_assets.DropTable(40, 10, 10, 90, 62, 30, 8).Roll(new CombatRandom(3)));
        }

        [Test]
        public void Roll_IsSeeded()
        {
            ArrowDropTableSO table = _assets.DropTable(40, 10, 10, 90, 62, 30, 8, _common, _uncommon, _rare);
            var first = new CombatRandom(99);
            var second = new CombatRandom(99);

            for (int i = 0; i < 30; i++)
            {
                Assert.AreSame(table.Roll(first), table.Roll(second));
            }
        }
    }
}
