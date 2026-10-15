// Test double for IDamageModifier: adds a flat amount, then scales by a percentage with integer (floor) division.
// Lets resolver tests prove modifier order and the clamp at zero without real status effects.
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class FakeDamageModifier : IDamageModifier
    {
        private readonly int _flat;
        private readonly int _percent;

        private FakeDamageModifier(int flat, int percent)
        {
            _flat = flat;
            _percent = percent;
        }

        public int CallCount { get; private set; }

        public static FakeDamageModifier Add(int flat)
        {
            return new FakeDamageModifier(flat, 100);
        }

        public static FakeDamageModifier Percent(int percent)
        {
            return new FakeDamageModifier(0, percent);
        }

        public int ModifyDamage(ICombatant source, ICombatant target, int amount)
        {
            CallCount++;
            return (amount + _flat) * _percent / 100;
        }
    }
}
