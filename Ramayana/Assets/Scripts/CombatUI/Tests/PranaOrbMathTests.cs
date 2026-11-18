// EditMode tests for PranaOrbMath: the orb count follows the per-turn amount, grows with bonus Prana and respects
// the cap; a card previews the rightmost lit orbs it would spend, and the orbs it lacks as the deficit.
using NUnit.Framework;

namespace JourneyToLanka.CombatUI.Tests
{
    public sealed class PranaOrbMathTests
    {
        [Test]
        public void OrbCount_FollowsPerTurn_GrowsWithBonus_AndRespectsTheCap()
        {
            Assert.AreEqual(3, PranaOrbMath.OrbCount(3, 0, 10));
            Assert.AreEqual(3, PranaOrbMath.OrbCount(3, 2, 10));
            Assert.AreEqual(5, PranaOrbMath.OrbCount(3, 5, 10));
            Assert.AreEqual(10, PranaOrbMath.OrbCount(3, 14, 10));
            Assert.AreEqual(0, PranaOrbMath.OrbCount(3, 3, -1));
        }

        [Test]
        public void AffordableCard_PreviewsTheRightmostLitOrbs()
        {
            Assert.AreEqual(PranaOrbPreview.None, PranaOrbMath.Preview(0, 3, 2));
            Assert.AreEqual(PranaOrbPreview.Spend, PranaOrbMath.Preview(1, 3, 2));
            Assert.AreEqual(PranaOrbPreview.Spend, PranaOrbMath.Preview(2, 3, 2));
            Assert.AreEqual(PranaOrbPreview.None, PranaOrbMath.Preview(3, 3, 2));
        }

        [Test]
        public void UnaffordableCard_SpendsEveryLitOrb_AndMarksTheMissingOnes()
        {
            Assert.AreEqual(PranaOrbPreview.Spend, PranaOrbMath.Preview(0, 1, 3));
            Assert.AreEqual(PranaOrbPreview.Deficit, PranaOrbMath.Preview(1, 1, 3));
            Assert.AreEqual(PranaOrbPreview.Deficit, PranaOrbMath.Preview(2, 1, 3));
            Assert.AreEqual(PranaOrbPreview.None, PranaOrbMath.Preview(3, 1, 3));
        }

        [Test]
        public void FreeCardsAndInvalidIndices_PreviewNothing()
        {
            Assert.AreEqual(PranaOrbPreview.None, PranaOrbMath.Preview(2, 3, 0));
            Assert.AreEqual(PranaOrbPreview.None, PranaOrbMath.Preview(0, 3, -1));
            Assert.AreEqual(PranaOrbPreview.None, PranaOrbMath.Preview(-1, 3, 2));
        }
    }
}
