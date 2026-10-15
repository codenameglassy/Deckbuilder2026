// EditMode tests for EnemyCombatant: inclusive, seeded health rolls, the move pattern with a start offset and wrap,
// dropping its own Kavach before acting, attacking the player, and a fallen enemy never acting.
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class EnemyCombatantTests
    {
        private CombatTestAssets _assets;
        private CombatResolver _resolver;
        private Combatant _player;
        private ICombatant[] _playerSide;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _resolver = new CombatResolver();
            _player = new Combatant("player", 78, 78);
            _playerSide = new ICombatant[] { _player };
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Health_IsRolledInclusivelyWithinTheRange()
        {
            EnemyDefinitionSO definition = _assets.Enemy(30, 34, 0, _assets.Move());
            bool sawMin = false;
            bool sawMax = false;

            for (int seed = 0; seed < 500; seed++)
            {
                var enemy = new EnemyCombatant(definition, 0, new CombatRandom(seed));
                int health = enemy.Fighter.Health;
                Assert.That(health, Is.InRange(30, 34));
                Assert.AreEqual(health, enemy.Fighter.MaxHealth);
                sawMin |= health == 30;
                sawMax |= health == 34;
            }

            Assert.IsTrue(sawMin && sawMax, "Both ends of the range should be reachable.");
        }

        [Test]
        public void Health_SameSeed_GivesSameRoll()
        {
            EnemyDefinitionSO definition = _assets.Enemy(30, 34, 0, _assets.Move());

            var first = new EnemyCombatant(definition, 0, new CombatRandom(77));
            var second = new EnemyCombatant(definition, 0, new CombatRandom(77));

            Assert.AreEqual(first.Fighter.Health, second.Fighter.Health);
        }

        [Test]
        public void Pattern_StartsAtStartIndex_AndWraps()
        {
            EnemyMoveSO first = _assets.Move();
            EnemyMoveSO second = _assets.Move();
            EnemyMoveSO third = _assets.Move();
            var enemy = new EnemyCombatant(_assets.Enemy(30, 30, 2, first, second, third), 0, new CombatRandom(1));
            int intentChanges = 0;
            enemy.IntentChanged += _ => intentChanges++;

            Assert.AreSame(third, enemy.CurrentMove);
            enemy.TakeTurn(_resolver, _player, _playerSide);
            Assert.AreSame(first, enemy.CurrentMove);
            enemy.TakeTurn(_resolver, _player, _playerSide);
            Assert.AreSame(second, enemy.CurrentMove);
            Assert.AreEqual(2, intentChanges);
        }

        [Test]
        public void TakeTurn_DropsOwnKavachBeforeActing()
        {
            var enemy = new EnemyCombatant(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Block(6))), 0, new CombatRandom(1));
            enemy.Fighter.GainBlock(4);

            enemy.TakeTurn(_resolver, _player, _playerSide);

            Assert.AreEqual(6, enemy.Fighter.Block);
        }

        [Test]
        public void TakeTurn_AttacksThePlayer()
        {
            var enemy = new EnemyCombatant(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7))), 0, new CombatRandom(1));

            Assert.IsTrue(enemy.TakeTurn(_resolver, _player, _playerSide));
            Assert.AreEqual(71, _player.Health);
        }

        [Test]
        public void TakeTurn_WhenFallen_DoesNothing()
        {
            var enemy = new EnemyCombatant(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7)), _assets.Move()), 0, new CombatRandom(1));
            enemy.Fighter.TakeDamage(999);

            Assert.IsFalse(enemy.TakeTurn(_resolver, _player, _playerSide));
            Assert.AreEqual(78, _player.Health);
            Assert.AreEqual(0, enemy.PatternIndex);
        }
    }
}
