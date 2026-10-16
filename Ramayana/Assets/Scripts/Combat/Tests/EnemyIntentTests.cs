// EditMode tests for enemy intents: attack, multi-hit and defend moves, damage modifiers changing the preview,
// the intent following the move pattern, and a fallen enemy showing nothing.
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class EnemyIntentTests
    {
        private CombatTestAssets _assets;
        private CombatResolver _resolver;
        private Combatant _player;
        private EnemyCombatant _scout;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _resolver = new CombatResolver();
            _player = new Combatant("player", 78, 78);
            _scout = new EnemyCombatant(_assets.Enemy(30, 30, 0,
                _assets.Move(_assets.Damage(7)),
                _assets.Move(_assets.Damage(4, 2)),
                _assets.Move(_assets.Block(6))), 0, new CombatRandom(1));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void AttackMove_ShowsItsDamage()
        {
            MoveIntent intent = _scout.GetIntent(_resolver, _player);

            Assert.IsTrue(intent.HasAttack);
            Assert.IsFalse(intent.HasBlock);
            Assert.IsFalse(intent.ShowsHits);
            Assert.AreEqual(7, intent.DamagePerHit);
            Assert.AreEqual(7, intent.TotalDamage);
        }

        [Test]
        public void MultiHitMove_ShowsDamagePerHitAndHits()
        {
            _scout.TakeTurn(_resolver, _player, new ICombatant[] { _player });

            MoveIntent intent = _scout.GetIntent(_resolver, _player);

            Assert.IsTrue(intent.ShowsHits);
            Assert.AreEqual(4, intent.DamagePerHit);
            Assert.AreEqual(2, intent.Hits);
            Assert.AreEqual(8, intent.TotalDamage);
        }

        [Test]
        public void DefendMove_ShowsKavachOnly()
        {
            _scout.TakeTurn(_resolver, _player, new ICombatant[] { _player });
            _scout.TakeTurn(_resolver, _player, new ICombatant[] { _player });

            MoveIntent intent = _scout.GetIntent(_resolver, _player);

            Assert.IsFalse(intent.HasAttack);
            Assert.IsTrue(intent.HasBlock);
            Assert.AreEqual(6, intent.Block);
        }

        [Test]
        public void DamageModifiers_ChangeThePreview()
        {
            _resolver.AddModifier(FakeDamageModifier.Add(3));

            MoveIntent intent = _scout.GetIntent(_resolver, _player);

            Assert.AreEqual(10, intent.DamagePerHit);
            Assert.AreEqual(10, intent.TotalDamage);
        }

        [Test]
        public void Intent_FollowsThePatternAndWraps()
        {
            ICombatant[] playerSide = { _player };
            for (int i = 0; i < 3; i++)
            {
                _scout.TakeTurn(_resolver, _player, playerSide);
            }

            MoveIntent intent = _scout.GetIntent(_resolver, _player);

            Assert.IsTrue(intent.HasAttack);
            Assert.AreEqual(7, intent.TotalDamage);
        }

        [Test]
        public void FallenEnemy_HasNoIntent()
        {
            _scout.Fighter.TakeDamage(999);

            Assert.IsTrue(_scout.GetIntent(_resolver, _player).IsEmpty);
        }
    }
}
