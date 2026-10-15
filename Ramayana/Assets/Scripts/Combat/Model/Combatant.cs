// Health and Kavach for one fighter, player or enemy. Kavach absorbs damage before health, each hit separately.
// Effects never call this directly; they go through CombatResolver so modifiers apply first.
// Events fire in a fixed order on a hit: BlockChanged (if Kavach absorbed any), Damaged, then Died on the killing blow.
using System;
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Model
{
    public sealed class Combatant : ICombatant
    {
        public Combatant(string nameKey, int maxHealth, int health)
        {
            if (maxHealth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be at least 1.");
            }

            NameKey = nameKey;
            MaxHealth = maxHealth;
            Health = Math.Clamp(health, 0, maxHealth);
        }

        public event Action<Combatant> BlockChanged;

        public event Action<Combatant, DamageResult> Damaged;

        public event Action<Combatant> Died;

        public string NameKey { get; }

        public int MaxHealth { get; }

        public int Health { get; private set; }

        public int Block { get; private set; }

        public bool IsAlive => Health > 0;

        public DamageResult TakeDamage(int amount)
        {
            if (!IsAlive || amount < 0)
            {
                return default;
            }

            int blocked = Math.Min(Block, amount);
            int healthLost = Math.Min(Health, amount - blocked);
            Block -= blocked;
            Health -= healthLost;
            bool killed = healthLost > 0 && Health == 0;
            var result = new DamageResult(amount, blocked, healthLost, killed);

            if (blocked > 0)
            {
                BlockChanged?.Invoke(this);
            }

            Damaged?.Invoke(this, result);

            if (killed)
            {
                Died?.Invoke(this);
            }

            return result;
        }

        public void GainBlock(int amount)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            Block += amount;
            BlockChanged?.Invoke(this);
        }

        public void ClearBlock()
        {
            if (Block == 0)
            {
                return;
            }

            Block = 0;
            BlockChanged?.Invoke(this);
        }
    }
}
