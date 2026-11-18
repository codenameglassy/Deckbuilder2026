// Test double for ICombatActions: applies raw damage, Kavach, healing and health loss to FakeCombatants and logs
// every call in order, statuses included. It has no modifiers, Kavach absorption or status rules; those belong to the
// real combat model and its own tests. GetStatusAmount answers from StatusAmounts, whoever is asked about.
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Statuses;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class FakeCombatActions : ICombatActions
    {
        public List<string> Log { get; } = new List<string>();

        public Dictionary<StatusDefinitionSO, int> StatusAmounts { get; } = new Dictionary<StatusDefinitionSO, int>();

        public int MaxHealth { get; set; } = 100;

        public void DealDamage(ICombatant source, ICombatant target, int baseAmount)
        {
            var fake = (FakeCombatant)target;
            fake.Health -= baseAmount;
            Log.Add("damage:" + fake.Name + ":" + baseAmount);
        }

        public void DealUnmodifiedDamage(ICombatant source, ICombatant target, int amount)
        {
            var fake = (FakeCombatant)target;
            fake.Health -= amount;
            Log.Add("unmodified:" + fake.Name + ":" + amount);
        }

        public void RetainBlock(ICombatant target)
        {
            var fake = (FakeCombatant)target;
            Log.Add("retain:" + fake.Name);
        }

        public void RemoveBlock(ICombatant target)
        {
            var fake = (FakeCombatant)target;
            fake.Block = 0;
            Log.Add("strip:" + fake.Name);
        }

        public void Heal(ICombatant target, int amount)
        {
            var fake = (FakeCombatant)target;
            fake.Health += amount;
            Log.Add("heal:" + fake.Name + ":" + amount);
        }

        public int GetStatusAmount(ICombatant target, StatusDefinitionSO status)
        {
            return status != null && StatusAmounts.TryGetValue(status, out int amount) ? amount : 0;
        }

        public int GetMaxHealth(ICombatant target)
        {
            return MaxHealth;
        }

        public void GainBlock(ICombatant target, int amount)
        {
            var fake = (FakeCombatant)target;
            fake.Block += amount;
            Log.Add("block:" + fake.Name + ":" + amount);
        }

        public void ApplyStatus(ICombatant target, StatusDefinitionSO status, int amount, int duration)
        {
            var fake = (FakeCombatant)target;
            Log.Add("status:" + fake.Name + ":" + (status != null ? status.name : "none") + ":" + amount + ":" + duration);
        }

        public void LoseHealth(ICombatant target, int amount)
        {
            var fake = (FakeCombatant)target;
            fake.Health -= amount;
            Log.Add("lose:" + fake.Name + ":" + amount);
        }
    }
}
