// EditMode tests for CombatResolver: base damage, modifier order and the clamp at zero, multi-hit damage through
// Kavach one hit at a time, dead targets, and rejecting combatants it does not own.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatResolverTests
    {
        private CombatTestAssets _assets;
        private CombatResolver _resolver;
        private Combatant _player;
        private Combatant _enemy;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _resolver = new CombatResolver();
            _player = new Combatant("player", 78, 78);
            _enemy = new Combatant("enemy", 30, 30);
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void DealDamage_WithoutModifiers_DealsBaseAmount()
        {
            _resolver.DealDamage(_player, _enemy, 6);

            Assert.AreEqual(24, _enemy.Health);
        }

        [Test]
        public void Modifiers_RunInOrder_AndTheTotalIsClampedAtZero()
        {
            _resolver.AddModifier(FakeDamageModifier.Add(3));
            _resolver.AddModifier(FakeDamageModifier.Percent(50));

            Assert.AreEqual(4, _resolver.CalculateDamage(_player, _enemy, 6));

            _resolver.AddModifier(FakeDamageModifier.Add(-100));
            Assert.AreEqual(0, _resolver.CalculateDamage(_player, _enemy, 6));

            _resolver.DealDamage(_player, _enemy, 6);
            Assert.AreEqual(30, _enemy.Health);
        }

        [Test]
        public void AddModifier_IgnoresDuplicates_AndRemoveModifierWorks()
        {
            FakeDamageModifier bonus = FakeDamageModifier.Add(2);

            _resolver.AddModifier(bonus);
            _resolver.AddModifier(bonus);
            Assert.AreEqual(1, _resolver.ModifierCount);
            Assert.AreEqual(8, _resolver.CalculateDamage(_player, _enemy, 6));

            Assert.IsTrue(_resolver.RemoveModifier(bonus));
            Assert.AreEqual(6, _resolver.CalculateDamage(_player, _enemy, 6));
            Assert.IsFalse(_resolver.RemoveModifier(bonus));
        }

        [Test]
        public void MultiHitDamage_GoesThroughKavachOneHitAtATime()
        {
            _player.GainBlock(5);
            var results = new List<DamageResult>();
            _resolver.DamageDealt += (_, _, result) => results.Add(result);
            DamageEffectSO doubleHit = _assets.Damage(4, 2);

            doubleHit.Apply(new EffectContext(_resolver, _enemy, _player, new ICombatant[] { _player }));

            Assert.AreEqual(0, _player.Block);
            Assert.AreEqual(75, _player.Health);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(4, results[0].Blocked);
            Assert.AreEqual(0, results[0].HealthLost);
            Assert.AreEqual(1, results[1].Blocked);
            Assert.AreEqual(3, results[1].HealthLost);
        }

        [Test]
        public void DealDamage_ToDeadTarget_DoesNothing()
        {
            _enemy.TakeDamage(30);
            int events = 0;
            _resolver.DamageDealt += (_, _, _) => events++;

            _resolver.DealDamage(_player, _enemy, 6);

            Assert.AreEqual(0, events);
        }

        [Test]
        public void Actions_OnForeignCombatant_Throw()
        {
            var foreign = new FakeCombatant("foreign", 30);

            Assert.Throws<InvalidOperationException>(() => _resolver.DealDamage(_player, foreign, 6));
            Assert.Throws<InvalidOperationException>(() => _resolver.GainBlock(foreign, 5));
            Assert.Throws<InvalidOperationException>(() => _resolver.DealDamage(_player, null, 6));
        }
    }
}
