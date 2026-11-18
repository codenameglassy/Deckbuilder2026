// EditMode tests for ChainMultiplierFormat: whole percents become multipliers with no trailing zeros (100 -> 1,
// 150 -> 1.5, 125 -> 1.25, 105 -> 1.05, 300 -> 3), the placeholder is filled wherever it appears, a missing pattern
// writes the bare multiplier, and a negative percent writes 0.
using System.Text;
using NUnit.Framework;

namespace JourneyToLanka.CombatUI.Tests
{
    public sealed class ChainMultiplierFormatTests
    {
        private readonly StringBuilder _output = new StringBuilder();

        private string Format(string pattern, int percent)
        {
            ChainMultiplierFormat.Format(pattern, percent, _output);
            return _output.ToString();
        }

        [Test]
        public void Percents_BecomeMultipliers_WithoutTrailingZeros()
        {
            Assert.AreEqual("x1", Format("x{0}", 100));
            Assert.AreEqual("x1.5", Format("x{0}", 150));
            Assert.AreEqual("x2", Format("x{0}", 200));
            Assert.AreEqual("x3", Format("x{0}", 300));
            Assert.AreEqual("x1.25", Format("x{0}", 125));
            Assert.AreEqual("x1.05", Format("x{0}", 105));
            Assert.AreEqual("x0.5", Format("x{0}", 50));
        }

        [Test]
        public void Placeholder_IsFilledWhereverItAppears_AndOtherTextIsKept()
        {
            Assert.AreEqual("1.5x", Format("{0}x", 150));
            Assert.AreEqual("x2 (2)", Format("x{0} ({0})", 200));
            Assert.AreEqual("{1} x1.5", Format("{1} x{0}", 150));
        }

        [Test]
        public void MissingPattern_WritesTheBareMultiplier_AndNegativeIsZero()
        {
            Assert.AreEqual("1.5", Format(null, 150));
            Assert.AreEqual("2", Format(string.Empty, 200));
            Assert.AreEqual("x0", Format("x{0}", -40));
        }
    }
}
