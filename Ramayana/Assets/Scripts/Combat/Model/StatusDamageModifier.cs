// Feeds every status into the resolver's damage pipeline as one modifier: the attacker's statuses change what it
// deals (Focus adds, then Moha scales), then the target's change what it takes (Bhaya scales), each status through
// its own damage rule, in rule order. The combat model adds it first, so it runs before any later modifier.
// While a card is previewed, Overlay supplies the preview's copies of any statuses the card has changed so far, so
// a Bhaya applied earlier in the card shows in the hit that follows; otherwise each fighter's real statuses are read.
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Statuses;

namespace JourneyToLanka.Combat.Model
{
    public sealed class StatusDamageModifier : IDamageModifier
    {
        public IStatusOverlay Overlay { get; set; }

        public int ModifyDamage(ICombatant source, ICombatant target, int amount)
        {
            StatusSet outgoing = Find(source);
            if (outgoing != null)
            {
                amount = outgoing.ModifyOutgoing(amount);
            }

            StatusSet incoming = Find(target);
            if (incoming != null)
            {
                amount = incoming.ModifyIncoming(amount);
            }

            return amount;
        }

        private StatusSet Find(ICombatant combatant)
        {
            if (combatant == null)
            {
                return null;
            }

            if (Overlay != null && Overlay.TryGetStatuses(combatant, out StatusSet overlay))
            {
                return overlay;
            }

            return combatant is Combatant fighter ? fighter.Statuses : null;
        }
    }
}
