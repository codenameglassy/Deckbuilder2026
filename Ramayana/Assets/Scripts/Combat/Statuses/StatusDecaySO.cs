// Strategy for how a status wears off: at the end of its owner's turn, at the end of the round, both, or never (a
// status with no decay asset, such as Dharma, simply stays). Concrete decays are shared, immutable assets.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    public abstract class StatusDecaySO : ScriptableObject
    {
        public virtual void OnOwnerTurnEnd(ref StatusInstance status)
        {
        }

        public virtual void OnRoundEnd(ref StatusInstance status, in StatusTickContext context)
        {
        }
    }
}
