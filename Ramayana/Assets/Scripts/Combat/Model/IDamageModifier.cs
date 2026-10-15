// A rule that changes outgoing damage before Kavach, such as Focus, Bhaya or Moha. CombatResolver runs modifiers
// in the order they were added, so flat bonuses must be added before multipliers. Each modifier does its own
// rounding (the reference build floors) and may return any value; the resolver clamps the final total at zero.
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Model
{
    public interface IDamageModifier
    {
        int ModifyDamage(ICombatant source, ICombatant target, int amount);
    }
}
