// The only way effects change combat state. The combat model implements it, so damage modifiers (Focus, Bhaya,
// Moha) and Kavach absorption live in one place and every card and enemy move goes through the same rules.
namespace JourneyToLanka.Combat.Effects
{
    public interface ICombatActions
    {
        void DealDamage(ICombatant source, ICombatant target, int baseAmount);

        void GainBlock(ICombatant target, int amount);
    }
}
