// The combat model's implementation of ICombatActions, so every card and enemy move resolves through one place:
// base damage, then damage modifiers in order, then Kavach, then health. CalculateDamage is also what card and
// intent previews call, so a shown number always matches what lands. DamageDealt reports the source as well,
// for later rules such as thorns or lifesteal; health lost with no attacker (Dahan's burn) reports a null source.
// StatusApplied reports each status change an effect makes, with the amount asked for (negative when spent).
// Unmodified damage (Brahmastra) skips the modifiers but still meets Kavach and reports through DamageDealt.
// A status gained (not spent) then runs the target's status triggers, so Devotion turns gained Dharma into Kavach. It also serves
// IDamagePreview for enemy intents, and ICardActions once the combat model attaches the deck and Prana, so draw and
// Prana effects resolve through the same place. ModifiersChanged fires when a modifier is added or removed, or when a modifier
// whose own numbers changed calls NotifyModifiersChanged (the combat model does so whenever a status changes), so
// card text and intents can refresh their previews only when something changed.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Statuses;

namespace JourneyToLanka.Combat.Model
{
    public sealed class CombatResolver : ICombatActions, IDamagePreview, ICardActions
    {
        private readonly List<IDamageModifier> _modifiers = new List<IDamageModifier>(8);
        private CombatDeck _deck;
        private PranaPool _prana;

        // Source (null for health lost with no attacker), target, what the hit did.
        public event Action<ICombatant, Combatant, DamageResult> DamageDealt;

        // Target, status, amount applied.
        public event Action<Combatant, StatusDefinitionSO, int> StatusApplied;

        public event Action ModifiersChanged;

        public int ModifierCount => _modifiers.Count;

        public void AttachDeck(CombatDeck deck)
        {
            _deck = deck;
        }

        public void AttachPrana(PranaPool prana)
        {
            _prana = prana;
        }

        public void GainPrana(int amount)
        {
            _prana?.Gain(amount);
        }

        public int DrawCards(int count)
        {
            return _deck != null && count > 0 ? _deck.Draw(count) : 0;
        }

        public void AddModifier(IDamageModifier modifier)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }

            if (!_modifiers.Contains(modifier))
            {
                _modifiers.Add(modifier);
                NotifyModifiersChanged();
            }
        }

        public bool RemoveModifier(IDamageModifier modifier)
        {
            if (modifier == null || !_modifiers.Remove(modifier))
            {
                return false;
            }

            NotifyModifiersChanged();
            return true;
        }

        public void NotifyModifiersChanged()
        {
            ModifiersChanged?.Invoke();
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

        public void DealUnmodifiedDamage(ICombatant source, ICombatant target, int amount)
        {
            Combatant combatant = Require(target);
            if (!combatant.IsAlive || amount < 0)
            {
                return;
            }

            DamageResult result = combatant.TakeDamage(amount);
            DamageDealt?.Invoke(source, combatant, result);
        }

        public void GainBlock(ICombatant target, int amount)
        {
            Require(target).GainBlock(amount);
        }

        public void RetainBlock(ICombatant target)
        {
            Require(target).RetainBlockOnce();
        }

        public void RemoveBlock(ICombatant target)
        {
            Require(target).ClearBlock();
        }

        public void Heal(ICombatant target, int amount)
        {
            Require(target).Heal(amount);
        }

        public int GetStatusAmount(ICombatant target, StatusDefinitionSO status)
        {
            return target is Combatant combatant ? combatant.Statuses.GetAmount(status) : 0;
        }

        public int GetMaxHealth(ICombatant target)
        {
            return target is Combatant combatant ? combatant.MaxHealth : 0;
        }

        public void ApplyStatus(ICombatant target, StatusDefinitionSO status, int amount, int duration)
        {
            Combatant combatant = Require(target);
            if (!combatant.IsAlive || status == null || amount == 0)
            {
                return;
            }

            combatant.ApplyStatus(status, amount, duration);
            StatusApplied?.Invoke(combatant, status, amount);
            if (amount > 0)
            {
                combatant.Statuses.TriggerStatusGained(status, amount, new StatusTickContext(this, combatant));
            }
        }

        public void LoseHealth(ICombatant target, int amount)
        {
            Combatant combatant = Require(target);
            if (!combatant.IsAlive || amount <= 0)
            {
                return;
            }

            DamageResult result = combatant.LoseHealth(amount);
            DamageDealt?.Invoke(null, combatant, result);
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
