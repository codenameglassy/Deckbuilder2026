// Test double for ICombatant: a named fighter with plain health and Kavach values that FakeCombatActions changes.
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class FakeCombatant : ICombatant
    {
        public FakeCombatant(string name, int health)
        {
            Name = name;
            Health = health;
        }

        public string Name { get; }

        public int Health { get; set; }

        public int Block { get; set; }

        public bool IsAlive => Health > 0;
    }
}
