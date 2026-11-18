// The status's amount is the number of rounds it lasts; it loses one at the end of every round (Bhaya, Moha).
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Statuses/Decay/Turn Timer", fileName = "TurnTimerDecay")]
    public sealed class TurnTimerDecaySO : StatusDecaySO
    {
        public override void OnRoundEnd(ref StatusInstance status, in StatusTickContext context)
        {
            if (status.Amount > 0)
            {
                status.Amount--;
            }
        }
    }
}
