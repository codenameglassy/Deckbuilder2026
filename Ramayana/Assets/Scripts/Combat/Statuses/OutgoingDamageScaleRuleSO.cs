// While the status lasts, every hit its holder deals is scaled by a percentage, rounded down (Moha: 75%). The size of
// the status does not matter, only that it is there, so it never stacks into a bigger multiplier.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Statuses/Damage/Outgoing Scale", fileName = "OutgoingDamageScaleRule")]
    public sealed class OutgoingDamageScaleRuleSO : StatusDamageRuleSO
    {
        [SerializeField, Range(0, 300)] private int _percent = 75;

        public int Percent => _percent;

        public override int ModifyOutgoing(int amount, in StatusInstance status)
        {
            return status.Amount > 0 ? amount * _percent / 100 : amount;
        }
    }
}
