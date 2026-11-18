// The only way effects change combat state. The combat model implements it, so damage modifiers (Focus, Bhaya,
// Moha), Kavach absorption and statuses live in one place and every card and enemy move goes through the same rules.
// DealDamage with a final percent applies that percent after every damage modifier and rounds down (the combo
// chain's multiplier); the three-argument DealDamage is the same at 100.
// DealUnmodifiedDamage still meets Kavach but skips every modifier (Brahmastra's spent Dharma). RetainBlock keeps the
// target's Kavach through its next turn start once; RemoveBlock strips all of it (Vayavyastra). ApplyStatus adds to a status, or spends it with a negative amount
// (a duration of 0 uses the status's own default); LoseHealth takes health straight past Kavach and every modifier,
// as Dahan's burn does. GetStatusAmount and GetMaxHealth are the reads: effects use them for conditions, costs
// (Dharma) and sizes (the Sanjivani Arrow heals a share of maximum health), and a preview answers them from its own
// simulation.
using JourneyToLanka.Combat.Statuses;

namespace JourneyToLanka.Combat.Effects
{
    public interface ICombatActions
    {
        void DealDamage(ICombatant source, ICombatant target, int baseAmount);

        void DealDamage(ICombatant source, ICombatant target, int baseAmount, int finalPercent);

        void DealUnmodifiedDamage(ICombatant source, ICombatant target, int amount);

        void GainBlock(ICombatant target, int amount);

        void RetainBlock(ICombatant target);

        void RemoveBlock(ICombatant target);

        void Heal(ICombatant target, int amount);

        void ApplyStatus(ICombatant target, StatusDefinitionSO status, int amount, int duration);

        void LoseHealth(ICombatant target, int amount);

        int GetStatusAmount(ICombatant target, StatusDefinitionSO status);

        int GetMaxHealth(ICombatant target);
    }
}
