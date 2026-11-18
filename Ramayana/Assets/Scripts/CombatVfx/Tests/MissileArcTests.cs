// EditMode tests for MissileArc: a missile starts at its launch point and ends at its landing point, peaks by the arc
// height above the straight line halfway, never leaves the two ends when progress runs outside 0 to 1, and points
// upward on launch, level at the top and downward on landing.
using NUnit.Framework;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.Tests
{
    public sealed class MissileArcTests
    {
        private static readonly Vector3 From = new Vector3(-3f, 1f, 0f);
        private static readonly Vector3 To = new Vector3(3f, 1.4f, 0f);
        private const float Arc = 0.6f;
        private const float Tolerance = 1e-4f;

        [Test]
        public void Starts_At_Launch_And_Ends_At_Landing()
        {
            AssertNear(From, MissileArc.Position(From, To, Arc, 0f));
            AssertNear(To, MissileArc.Position(From, To, Arc, 1f));
        }

        [Test]
        public void Peaks_By_The_Arc_Height_Halfway()
        {
            Vector3 middle = Vector3.Lerp(From, To, 0.5f) + Vector3.up * Arc;

            AssertNear(middle, MissileArc.Position(From, To, Arc, 0.5f));
        }

        [Test]
        public void Progress_Outside_The_Flight_Stays_At_The_Ends()
        {
            AssertNear(From, MissileArc.Position(From, To, Arc, -1f));
            AssertNear(To, MissileArc.Position(From, To, Arc, 2f));
        }

        [Test]
        public void Points_Up_On_Launch_Level_At_The_Top_And_Down_On_Landing()
        {
            Vector3 flat = new Vector3(6f, 0f, 0f);
            Vector3 launch = MissileArc.Direction(From, flat + From, Arc, 0f);
            Vector3 top = MissileArc.Direction(From, flat + From, Arc, 0.5f);
            Vector3 landing = MissileArc.Direction(From, flat + From, Arc, 1f);

            Assert.Greater(launch.y, 0f);
            Assert.AreEqual(0f, top.y, Tolerance);
            Assert.Less(landing.y, 0f);
        }

        private static void AssertNear(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance);
            Assert.AreEqual(expected.y, actual.y, Tolerance);
            Assert.AreEqual(expected.z, actual.z, Tolerance);
        }
    }
}
