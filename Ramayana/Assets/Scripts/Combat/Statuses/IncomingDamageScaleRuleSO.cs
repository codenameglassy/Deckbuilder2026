// While the status lasts, every hit its holder takes is scaled by a percentage, rounded down (Bhaya: 125%). The size
// of the status does not matter, only that it is there, so it never stacks into a bigger multiplier.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Statuses/Damage/Incoming Scale", fileName = "IncomingDamageScaleRule")]
    public sealed class IncomingDamageScaleRuleSO : StatusDamageRuleSO
    {
        [SerializeField, Range(0, 300)] private int _percent = 125;

        public int Percent => _percent;

        public override int ModifyIncoming(int amount, in StatusInstance status)
        {
            return status.Amount > 0 ? amount * _percent / 100 : amount;
        }
    }
}
