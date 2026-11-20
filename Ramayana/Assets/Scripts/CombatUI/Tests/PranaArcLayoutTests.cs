// EditMode tests for PranaArcLayout: the arc is centred on its direction, runs from the top down for an arc pointing
// right, keeps its step until the orbs would wrap past a full circle, places each orb at the arc's radius, and reports
// how far the orbs reach above and below the centre.
using NUnit.Framework;
using UnityEngine;

namespace JourneyToLanka.CombatUI.Tests
{
    public sealed class PranaArcLayoutTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void ThreeOrbs_AreCentredOnTheArcDirection_TopFirst()
        {
            Assert.AreEqual(24f, PranaArcLayout.Angle(0, 3, 24f, 0f), Tolerance);
            Assert.AreEqual(0f, PranaArcLayout.Angle(1, 3, 24f, 0f), Tolerance);
            Assert.AreEqual(-24f, PranaArcLayout.Angle(2, 3, 24f, 0f), Tolerance);
        }

        [Test]
        public void EvenCount_StraddlesTheArcDirection()
        {
            Assert.AreEqual(100f, PranaArcLayout.Angle(0, 2, 20f, 90f), Tolerance);
            Assert.AreEqual(80f, PranaArcLayout.Angle(1, 2, 20f, 90f), Tolerance);
        }

        [Test]
        public void SingleOrb_SitsOnTheArcDirection()
        {
            Assert.AreEqual(0f, PranaArcLayout.Step(1, 24f), Tolerance);
            Assert.AreEqual(45f, PranaArcLayout.Angle(0, 1, 24f, 45f), Tolerance);
        }

        [Test]
        public void ManyOrbs_ShrinkTheStep_SoTheyNeverWrapPastAFullCircle()
        {
            Assert.AreEqual(24f, PranaArcLayout.Step(10, 24f), Tolerance);
            Assert.AreEqual(18f, PranaArcLayout.Step(20, 24f), Tolerance);
            Assert.AreEqual(0f, PranaArcLayout.Step(5, -10f), Tolerance);
        }

        [Test]
        public void Position_LiesOnTheRadius_AtTheOrbsAngle()
        {
            Vector2 middle = PranaArcLayout.Position(1, 3, 50f, 30f, 0f);
            Assert.AreEqual(50f, middle.x, Tolerance);
            Assert.AreEqual(0f, middle.y, Tolerance);

            Vector2 top = PranaArcLayout.Position(0, 3, 50f, 30f, 0f);
            Assert.AreEqual(50f, top.magnitude, Tolerance);
            Assert.AreEqual(25f, top.y, Tolerance);
            Assert.Greater(top.x, 0f);
        }

        [Test]
        public void HalfHeight_IsTheHighestOrbPlusHalfAnOrb()
        {
            Assert.AreEqual(35f, PranaArcLayout.HalfHeight(3, 50f, 30f, 0f, 20f), Tolerance);
            Assert.AreEqual(10f, PranaArcLayout.HalfHeight(1, 50f, 30f, 0f, 20f), Tolerance);
            Assert.AreEqual(60f, PranaArcLayout.HalfHeight(1, 50f, 30f, 90f, 20f), Tolerance);
            Assert.AreEqual(0f, PranaArcLayout.HalfHeight(0, 50f, 30f, 0f, 20f), Tolerance);
        }
    }
}
