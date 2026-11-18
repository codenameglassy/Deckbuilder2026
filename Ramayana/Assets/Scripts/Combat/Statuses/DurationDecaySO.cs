// The status lasts a number of its owner's turns separate from its amount, and vanishes when they run out (Focus). A
// player's Focus is applied for one turn, so it ends with the turn it was gained; an enemy's for two, so it survives
// the action that raised it and ends after its next one.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Statuses/Decay/Duration", fileName = "DurationDecay")]
    public sealed class DurationDecaySO : StatusDecaySO
    {
        public override void OnOwnerTurnEnd(ref StatusInstance status)
        {
            status.Duration--;
            if (status.Duration <= 0)
            {
                status.Amount = 0;
                status.Duration = 0;
            }
        }
    }
}
