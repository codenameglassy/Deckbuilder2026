// Test double for ICombatActions: applies raw damage and Kavach to FakeCombatants and logs every call in order.
// It has no modifiers or Kavach absorption; those belong to the real combat model and its own tests.
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class FakeCombatActions : ICombatActions
    {
        public List<string> Log { get; } = new List<string>();

        public void DealDamage(ICombatant source, ICombatant target, int baseAmount)
        {
            var fake = (FakeCombatant)target;
            fake.Health -= baseAmount;
            Log.Add("damage:" + fake.Name + ":" + baseAmount);
        }

        public void GainBlock(ICombatant target, int amount)
        {
            var fake = (FakeCombatant)target;
            fake.Block += amount;
            Log.Add("block:" + fake.Name + ":" + amount);
        }
    }
}
