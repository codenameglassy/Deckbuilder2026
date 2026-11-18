// Health, Kavach and statuses for one fighter, player or enemy. Kavach absorbs damage before health, each hit
// separately; LoseHealth skips Kavach entirely (Dahan's burn). Effects never call this directly; they go through
// CombatResolver so modifiers apply first. Events fire in a fixed order on a hit: BlockChanged (if Kavach absorbed
// any), Damaged, then Died on the killing blow. A fighter's statuses are cleared when it falls, just before Died.
// ApplyStatus changes a status and raises StatusApplied with the amount asked for (negative when spent), so views can
// announce it; statuses wearing off at turn and round ends change the set without it.
// Heal restores health up to the maximum and raises Healed with the amount restored. RetainBlockOnce marks the
// current Kavach to survive the next ClearBlockAtTurnStart, which then clears the mark instead of the Kavach.
using System;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Statuses;

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

        // Fighter, health restored.
        public event Action<Combatant, int> Healed;

        // Fighter, status, amount applied (negative when spent).
        public event Action<Combatant, StatusDefinitionSO, int> StatusApplied;

        public string NameKey { get; }

        public int MaxHealth { get; }

        public int Health { get; private set; }

        public int Block { get; private set; }

        public StatusSet Statuses { get; } = new StatusSet();

        public bool RetainsBlock { get; private set; }

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
                Fall();
            }

            return result;
        }

        public DamageResult LoseHealth(int amount)
        {
            if (!IsAlive || amount <= 0)
            {
                return default;
            }

            int healthLost = Math.Min(Health, amount);
            Health -= healthLost;
            bool killed = Health == 0;
            var result = new DamageResult(amount, 0, healthLost, killed);

            Damaged?.Invoke(this, result);

            if (killed)
            {
                Fall();
            }

            return result;
        }

        public void ApplyStatus(StatusDefinitionSO status, int amount, int duration)
        {
            if (!IsAlive || status == null || amount == 0)
            {
                return;
            }

            Statuses.Apply(status, amount, duration);
            StatusApplied?.Invoke(this, status, amount);
        }

        public int Heal(int amount)
        {
            if (!IsAlive || amount <= 0)
            {
                return 0;
            }

            int restored = Math.Min(MaxHealth - Health, amount);
            if (restored <= 0)
            {
                return 0;
            }

            Health += restored;
            Healed?.Invoke(this, restored);
            return restored;
        }

        public void RetainBlockOnce()
        {
            if (IsAlive)
            {
                RetainsBlock = true;
            }
        }

        public void ClearBlockAtTurnStart()
        {
            if (RetainsBlock)
            {
                RetainsBlock = false;
                return;
            }

            ClearBlock();
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

        private void Fall()
        {
            Statuses.Clear();
            Died?.Invoke(this);
        }
    }
}
