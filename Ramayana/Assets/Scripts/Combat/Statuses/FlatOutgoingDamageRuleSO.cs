// Every hit its holder deals is raised by the status's amount (Focus).
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Statuses/Damage/Flat Outgoing Bonus", fileName = "FlatOutgoingDamageRule")]
    public sealed class FlatOutgoingDamageRuleSO : StatusDamageRuleSO
    {
        public override int ModifyOutgoing(int amount, in StatusInstance status)
        {
            return status.Amount > 0 ? amount + status.Amount : amount;
        }
    }
}
