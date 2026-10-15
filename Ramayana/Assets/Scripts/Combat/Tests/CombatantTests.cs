// EditMode tests for Combatant: Kavach absorbs before health, full blocks, overkill clamping, a single death event,
// and Kavach gain and clear.
using System.Collections.Generic;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatantTests
    {
        [Test]
        public void TakeDamage_WithoutKavach_LosesHealth()
        {
            var fighter = new Combatant("test", 30, 30);

            DamageResult result = fighter.TakeDamage(6);

            Assert.AreEqual(24, fighter.Health);
            Assert.AreEqual(6, result.Amount);
            Assert.AreEqual(0, result.Blocked);
            Assert.AreEqual(6, result.HealthLost);
            Assert.IsFalse(result.Killed);
        }

        [Test]
        public void TakeDamage_KavachAbsorbsFirst_ThenHealth()
        {
            var fighter = new Combatant("test", 30, 30);
            fighter.GainBlock(5);

            DamageResult result = fighter.TakeDamage(8);

            Assert.AreEqual(5, result.Blocked);
            Assert.AreEqual(3, result.HealthLost);
            Assert.AreEqual(0, fighter.Block);
            Assert.AreEqual(27, fighter.Health);
        }

        [Test]
        public void TakeDamage_FullyBlocked_RaisesEventsInOrder()
        {
            var fighter = new Combatant("test", 30, 30);
            fighter.GainBlock(8);
            var events = new List<string>();
            fighter.BlockChanged += _ => events.Add("block");
            fighter.Damaged += (_, _) => events.Add("damaged");
            fighter.Died += _ => events.Add("died");

            DamageResult result = fighter.TakeDamage(6);

            Assert.IsTrue(result.FullyBlocked);
            Assert.AreEqual(2, fighter.Block);
            Assert.AreEqual(30, fighter.Health);
            CollectionAssert.AreEqual(new[] { "block", "damaged" }, events);
        }

        [Test]
        public void TakeDamage_Overkill_ClampsHealthAndDiesOnce()
        {
            var fighter = new Combatant("test", 30, 5);
            int deaths = 0;
            int hits = 0;
            fighter.Died += _ => deaths++;
            fighter.Damaged += (_, _) => hits++;

            DamageResult killing = fighter.TakeDamage(9);
            DamageResult afterDeath = fighter.TakeDamage(9);

            Assert.AreEqual(0, fighter.Health);
            Assert.AreEqual(5, killing.HealthLost);
            Assert.IsTrue(killing.Killed);
            Assert.IsFalse(fighter.IsAlive);
            Assert.AreEqual(0, afterDeath.Amount);
            Assert.AreEqual(1, deaths);
            Assert.AreEqual(1, hits);
        }

        [Test]
        public void Kavach_Stacks_ClearsAndIsIgnoredWhenDead()
        {
            var fighter = new Combatant("test", 30, 30);

            fighter.GainBlock(5);
            fighter.GainBlock(8);
            Assert.AreEqual(13, fighter.Block);

            fighter.ClearBlock();
            Assert.AreEqual(0, fighter.Block);

            fighter.TakeDamage(30);
            fighter.GainBlock(5);
            Assert.AreEqual(0, fighter.Block);
        }

        [Test]
        public void Constructor_ClampsStartingHealthToMax()
        {
            var fighter = new Combatant("test", 78, 999);

            Assert.AreEqual(78, fighter.Health);
            Assert.AreEqual(78, fighter.MaxHealth);
        }
    }
}
