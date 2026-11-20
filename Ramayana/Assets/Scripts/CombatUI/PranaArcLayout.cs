// Where each Prana orb sits on the arc around the medallion, as plain C# so it is EditMode tested. Orbs are spread
// evenly around the arc's centre direction, index 0 at the counter-clockwise end (the top, for an arc pointing right),
// so the last orbs, the first to drain, sit at the bottom. When many bonus orbs would wrap past a full circle the step
// shrinks so they share the ring evenly instead of overlapping. Angles are in degrees, 0 pointing right and 90 up;
// positions are offsets from the medallion's centre in canvas units. HalfHeight is how far the orbs reach above or
// below the centre, so the HUD can stack the Prana cluster with even gaps.
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    public static class PranaArcLayout
    {
        private const float FullCircle = 360f;

        public static float Step(int count, float stepDegrees)
        {
            if (count <= 1)
            {
                return 0f;
            }

            return Mathf.Min(Mathf.Max(stepDegrees, 0f), FullCircle / count);
        }

        public static float Angle(int index, int count, float stepDegrees, float centreDegrees)
        {
            return centreDegrees + Step(count, stepDegrees) * ((count - 1) * 0.5f - index);
        }

        public static Vector2 Position(int index, int count, float radius, float stepDegrees, float centreDegrees)
        {
            float radians = Angle(index, count, stepDegrees, centreDegrees) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        public static float HalfHeight(int count, float radius, float stepDegrees, float centreDegrees, float orbDiameter)
        {
            if (count <= 0)
            {
                return 0f;
            }

            float reach = 0f;
            for (int i = 0; i < count; i++)
            {
                reach = Mathf.Max(reach, Mathf.Abs(Position(i, count, radius, stepDegrees, centreDegrees).y));
            }

            return reach + orbDiameter * 0.5f;
        }
    }
}
