// A rule that changes outgoing damage before Kavach, such as Focus, Bhaya or Moha. CombatResolver runs modifiers
// in the order they were added, so flat bonuses must be added before multipliers. Each modifier does its own
// rounding (the reference build floors) and may return any value; the resolver clamps the final total at zero.
// Previews may pass a null target (card text before an enemy is chosen), so a modifier that depends on the target
// must treat null as "no target-specific change". A modifier whose numbers change while it stays registered
// (stacks gained or lost) calls CombatResolver.NotifyModifiersChanged so previews refresh.
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Model
{
    public interface IDamageModifier
    {
        int ModifyDamage(ICombatant source, ICombatant target, int amount);
    }
}
