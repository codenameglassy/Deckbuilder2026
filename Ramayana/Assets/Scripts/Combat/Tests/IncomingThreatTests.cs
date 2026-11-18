// EditMode tests for the threat panel's numbers: attacks from every living enemy add up, multi-hit moves count
// every hit, Kavach absorbs up to the incoming total, fallen enemies and defend moves add nothing, damage modifiers
// change the preview, and the result is lethal only when the damage taken reaches Rama's current health.
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class IncomingThreatTests
    {
        private CombatTestAssets _assets;
        private CombatResolver _resolver;
        private Combatant _player;
        private EnemyCombatant _scout;
        private EnemyCombatant _hound;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _resolver = new CombatResolver();
            _player = new Combatant("player", 78, 78);
            _scout = new EnemyCombatant(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7))), 0, new CombatRandom(1));
            _hound = new EnemyCombatant(_assets.Enemy(20, 20, 0, _assets.Move(_assets.Damage(4, 2))), 1, new CombatRandom(2));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void AttacksFromAllLivingEnemies_AddUp()
        {
            IncomingThreat threat = Calculate(_scout, _hound);

            Assert.IsTrue(threat.HasAttack);
            Assert.AreEqual(15, threat.Incoming);
            Assert.AreEqual(0, threat.Blocked);
            Assert.AreEqual(15, threat.Taken);
        }

        [Test]
        public void Kavach_AbsorbsPartOfTheIncomingDamage()
        {
            _player.GainBlock(5);

            IncomingThreat threat = Calculate(_scout, _hound);

            Assert.AreEqual(15, threat.Incoming);
            Assert.AreEqual(5, threat.Blocked);
            Assert.AreEqual(10, threat.Taken);
        }

        [Test]
        public void KavachAboveIncoming_AbsorbsOnlyTheIncomingDamage()
        {
            _player.GainBlock(20);

            IncomingThreat threat = Calculate(_scout, _hound);

            Assert.AreEqual(15, threat.Blocked);
            Assert.AreEqual(0, threat.Taken);
            Assert.IsFalse(threat.IsLethal);
        }

        [Test]
        public void FallenEnemy_AddsNothing()
        {
            _scout.Fighter.TakeDamage(999);

            IncomingThreat threat = Calculate(_scout, _hound);

            Assert.AreEqual(8, threat.Incoming);
        }

        [Test]
        public void DefendMove_AddsNothing()
        {
            var defender = new EnemyCombatant(_assets.Enemy(25, 25, 0, _assets.Move(_assets.Block(6))), 0, new CombatRandom(3));

            IncomingThreat threat = Calculate(defender);

            Assert.IsFalse(threat.HasAttack);
            Assert.AreEqual(0, threat.Incoming);
            Assert.AreEqual(0, threat.Taken);
        }

        [Test]
        public void DamageModifiers_ChangeThePreview()
        {
            _resolver.AddModifier(FakeDamageModifier.Add(3));

            IncomingThreat threat = Calculate(_scout);

            Assert.AreEqual(10, threat.Incoming);
        }

        [Test]
        public void Lethal_WhenDamageTakenReachesCurrentHealth()
        {
            var wounded = new Combatant("player", 78, 15);

            IncomingThreat exact = IncomingThreat.Calculate(new[] { _scout, _hound }, _resolver, wounded);
            Assert.IsTrue(exact.IsLethal);

            wounded.GainBlock(1);
            IncomingThreat survives = IncomingThreat.Calculate(new[] { _scout, _hound }, _resolver, wounded);
            Assert.IsFalse(survives.IsLethal);
        }

        [Test]
        public void NoAttack_IsNeverLethal()
        {
            var threat = new IncomingThreat(0, 0, 1);

            Assert.IsFalse(threat.IsLethal);
        }

        private IncomingThreat Calculate(params EnemyCombatant[] enemies)
        {
            return IncomingThreat.Calculate(enemies, _resolver, _player);
        }
    }
}
