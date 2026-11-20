// EditMode tests for PileStackMath: an empty pile shows no cards, the stack gains a card per batch of cards, it never
// exceeds the cap, and a batch size below one is treated as one.
using NUnit.Framework;

namespace JourneyToLanka.CombatUI.Tests
{
    public sealed class PileStackMathTests
    {
        [Test]
        public void EmptyPile_ShowsNoCards()
        {
            Assert.AreEqual(0, PileStackMath.LayerCount(0, 4, 5));
            Assert.AreEqual(0, PileStackMath.LayerCount(-3, 4, 5));
            Assert.AreEqual(0, PileStackMath.LayerCount(7, 0, 5));
        }

        [Test]
        public void Stack_GainsACardPerBatch()
        {
            Assert.AreEqual(1, PileStackMath.LayerCount(1, 4, 5));
            Assert.AreEqual(1, PileStackMath.LayerCount(5, 4, 5));
            Assert.AreEqual(2, PileStackMath.LayerCount(6, 4, 5));
            Assert.AreEqual(3, PileStackMath.LayerCount(15, 4, 5));
        }

        [Test]
        public void Stack_NeverExceedsTheCap()
        {
            Assert.AreEqual(4, PileStackMath.LayerCount(16, 4, 5));
            Assert.AreEqual(4, PileStackMath.LayerCount(200, 4, 5));
        }

        [Test]
        public void BatchSizeBelowOne_CountsEveryCard()
        {
            Assert.AreEqual(3, PileStackMath.LayerCount(3, 6, 0));
        }
    }
}
