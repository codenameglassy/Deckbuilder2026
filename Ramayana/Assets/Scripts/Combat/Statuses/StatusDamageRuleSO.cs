// Strategy for how a status changes damage: what it does to damage its holder deals (outgoing) and to damage its
// holder takes (incoming). Order decides the sequence when several apply: flat bonuses first, then scaling, as in the
// reference build (Focus, then Moha, then Bhaya). Concrete rules are shared, immutable assets.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    public abstract class StatusDamageRuleSO : ScriptableObject
    {
        [SerializeField] private int _order;

        public int Order => _order;

        public virtual int ModifyOutgoing(int amount, in StatusInstance status)
        {
            return amount;
        }

        public virtual int ModifyIncoming(int amount, in StatusInstance status)
        {
            return amount;
        }
    }
}
