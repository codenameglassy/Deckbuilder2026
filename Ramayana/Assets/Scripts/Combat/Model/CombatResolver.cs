// The combat model's implementation of ICombatActions, so every card and enemy move resolves through one place:
// base damage, then damage modifiers in order, then Kavach, then health. CalculateDamage is also what card and
// intent previews call, so a shown number always matches what lands. DamageDealt reports the source as well,
// for later rules such as thorns or lifesteal. It also serves IDamagePreview for enemy intents.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Model
{
    public sealed class CombatResolver : ICombatActions, IDamagePreview
    {
        private readonly List<IDamageModifier> _modifiers = new List<IDamageModifier>(8);

        // Source, target, what the hit did.
        public event Action<ICombatant, Combatant, DamageResult> DamageDealt;

        public int ModifierCount => _modifiers.Count;

        public void AddModifier(IDamageModifier modifier)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }

            if (!_modifiers.Contains(modifier))
            {
                _modifiers.Add(modifier);
            }
        }

        public bool RemoveModifier(IDamageModifier modifier)
        {
            return modifier != null && _modifiers.Remove(modifier);
        }

        public int CalculateDamage(ICombatant source, ICombatant target, int baseAmount)
        {
            int amount = baseAmount;
            for (int i = 0; i < _modifiers.Count; i++)
            {
                amount = _modifiers[i].ModifyDamage(source, target, amount);
            }

            return Math.Max(0, amount);
        }

        public int PreviewDamage(ICombatant source, ICombatant target, int baseAmount)
        {
            return CalculateDamage(source, target, baseAmount);
        }

        public void DealDamage(ICombatant source, ICombatant target, int baseAmount)
        {
            Combatant combatant = Require(target);
            if (!combatant.IsAlive)
            {
                return;
            }

            DamageResult result = combatant.TakeDamage(CalculateDamage(source, target, baseAmount));
            DamageDealt?.Invoke(source, combatant, result);
        }

        public void GainBlock(ICombatant target, int amount)
        {
            Require(target).GainBlock(amount);
        }

        private static Combatant Require(ICombatant target)
        {
            if (target is Combatant combatant)
            {
                return combatant;
            }

            throw new InvalidOperationException(
                $"CombatResolver can only act on {nameof(Combatant)}, but got {(target == null ? "null" : target.GetType().Name)}.");
        }
    }
}
