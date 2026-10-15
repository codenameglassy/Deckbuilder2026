// EditMode tests for PranaPool: refill without carry-over, spending limits, and change notifications.
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class PranaPoolTests
    {
        [Test]
        public void Refill_SetsPerTurnAmount_WithoutCarryOver()
        {
            var prana = new PranaPool(3);

            prana.Refill();
            Assert.AreEqual(3, prana.Current);

            prana.Gain(2);
            prana.Refill();
            Assert.AreEqual(3, prana.Current);
        }

        [Test]
        public void TrySpend_RejectsOverspendAndNegativeAmounts()
        {
            var prana = new PranaPool(3);
            prana.Refill();

            Assert.IsFalse(prana.TrySpend(4));
            Assert.IsFalse(prana.TrySpend(-1));
            Assert.AreEqual(3, prana.Current);

            Assert.IsTrue(prana.TrySpend(1));
            Assert.IsTrue(prana.TrySpend(2));
            Assert.AreEqual(0, prana.Current);
            Assert.IsTrue(prana.CanSpend(0));
            Assert.IsFalse(prana.CanSpend(1));
        }

        [Test]
        public void Changed_FiresOnlyWhenTheValueMoves()
        {
            var prana = new PranaPool(3);
            int changes = 0;
            prana.Changed += _ => changes++;

            prana.Refill();
            prana.Refill();
            prana.TrySpend(0);
            prana.Gain(0);
            Assert.AreEqual(1, changes);

            prana.TrySpend(2);
            prana.Gain(1);
            Assert.AreEqual(3, changes);
        }
    }
}
