// The path a flying missile follows: a straight line from launch to landing lifted by a parabola that peaks at the
// arc height halfway, so it rises out of the bow and drops onto the target. Position and Direction take the flight's
// progress from 0 at launch to 1 at landing (clamped); Direction is the unnormalized way the missile points, never
// zero while the two ends differ. Pure math with no state, so it is unit tested.
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    public static class MissileArc
    {
        public static Vector3 Position(Vector3 from, Vector3 to, float arc, float progress)
        {
            float t = Mathf.Clamp01(progress);
            return Vector3.LerpUnclamped(from, to, t) + Vector3.up * (arc * 4f * t * (1f - t));
        }

        public static Vector3 Direction(Vector3 from, Vector3 to, float arc, float progress)
        {
            float t = Mathf.Clamp01(progress);
            return (to - from) + Vector3.up * (arc * 4f * (1f - 2f * t));
        }
    }
}
